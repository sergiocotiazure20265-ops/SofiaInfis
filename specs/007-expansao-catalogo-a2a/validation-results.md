# Validação — expansão do catálogo A2A

Data: 2026-11-10. O diagnóstico anterior do chat foi interrompido conforme pedido. Nesta entrega foram cadastrados os seis novos agentes, preservados NFS-e e ECD e as configurações explícitas existentes. Não houve alteração de tokens, RAG, esquema Mongo, CORS ou contratos de domínio.

## Cards e autenticação

Os seis cards retornaram 401 sem token e com token inválido, e 200 com a chave compartilhada. Confirmados nomes exatos, protocolo 0.3.0, JSONRPC, HTTPS, segurança Bearer e skills. O agente de legislação declara analisar_documentos e monitorar_legislacao_tributaria; a segunda capacidade foi acrescentada ao cliente sofia-web já existente no Render, preservando seus tokens e quatro permissões.

O card ECF inclui adicionalmente supportedInterfaces.protocolVersion=1.0, embora o perfil principal seja 0.3.0; a implementação atual usa o perfil principal. Recomenda-se ao aluno alinhar essa declaração adicional. O card NF-e declara textualmente que apenas o card está publicado e message/send está indisponível.

AllowedAddresses contém somente endereços públicos efetivamente observados por domínio em consultas DNS locais e Google Public DNS com contexto das duas redes Render autorizadas. Arquivos de evidência local em artifacts/new-student-agent-cards.json e artifacts/eight-agents-dns.json; não contêm a chave. A rotação DNS pode exigir novas revisões; nenhum wildcard/CIDR nem bypass foi introduzido na política de destinos A2A.

## Código e regressão

- Catálogo com oito IDs únicos; caminho NF-e preservado exatamente como /a2a/auditoria-nfe.
- Descoberta agora limitada a MaxConcurrentCalls=3, com ordem estável de registry, timeout individual e cancelamento. Mantidas as validações de URL, DNS, TLS, identidade, segurança e autorização de capacidades.
- Teste determinístico com oito servidores e barreira confirmou três consultas simultâneas, sem exceder o limite e sem reordenar resultados.
- Build aprovado, 0 erros; avisos NU1902/NU1903 preexistentes preservados.
- Regressão completa: 94 testes aprovados, zero falhas, 34 s. Grupo catálogo/descoberta/gateway real com agentes simulados: 7 aprovados, 5 s.

## Execução externa real, host SOFIA local

Comando ./scripts/Test-StudentAgents.ps1, com Mongo isolado e somente TXT ECD/XML NFS-e sintéticos. Um teste aprovado, 35 s. Aprovação significa que os agentes anteriores continuam funcionais e que o relatório individual foi produzido; não representa aprovação de todos os novos agentes.

Execução 565a0e9d-9b02-464f-b51c-e8c3bde48b6c, 15:58:51 UTC, estado agregado parcial. Todos os oito cards ficaram elegíveis na descoberta. Resultados reais passaram pelo cliente Task/Artifact e validador de domínio, com IDs de execução/solicitação e identidade preservados quando aceitos.

| Agente | Card Bearer | RPC tasks/get sintético | Resultado message/send |
|---|---|---|---|
| Parecer Tributário | 200 | 200 | Resultado de domínio status erro, contrato válido; fixture não comprova obrigações previamente validadas |
| ECF | 200 | 200 | Resultado de domínio status erro, contrato válido; fixture não é uma ECF integral |
| Classificação Fiscal | 200 | 200 | Resultado de domínio status erro, contrato válido; fixture fora da especialidade |
| Auditoria NF-e | 200 | 404 | Falha técnica AGENT_HTTP_FAILURE; publicar a execução no RPC informado |
| DIMP | 200 | 404 para ID inexistente | Falha técnica RESULT_CORRELATION_MISMATCH; alinhar IDs e identidade do resultado ao pedido e card |
| Legislação/Jurisprudência | 200 | 200 | Resultado concluido, contrato válido, zero achados na fixture |
| ECD existente | 200 | 200 | parcial, 2 achados, contrato válido |
| NFS-e existente | 200 | 200 | parcial, 2 achados, contrato válido |

Todos os RPCs rejeitaram sem chave e chave inválida com 401. HTTP 404 em tasks/get de tarefa inexistente não prova falta de message/send; DIMP efetivamente respondeu à análise, mas o domínio foi rejeitado. Não adaptar a SOFIA para aceitar IDs ou nomes divergentes. Não foram enviados documentos reais e não foi chamada síntese OpenAI.

## Publicação

Capacidade monitorar_legislacao_tributaria já autorizada no ambiente do Render por merge, sem substituir variáveis existentes. Publicação da revisão do catálogo ampliado e validação a partir do Render ainda serão registradas após o deploy. Os bloqueios remotos de NF-e/DIMP não serão apresentados como integrações funcionais.
