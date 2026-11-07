# Instruções para agente local do VS Code — Bluetooth Saga

## Objetivo e escopo atual

Continuar o diagnóstico autorizado da fechadura hoteleira Saga Systems fotografada pelo usuário, com placa identificada visualmente como HB2546. O protocolo BLE de abertura, firmware, UUIDs reais e autenticação ainda não foram identificados.

O programa em src/SagaBle.Diagnostic utiliza o rádio BLE do Windows. O computador com o Bluetooth é o do operador, não a máquina do GitHub Actions.

## Antes dos testes

1. Leia README.md, docs/TESTE_LOCAL_VSCODE.md e docs/INTEGRACAO_ABERTURA.md.
2. Os relatórios em diagnostics/ são privados, ignorados pelo Git e podem conter MAC BLE e dados de publicidade. Não envie ao GitHub público.
3. Confirme fechadura de testes autorizada, fora de ocupação e mantenha cartão físico de reserva.
4. Execute no VS Code a tarefa Saga BLE: 02 - Testes completos offline (sem fechadura).
5. Depois execute Saga BLE: 00 - Diagnóstico completo (guiado) no próprio Windows ao lado da fechadura.
6. O usuário escolherá um dispositivo visto na varredura. Não deduza identidade somente pelo RSSI, nome ou UUID.
7. A consulta GATT é exclusivamente de metadados. Não faça ReadValue, WriteValue, pareamento automático, reset, brute-force ou destravamento.
8. A análise de JSON pode ser feita com Saga BLE: 05 - Validar estrutura de um relatório.
9. Para avançar à abertura, obtenha SDK oficial e credenciais/provisionamento legítimo. Não invente protocolo.

## Definição de sucesso nesta fase

- Ambiente Windows e SDK .NET 10 verificados.
- Compilação e testes offline aprovados.
- Scanner BLE rodando fisicamente junto à fechadura.
- Endereço selecionado e confirmado pelo operador se anunciado.
- Serviços GATT identificados sem alterar o estado do equipamento.
- Relatório privado, falhas diagnosticadas e próximos bloqueios documentados.

Nunca declare que abriu a fechadura sem confirmação física. O GitHub Actions não testa Bluetooth real. O debugger F5 usa .vscode/launch.json; as tarefas usam .vscode/tasks.json.

## Instruções de segurança para o agente

O primeiro ciclo serve somente para leitura e descoberta. Não manipule fechaduras de hóspedes, não suba logs brutos ou segredos a commits/issues, não tente PINs, não escreva em características GATT desconhecidas e não modifique o sistema BIS Hotel/cartões físicos.

Se não houver anúncios, reporte isso e analise alimentação, alcance, driver, modo BLE e eventual provisionamento com a Saga; não assuma defeito nem protocolo.
