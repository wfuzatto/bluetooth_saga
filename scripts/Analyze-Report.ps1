# Inspeção de estrutura do relatório BLE sem publicar endereços ou dados do fabricante.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Path,
    [int]$ExpectDevices = -1,
    [int]$ExpectInspections = -1
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "Arquivo de diagnóstico inexistente: $Path"
}

$report = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
if ($null -eq $report -or $report.Project -ne 'bluetooth_saga') {
    throw "Arquivo inválido: não é relatório do bluetooth_saga."
}
if (-not ([string]$report.Mode -match 'somente leitura')) {
    throw "Relatório fora do modo seguro esperado."
}
$devices = @($report.Devices | Where-Object { $null -ne $_ })
$inspections = @($report.Inspections | Where-Object { $null -ne $_ })

if ($ExpectDevices -ge 0 -and $devices.Count -ne $ExpectDevices) {
    throw "Esperados $ExpectDevices dispositivos, recebidos $($devices.Count)."
}
if ($ExpectInspections -ge 0 -and $inspections.Count -ne $ExpectInspections) {
    throw "Esperadas $ExpectInspections inspeções, recebidas $($inspections.Count)."
}

foreach ($d in $devices) {
    if ([string]$d.Address -notmatch '^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$') {
        throw "Relatório contém formato de endereço BLE inválido."
    }
}
foreach ($entry in $inspections) {
    if ([string]::IsNullOrWhiteSpace([string]$entry.Status)) {
        throw "Inspeção sem status."
    }
}
$services = @($inspections | ForEach-Object { $_.Services } | Where-Object { $null -ne $_ })
$characteristics = @($services | ForEach-Object { $_.Characteristics } | Where-Object { $null -ne $_ })

Write-Host ("Relatório válido | dispositivos={0} | inspeções={1} | serviços={2} | características={3}" -f
    $devices.Count,$inspections.Count,$services.Count,$characteristics.Count) -ForegroundColor Green
foreach ($entry in $inspections) {
    Write-Host ("  Estado GATT: {0}; serviços: {1}" -f $entry.Status, @($entry.Services).Count)
}
Write-Host "Resumo sem MACs/dados de fabricante. Original é local e confidencial."
