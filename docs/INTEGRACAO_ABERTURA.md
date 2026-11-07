# Contrato de integração futura de abertura Saga

Estado: rota oficial TTLock identificada, mas integração física ainda NÃO HOMOLOGADA. A Saga informou ao operador que seus módulos BLE funcionam com TTLock. O fluxo eKey usa o SDK móvel oficial; ainda faltam confirmar firmware/provisionamento da unidade, vínculo da fechadura a uma conta TTLock, eKey autorizada e teste controlado. O projeto atual continua limitado a descoberta e metadados GATT.

## Evidência de integração TTLock

- O guia oficial eKey informa que o SDK do aplicativo comunica com a fechadura via Bluetooth e encapsula o protocolo BLE; os SDKs publicados são nativos para Android e iOS.
- No fluxo eKey, o administrador envia a chave eletrônica e o usuário sincroniza os dados da eKey no aplicativo. O demo Android oficial usa `TTLockClient.controlLock(...)` com dados da fechadura/eKey.
- A documentação de início do TTLock exige conta de desenvolvedor e aprovação de aplicativo para uso próprio da Open Platform; a conta TTLock do operador é separada. O TTLock informa que não fornece conta de teste ou sandbox.
- O demo também possui um fluxo separado `initLock`. Não executar inicialização, exclusão, reset ou mudança de configuração numa fechadura existente do hotel sem orientação escrita específica da Saga/TTLock e plano de recuperação dos cartões BIS/MIFARE.

Referências: [guia eKey TTLock](https://cnopen.ttlock.com/documentPages/htmlPages/userGuide/ekeyEn.html), [início da integração TTLock](https://euopen.ttlock.com/documentPages/htmlPages/userGuide/getStartedEn.html), [demo oficial Android](https://github.com/ttlock/Android_SDK_Demo).

## Modelo da integração autenticada

Quando houver provisionamento e autorização oficial para a unidade de teste, implementar módulo separado que represente:

- DeviceIdentity: identificador provisionado, modelo e firmware verificados por método oficial, não inferidos pelo RSSI.
- AccessGrant: autorização temporária do sistema homologado, apartamento, reserva, validade e vínculo a hóspede/dispositivo.
- OpenAttempt: solicitação autenticada, contexto de auditoria, timeout, nonce ou contador conforme SDK.
- OpenResult: status confirmado separando sucesso de transporte e abertura física (Opened, Rejected, Expired, Unavailable, Unknown).
- RevokeGrant: procedimento de revogação na saída do hóspede e recuperação segura.
- Provider: SDK oficial ou cliente API autorizado, sem UUID ou bytes de abertura inventados.

## Portões de aprovação

1. Comunicação criptográfica e autenticação contra replay conforme especificação homologada.
2. Segredos fora do código-fonte, relatórios, logs e GitHub.
3. Teste em fechadura do hotel fora de ocupação, mantendo cartão físico de reserva.
4. Confirmação de coexistência com cartões MIFARE e sistema BIS Hotel.
5. Testes de revogação, troca de quarto, chave expirada, celular perdido, offline, falhas de rede e horário.
6. Sucesso BLE/GATT isolado não é prova de porta aberta.
7. Documentação do fabricante, auditoria e validação física antes de produção.

## Dados e condições que faltam

Confirmação da Saga/TTLock sobre modelo exato e firmware compatíveis; saber se a fechadura já está cadastrada no TTLock sem re-inicializá-la; eKey temporária ativa para uma conta autorizada; credenciais de desenvolvedor aprovadas se for criada uma aplicação própria; telefone Android/iOS para executar o SDK; teste em unidade de bancada ou fora de ocupação com cartão reserva. Não enviar tokens, lockData, chaves ou credenciais por chat nem armazená-los em código, relatórios ou GitHub. O scanner Windows não deve escrever diretamente em características GATT.
