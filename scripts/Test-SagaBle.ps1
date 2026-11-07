# Diagnóstico controlado por VS Code. Nunca escreve no dispositivo BLE.
[CmdletBinding()]
param(
    [ValidateRange(3,120)][int]$Seconds = 30,
    [string]$Address = "",
    [switch]$PreflightOnly,
    [switch]$OfflineOnly,
    [switch]$NonInteractive
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Step([string]$Message) {
    Write-Host ""
    Write-Host ("=== " + $Message + " ===") -ForegroundColor Cyan
}

function DotNet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw ("dotnet falhou (código " + $LASTEXITCODE + "): " + ($Arguments -join " "))
    }
}

function Format-Address([string]$Value) {
    $hex = $Value.Trim().Replace(':','').Replace('-','').ToUpperInvariant()
    if ($hex -notmatch '^[0-9A-F]{12}$') {
        throw "Endereço BLE inválido. Use AA:BB:CC:DD:EE:FF."
    }
    return (($hex -split '(.{2})' | Where-Object { $_ }) -join ':')
}

try {
    $project = Join-Path $PSScriptRoot '..\src\SagaBle.Diagnostic\SagaBle.Diagnostic.csproj'
    $project = [System.IO.Path]::GetFullPath($project)
    $repository = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $reports = Join-Path $repository 'diagnostics'
    $fixture = Join-Path $repository 'tests\fixtures\ble-sintetico.json'
    $analyzer = Join-Path $PSScriptRoot 'Analyze-Report.ps1'

    Step "Pré-requisitos"
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        throw "Este procedimento precisa rodar no Windows conectado ao adaptador Bluetooth."
    }
    if (-not (Test-Path -LiteralPath $project)) { throw "Projeto C# não encontrado: $project" }

    $dotnet = Get-Command 'dotnet' -ErrorAction SilentlyContinue
    if (-not $dotnet) { throw "SDK .NET 10 ausente. Instale https://dotnet.microsoft.com/download/dotnet/10.0" }
    $version = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0) { throw "Falha ao consultar dotnet --version." }
    $major = [int](($version -split '\.')[0])
    if ($major -lt 10) { throw "SDK .NET 10 ou superior é necessário. Encontrado: $version" }
    Write-Host "SDK .NET: $version" -ForegroundColor Green
    Write-Host "Sistema: $([Environment]::OSVersion.VersionString)"
    Write-Host "Projeto: $project"

    $adapters = @()
    try {
        $adapters = @(Get-PnpDevice -Class Bluetooth -ErrorAction Stop |
            Where-Object { $_.Status -eq 'OK' })
        Write-Host "Dispositivos Bluetooth do Windows com status OK: $($adapters.Count)"
        if ($adapters.Count -eq 0) {
            Write-Warning "Nenhum dispositivo na classe Bluetooth com status OK. Confirme driver/rádio no Windows."
        }
    }
    catch {
        Write-Warning "Não foi possível listar adaptadores por Get-PnpDevice. O scanner C# fará sua própria verificação."
    }

    if ($PreflightOnly) {
        Write-Host "Pré-requisitos básicos verificados; a presença de BLE será validada ao executar a varredura."
        exit 0
    }

    Step "Compilar a ferramenta"
    DotNet @('build',$project,'--configuration','Release','--nologo')

    Step "Autotestes locais, sem hardware"
    DotNet @('run','--project',$project,'--configuration','Release','--no-build','--','--self-test')

    Step "Teste de análise com dados sintéticos"
    if (-not (Test-Path -LiteralPath $fixture)) {
        throw "Fixture de teste ausente: $fixture"
    }
    & $analyzer -Path $fixture -ExpectDevices 2 -ExpectInspections 1
    if (-not $?) { throw "Validação do relatório sintético falhou." }

    if ($OfflineOnly) {
        Write-Host "OFFLINE_PASS — compilação, autotestes e parser do relatório passaram." -ForegroundColor Green
        exit 0
    }

    New-Item -ItemType Directory -Path $reports -Force | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $scanPath = Join-Path $reports ("scan-" + $stamp + ".json")

    Step "Escanear Bluetooth Low Energy por $Seconds segundos"
    DotNet @('run','--project',$project,'--configuration','Release','--no-build','--',
        '--scan',"$Seconds",'--export',$scanPath)

    $scan = Get-Content -LiteralPath $scanPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $devices = @($scan.Devices | Where-Object { $null -ne $_ })
    if ($devices.Count -eq 0) {
        Write-Warning "Nenhum anúncio BLE recebido. Não é possível identificar o módulo ainda."
        Write-Host "Verifique alimentação, distância e se o fabricante exige ativação/provisionamento."
        Write-Host "Relatório local: $scanPath"
        exit 3
    }

    Step "Dispositivos observados (não são fechaduras confirmadas)"
    for ($i = 0; $i -lt $devices.Count; $i++) {
        $d = $devices[$i]
        $deviceName = if ([string]::IsNullOrWhiteSpace([string]$d.Name)) { '(sem nome)' } else { [string]$d.Name }
        Write-Host ("{0,2}) {1}  RSSI {2,4} dBm  {3}" -f
            ($i + 1),$d.Address,$d.RssiDbm,$deviceName)
    }

    $selected = ""
    if (-not [string]::IsNullOrWhiteSpace($Address)) {
        $selected = Format-Address $Address
    }
    elseif (-not $NonInteractive) {
        Write-Host ""
        Write-Host "Selecione somente o módulo sob sua responsabilidade. ENTER apenas salva a varredura."
        $choice = Read-Host "Número da lista para inspecionar GATT (ou ENTER para pular)"
        if (-not [string]::IsNullOrWhiteSpace($choice)) {
            $parsed = 0
            if (-not [int]::TryParse($choice,[ref]$parsed) -or
                $parsed -lt 1 -or $parsed -gt $devices.Count) {
                throw "Seleção inválida. Nenhum comando foi enviado à fechadura."
            }
            $selected = [string]$devices[$parsed - 1].Address
        }
    }

    if ([string]::IsNullOrWhiteSpace($selected)) {
        Step "Varredura concluída"
        Write-Host "Relatório local: $scanPath"
        Write-Host "Sem inspeção GATT — use a tarefa guiada novamente e escolha um dispositivo."
        exit 0
    }

    $found = @($devices | Where-Object { ([string]$_.Address).ToUpperInvariant() -eq $selected })
    if ($found.Count -eq 0) {
        throw "Endereço não observado neste teste: $selected. Nenhuma conexão será tentada."
    }

    Step "Descoberta GATT de $selected (somente metadados)"
    Write-Host "Segunda varredura necessária: confirma que o endereço ainda está anunciando."
    $gattPath = Join-Path $reports ("gatt-" + $stamp + ".json")
    DotNet @('run','--project',$project,'--configuration','Release','--no-build','--',
        '--scan',"$Seconds",'--inspect',$selected,'--export',$gattPath)

    Step "Validar resultado GATT"
    & $analyzer -Path $gattPath -ExpectInspections 1
    if (-not $?) { throw "A análise de metadados GATT não passou." }

    Write-Host ""
    Write-Host "DIAGNOSTICO_CONCLUIDO — comunicação de descoberta GATT executada (não é abertura)." -ForegroundColor Green
    Write-Host "Varredura: $scanPath"
    Write-Host "GATT: $gattPath"
    Write-Host "Os arquivos contêm identificadores BLE. NÃO faça upload público." -ForegroundColor Yellow
}
catch {
    Write-Error ("Teste interrompido: " + $_.Exception.Message)
    exit 1
}
