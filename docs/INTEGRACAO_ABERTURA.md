# Contrato de integração futura de abertura Saga

Estado: NÃO HOMOLOGADO. Nenhuma credencial, protocolo ou UUID de abertura real foi obtido da placa HB2546. O projeto faz descoberta e consulta apenas metadados GATT.

## Modelo da integração autenticada

Quando houver documentação oficial Saga/VEGA, implementar módulo separado que represente:

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

## Dados que faltam

Modelo exato da placa, firmware, SDK/API Windows/Android/iOS, relação com VSKey (se houver), forma de provisionamento, ciclo de vida das chaves, autorização e licenciamento, mecanismos de estado e especificações de manutenção. Não se deve escrever em características GATT sem esse contrato.
