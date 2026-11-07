# Integração autorizada Saga + TTLock eKey

Atualizado em 07/11/2026.

## Evidências recebidas

- A Saga informou ao operador que seus módulos BLE funcionam com TTLock.
- O usuário confirmou que o módulo identificado na varredura aparece no Bluetooth do próprio celular. Isso corrobora a identificação local, mas não comprova que a fechadura esteja cadastrada/provisionada no TTLock.
- O guia oficial TTLock diz que o SDK de aplicativo comunica com a fechadura por Bluetooth e encapsula o protocolo BLE. São listados SDKs nativos para Android e iOS.
- O fluxo eKey é: o administrador provisiona/envia uma eKey; o usuário a sincroniza no aplicativo; o SDK usa os dados autorizados da fechadura para a operação Bluetooth. O demo oficial Android usa `TTLockClient.controlLock(...)`.
- A Open Platform requer aplicação de desenvolvedor aprovada para integração própria. A conta de desenvolvedor e a conta de usuário TTLock têm funções distintas. A documentação diz que não há conta de teste ou sandbox.

## Arquitetura compatível

O projeto atual é um diagnóstico C#/.NET para Windows. Ele enumera anúncios e metadados GATT, mas não é o SDK TTLock e não pode testar a operação de eKey.

Uma aplicação própria deve usar o SDK oficial no telefone Android/iOS e uma eKey autorizada. Não implementar bytes, UUIDs de escrita ou sessões BLE manualmente. Uma propriedade GATT `Write` apenas declara suporte de transporte; não define o significado nem a autorização do dado.

## Salvaguardas para esta fechadura

- Não chamar `initLock`, reset, delete, alteração de configuração ou firmware numa fechadura já usada pelo hotel sem instrução escrita da Saga/TTLock e procedimento de recuperação. A inicialização é um fluxo de cadastro/admin separado do desbloqueio por eKey.
- Não usar a fechadura enquanto estiver ocupada; testar em bancada ou unidade fora de serviço, com cartão físico reserva e confirmação física do resultado.
- Não colocar `client_secret`, tokens, `lockData`, chaves, PINs ou dados de hóspede no repositório, no relatório BLE, no log ou no chat. Configurar segredos localmente quando existir integração aprovada.
- Não presumir que aparecer nas configurações Bluetooth do celular significa que a fechadura está provisionada ou que o usuário possui permissão de abertura.

## Bloqueios antes de um teste físico autenticado

1. Confirmar no app TTLock que a fechadura já está cadastrada na conta correta e que há uma eKey válida para a conta de teste. Não inicializar a unidade existente para tentar fazê-la aparecer.
2. Confirmar com Saga/TTLock o modelo e firmware da placa, bem como a compatibilidade do provisionamento atual.
3. Para um app próprio, obter aprovação de uma aplicação TTLock e configurar as credenciais localmente; nunca compartilhar segredos no chat.
4. Usar um telefone Android/iOS com o SDK oficial. Este PC Windows não tem Java, Gradle nem ADB instalados, e o scanner desktop não envia comandos.
5. Antes de qualquer teste de abertura, confirmar a operação autorizada e que a unidade está em bancada/fora de ocupação, com cartão de reserva e recuperação física disponíveis.

## Referências oficiais

- [TTLock — Unlock with APP (eKey)](https://cnopen.ttlock.com/documentPages/htmlPages/userGuide/ekeyEn.html)
- [TTLock — How to get started](https://euopen.ttlock.com/documentPages/htmlPages/userGuide/getStartedEn.html)
- [TTLock — Android SDK Demo](https://github.com/ttlock/Android_SDK_Demo)
