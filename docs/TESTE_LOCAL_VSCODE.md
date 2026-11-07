# Laboratório Bluetooth Saga — execução local no VS Code

## Preparação

- Windows 10 versão 2004+ ou Windows 11 no PC próximo à fechadura, adaptador Bluetooth LE com driver e rádio ativos.
- Fechadura autorizada para testes, módulo HB2546 alimentado e cartão físico de reserva.
- VS Code e extensões recomendadas C# e PowerShell.
- SDK .NET 10 instalado: https://dotnet.microsoft.com/download/dotnet/10.0

No Windows PowerShell 5.1 (`powershell.exe`), mantenha os scripts `scripts/*.ps1` em UTF-8 com BOM. Sem o BOM, caracteres tipográficos podem ser interpretados incorretamente e impedir a tarefa de iniciar.

Abra a pasta raiz do repositório no VS Code. A varredura inicial não exige código/PIN da fechadura. O acesso a eventuais credenciais de abertura é uma etapa separada.

## Um comando para começar

No VS Code: Ctrl+Shift+P, Tasks: Run Task e escolha Saga BLE: 00 - Diagnóstico completo (guiado).

A tarefa executa scripts/Test-SagaBle.ps1 e:

1. Verifica Windows, SDK .NET e componentes Bluetooth detectados pelo Windows.
2. Compila o projeto em Release, executa autotestes C# e valida JSON sintético.
3. Faz varredura por 30 segundos e lista possíveis dispositivos BLE com nome, endereço e RSSI.
4. Solicita ao operador que identifique o dispositivo a examinar, por número. ENTER pula GATT.
5. Faz uma segunda varredura de confirmação e somente busca os metadados GATT desse dispositivo.
6. Valida os resultados e mostra os caminhos dos arquivos locais.

Arquivos gerados em diagnostics/scan-AAAAMMDD-HHMMSS.json e diagnostics/gatt-AAAAMMDD-HHMMSS.json. A pasta é ignorada pelo Git, mas contém dados potencialmente identificáveis e não deve ser publicada.

O relatório da varredura registra também o tipo de endereço e de anúncio, incluindo as contagens de anúncios conectáveis, escaneáveis e respostas a scan request. Isso ajuda a distinguir um anúncio que pode aceitar GATT de um beacon não conectável.

## Tarefas disponíveis

- 00 — Diagnóstico completo guiado: tudo até a descoberta GATT.
- 01 — Pré-requisitos Windows: não inicia varredura.
- 02 — Testes completos offline: build e autotestes sem rádio/fechadura.
- 03 — Somente varredura: scan e JSON sem perguntas.
- 04 — Inspecionar GATT com endereço escolhido: endereço já identificado; repete scan.
- 05 — Validar estrutura de um relatório: contagens e estados sem exibir MACs.
- 06 — Inspecionar janela breve por nome exato: inicia GATT assim que o anúncio conectável único com esse nome aparece; útil para um botão que só ativa o BLE por pouco tempo.
- Compilar (Debug/Release): código.
- F5: configurações para depurar scanner ou menu interativo.

## Exemplos de terminal no computador Windows

    powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-SagaBle.ps1 -OfflineOnly
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-SagaBle.ps1 -Seconds 30
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-SagaBle.ps1 -Seconds 45 -Address AA:BB:CC:DD:EE:FF -NonInteractive

O endereço deve aparecer na varredura e ser identificado pelo operador. NÃO se conecta automaticamente a todo Bluetooth ao redor.

## Interpretação

- OFFLINE_PASS: compilação, autotestes e parser de relatório funcionam; não comprova acesso ao rádio.
- 0 dispositivos: investigar distância, drivers, bateria, alimentação, modo repouso, provisionamento.
- Dispositivos com RSSI: anúncios recebidos; não comprova que sejam a fechadura.
- GATT Success: serviços descobertos; não significa autenticação ou que a porta abriu.
- GATT Unreachable/AccessDenied: avaliar estado de rádio, permissão e documentação da Saga, sem tentativa de PIN.
- Endereço ausente na segunda varredura: anúncio intermitente; repetir diagnóstico com dispositivo próximo.

## Próxima fase, após receber protocolo oficial

Confirmar referência e firmware do módulo, UUIDs documentados, autenticação, credenciais temporárias, chaves de acesso, revogação no checkout, coexistência com cartões MIFARE/BIS Hotel, disponibilidade offline e forma de validar abertura física.

Não implementar escrita GATT por tentativa e erro. Nunca inserir credenciais em commits, settings do VS Code ou logs públicos.

Para uma janela curta, escolha a tarefa 06, informe o nome exato observado na tarefa 00 e aperte o botão da placa somente quando o scanner estiver ativo. A conexão tenta apenas descobrir serviços/características; não faz pareamento nem lê ou escreve valores.
