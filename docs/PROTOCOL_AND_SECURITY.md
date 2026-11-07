# Identificação BLE e segurança da fechadura Saga

## Material de referência

Fotos do equipamento: fechadura Saga Systems com leitor frontal e módulo verde com inscrição **HB2546**.
A inscrição na placa, por si só, **não identifica** chipset, protocolo, SDK, versão de firmware nem forma de autenticação.
O leitor de cartões MIFARE e a plataforma BIS Hotel existente devem continuar funcionando sem alteração.

## Estado da integração

- [x] Diagnóstico Windows BLE por anúncios e RSSI
- [x] Descoberta de UUIDs GATT e propriedades de características sem leitura de valores
- [x] Relatório JSON salvo somente no Windows local
- [x] Configuração de compilação automática de EXE com GitHub Actions
- [ ] Teste físico em uma fechadura autorizada
- [ ] Identificação do fabricante/firmware do HB2546
- [ ] Documentação oficial do protocolo de abertura ou SDK homologado
- [ ] Provisionamento de credencial temporária por hóspede
- [ ] Comando de abertura autenticado
- [ ] Integração com BIS Hotel e reservas
- [ ] Auditoria, revogação e testes de segurança

## Regras do diagnóstico

1. A varredura apenas recebe anúncios BLE. RSSI não comprova identidade.
2. Inspeção GATT apenas lista serviços, características, propriedades e handles. **Não lê valores** e não escreve.
3. Não tentar códigos PIN por força bruta, comandos de abertura aleatórios, reset de firmware ou alteração elétrica de pinos.
4. Pareamento, emissão e revogação de credenciais devem usar método oficialmente autorizado.
5. Nunca salvar PIN, chaves criptográficas ou credenciais de hóspedes no GitHub.
6. A ferramenta não prova compatibilidade com VSKey, TTLock nem outro protocolo.

## Dados para homologação com Saga/VEGA

Solicitar ao fabricante:

- Nome e referência do produto e módulo de comunicação HB2546, versão de firmware.
- Se o módulo foi provisionado para Bluetooth e quando ele anuncia rádio.
- Protocolo/SDK Android, iOS e/ou Windows; UUIDs e formato das mensagens autorizadas.
- Fluxo de autenticação: chaves temporárias, sessão, anti-replay, expiração e revogação.
- API de emissão de chave digital para reservas e eventual licenciamento.
- Dependência de internet e tratamento de horários/relógios.
- Possibilidade de coexistência com cartão MIFARE/BIS Hotel e procedimento seguro de recuperação.

## Procedimento físico de diagnóstico

1. Testar em uma fechadura de bancada ou apartamento desocupado, com cartão físico reserva.
2. Confirmar módulo alimentado e próximo do computador Windows com Bluetooth BLE.
3. Executar SagaBleDiagnostico.exe e aguardar 20 segundos.
4. Identificar possíveis módulos por RSSI, nome e UUID; repetir aproximando o computador do módulo.
5. Selecionar um dispositivo e consultar serviços GATT. A consulta pode acionar uma conexão BLE, mas não envia comando de destravamento.
6. Exportar JSON na pasta local e compartilhar apenas por canal privado após revisar dados pessoais/identificadores.
7. Se nada anunciar: verificar BLE habilitado, driver, modo de repouso, necessidade de ativação e fonte de energia. Não presumir defeito.

## Arquitetura futura de abertura

Aplicativo Android (Kotlin) + backend de credenciais vinculado à reserva + SDK/protocolo homologado.
O aplicativo deve receber apenas a credencial necessária, com validade e escopo limitados, de modo a evitar comando fixo reutilizável. Revogação e fim da estadia precisam ser validados em teste de campo. Não haverá código de abertura inventado até obtenção de documentação autorizada.
