# Bluetooth Saga — diagnóstico BLE de fechaduras hoteleiras

Projeto de integração para fechadura Saga Systems com módulo fotografado **HB2546**.

**Estado real:** o diagnóstico BLE foi implementado no código. A abertura de porta **ainda não está implementada**: falta confirmar o protocolo/SDK oficial, o provisionamento e as credenciais de autorização. A ferramenta **não** substitui o sistema BIS Hotel nem interfere nos cartões MIFARE.

## Diagnóstico automatizado pelo VS Code

Abra o repositório no **VS Code instalado no computador Windows que tem Bluetooth**.
Instale o SDK **.NET 10** e abra **Terminal → Run Task / Executar Tarefa**. As tarefas disponíveis são:

- **Saga BLE: autoteste sem fechadura** — testa o código sem depender do rádio Bluetooth.
- **Saga BLE: varredura automática (30 segundos)** — escuta anúncios e salva `diagnostics/saga-scan.json`.
- **Saga BLE: inspecionar endereço (30 segundos)** — solicita o endereço BLE que apareceu na varredura, faz nova busca, conecta apenas se o dispositivo estiver presente e exporta metadados GATT em `diagnostics/saga-gatt.json`.

Os arquivos da pasta `diagnostics/` são ignorados pelo Git, mas **continuam sensíveis e não devem ser publicados**.

**Nenhuma das tarefas abre ou destrava a porta.** O comando de abertura só poderá ser implementado com documentação/SDK e uma credencial válida do fabricante. O GitHub Actions testa apenas software; a varredura física acontece exclusivamente no computador com a fechadura.

Também é possível automatizar a inspeção no terminal:

    dotnet run --project src/SagaBle.Diagnostic/SagaBle.Diagnostic.csproj -- --scan 30 --inspect AA:BB:CC:DD:EE:FF --export diagnostics/saga-gatt.json

O endereço deve ser real e ter sido detectado na varredura; o programa rejeita endereços ausentes.

## Executável Windows

Baixe o arquivo **SagaBleDiagnostico-Windows-x64** na aba [Actions](https://github.com/wfuzatto/bluetooth_saga/actions) → execução **Compilar diagnóstico Bluetooth Windows** → **Artifacts**. Extraia o ZIP e execute **SagaBleDiagnostico.exe**. É necessário Windows 10 (2004+) ou Windows 11 e adaptador compatível com BLE.

O EXE é publicado como arquivo único autossuficiente (.NET 10), sem necessidade de instalação separada do runtime.

## Usar no computador próximo à fechadura

1. Deixe o Bluetooth do Windows ativo e aproxime o computador da fechadura ligada.
2. Abra **SagaBleDiagnostico.exe**; o programa faz uma varredura de 20 segundos.
3. Veja nome, endereço BLE, RSSI e quantidade de UUIDs anunciados.
4. Escolha **2** para inspecionar os serviços e as características GATT do dispositivo selecionado.
5. Escolha **3** para exportar um JSON local (por padrão em %LOCALAPPDATA%/BluetoothSaga/Relatorios).
6. Guarde o relatório de forma privada. O endereço Bluetooth, UUIDs e dados do fabricante podem identificar equipamentos.

O scanner não realiza abertura, tentativa de senha, escrita ou leitura de valores de características. A inspeção GATT pode iniciar uma conexão BLE de descoberta, sem alteração da fechadura.

Se a placa não aparecer, ela pode estar em repouso, não provisionada, sem alimentação correta ou usando um método de comunicação diferente. Não é possível concluir que a placa está com defeito apenas pela varredura.

## Execução via terminal

    .\SagaBleDiagnostico.exe
    .\SagaBleDiagnostico.exe --scan 30
    .\SagaBleDiagnostico.exe --scan 30 --export C:\Temp\saga.json
    .\SagaBleDiagnostico.exe --self-test

## Desenvolvimento

O código está em src/SagaBle.Diagnostic, em C#/.NET, utilizando as APIs nativas Windows Bluetooth LE. A compilação é configurada no GitHub Actions para Windows x64.

    dotnet build src/SagaBle.Diagnostic/SagaBle.Diagnostic.csproj -c Release
    dotnet run --project src/SagaBle.Diagnostic/SagaBle.Diagnostic.csproj -c Release

O fluxo para integrar abertura autenticada, perguntas ao fabricante e salvaguardas está em [docs/PROTOCOL_AND_SECURITY.md](docs/PROTOCOL_AND_SECURITY.md).

### Limitações

- O GitHub Actions só compila/testa em uma máquina remota: **não consegue acessar o Bluetooth do seu computador**.
- Nenhum UUID observado na varredura comprova sozinho o protocolo de abertura.
- Sem documentação e credenciais homologadas, **não existe botão de destravamento funcional** neste projeto.
- Não suba logs de diagnóstico, credenciais ou identificadores do hotel em issues públicas.
