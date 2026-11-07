# Bluetooth Saga — laboratório BLE para fechaduras Saga Systems

Ferramenta Windows C#/.NET para diagnóstico do módulo fotografado HB2546 em fechadura hoteleira Saga Systems. Preserva a operação de cartões MIFARE/BIS Hotel, sem alterar a fechadura.

**Situação atual:** scanner Bluetooth LE, identificação de anúncios, inspeção de metadados GATT, exportação local de JSON, tarefas VS Code e testes offline. **Não abre portas ainda.** O protocolo e a credencial de abertura oficial da placa não foram identificados.

## Comece aqui no VS Code

1. Clone ou atualize https://github.com/wfuzatto/bluetooth_saga no **Windows que está próximo à fechadura**.
2. Instale o SDK .NET 10 em https://dotnet.microsoft.com/download/dotnet/10.0 e use VS Code com as extensões C# e PowerShell sugeridas.
3. Abra a pasta raiz do repositório no VS Code.
4. Pressione Ctrl+Shift+P, escolha Tasks: Run Task e **Saga BLE: 00 - Diagnóstico completo (guiado)**.
5. O VS Code verificará pré-requisitos, compilará, executará autotestes, escaneará Bluetooth LE por 30 segundos e listará os dispositivos observados.
6. Escolha o número do dispositivo identificado como sendo a sua fechadura (ou ENTER para não consultar nenhum). Só o dispositivo escolhido será examinado por GATT, e apenas seus metadados.
7. Os JSONs ficam na pasta privada e ignorada pelo Git: diagnostics/. Não publique os resultados brutos no GitHub.

A primeira varredura **não exige código PIN ou número de fechadura**. Esse tipo de informação só será necessário se o fabricante requerer provisionamento ou autenticação para outras funções. Um serviço GATT encontrado não comprova a identidade do módulo nem permite inventar um comando de abertura.

## Tarefas disponíveis

- Saga BLE: 00 - Diagnóstico completo (guiado) — fluxo recomendado, termina com análise GATT quando um dispositivo é escolhido.
- Saga BLE: 01 - Pré-requisitos Windows — verifica ambiente.
- Saga BLE: 02 - Testes completos offline (sem fechadura) — build, self-test e fixture sintética.
- Saga BLE: 03 - Somente varredura (sem interação) — BLE e exportação JSON.
- Saga BLE: 04 - Inspecionar GATT com endereço escolhido — endereço proveniente de varredura anterior.
- Saga BLE: 05 - Validar estrutura de um relatório — lê relatório JSON local sem imprimir identificadores.
- Saga BLE: compilar (Debug/Release) — build do projeto.

**Depuração:** pressione F5 e escolha scanner ou modo interativo configurado em .vscode/launch.json.

Para teste local também é possível abrir um terminal PowerShell:

    powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-SagaBle.ps1 -OfflineOnly
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-SagaBle.ps1 -Seconds 30

## Código e documentos

- src/SagaBle.Diagnostic — scanner e explorador GATT do Windows.
- scripts/Test-SagaBle.ps1 — orquestrador do laboratório local e validação offline.
- scripts/Analyze-Report.ps1 — validador de relatório.
- tests/fixtures/ble-sintetico.json — simulação sem hardware.
- .vscode/tasks.json e .vscode/launch.json — execução e depuração.
- AGENTS.md — procedimentos e limites para agentes de programação no VS Code.
- docs/TESTE_LOCAL_VSCODE.md — manual completo, incluindo falhas e interpretações.
- docs/INTEGRACAO_ABERTURA.md — contrato da futura abertura homologada.
- docs/PROTOCOL_AND_SECURITY.md — roteiro de identificação e segurança.

## Executável compilado no GitHub

Acesse https://github.com/wfuzatto/bluetooth_saga/actions e abra a execução concluída do workflow **Compilar e testar diagnóstico Bluetooth Saga**. Em Artifacts, baixe SagaBleDiagnostico-Windows-x64 (ZIP), extraia o EXE e rode no PC Windows com BLE. O EXE é autossuficiente, mas as tarefas de VS Code baseadas em código-fonte exigem SDK .NET 10.

**Limitações importantes:** o GitHub Actions roda em máquina remota sem acesso ao Bluetooth deste hotel. A compilação e o teste com dados sintéticos não substituem um teste físico, não demonstram abertura e não identificam o protocolo do módulo. A integração de abertura só pode avançar após a especificação e o provisionamento/autorização oficial da Saga/VEGA.

Não execute comandos arbitrários, scripts que escrevam em características GATT, tentativa de PIN ou reset. Nunca publique PIN, chaves digitais, dados de hóspedes, endereços BLE ou logs brutos no repositório público.
