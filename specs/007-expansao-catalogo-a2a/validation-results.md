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

Sondagem sintética adicional no DIMP confirmou Task completed e Artifact analise-sofia-v1, preservando ambos os IDs e agente.idAgente=agente-dimp. A divergência exata está em agente.nome: o resultado traz `Agente Fiscal DIMP`, enquanto card/catálogo exigem `Agente Fiscal DIMP INFIS`. O aluno deve alinhar o nome do resultado ao card; a SOFIA mantém a validação estrita.

## Publicação

Capacidade monitorar_legislacao_tributaria autorizada no ambiente do Render por merge, sem substituir variáveis existentes ou tokens. Revisão runtime `0a6fbae56e3c306de62efaa6552c4f573264c639`, deploy `dep-dbpk1majnfac739lamj0`, confirmado live em 2026-11-10 16:06:38 UTC. A revisão principal 49df8b8 ampliou o catálogo; a revisão seguinte acrescentou respostas DNS públicas verificadas, incluindo o par 216.198.79.3/64.29.17.3 observado para ECD pelo próprio Render.

Teste `./scripts/Test-SofiaCloudA2A.ps1 -AllowPartial`, concluído às 16:09:30 UTC, contra a instância pública do Render e Atlas persistente. O script valida que todos os oito IDs estão no catálogo, rejeição de credenciais de entrada ausentes/incorretas, criação, consulta, contrato/correlação de cada resultado e cancelamento separado. AllowPartial apenas registra as falhas individuais; não enfraquece a política do servidor nem converte falha em análise.

Execução `2c2ea517-a8ae-4657-b488-366717529994`, estado agregado parcial:

| Agente | Resultado no Render | idSolicitacao |
|---|---|---|
| ECF | contrato válido, domínio erro | cd30ff75-c508-432e-99cf-7de7c849e861 |
| Classificação Fiscal | contrato válido, domínio erro | 7091cf87-b6af-4190-b698-deb3e5a7f759 |
| Legislação/Jurisprudência | contrato válido, domínio parcial | 2150e01d-2c01-4b73-be75-1f5af87fa41b |
| Parecer Tributário | contrato válido, domínio erro | 2e053ece-2792-4e2a-9412-a8dff13bb7ea |
| ECD existente | contrato válido, domínio parcial | 4caacd8b-3b9e-4498-9ca4-d7adcd1b4eda |
| NFS-e existente | contrato válido, domínio parcial | 89a21760-0e1d-44fa-aa9d-9892840ece7f |
| Auditoria NF-e | AGENT_HTTP_FAILURE, sem análise aceita | registrado como falha individual |
| DIMP | RESULT_CORRELATION_MISMATCH, sem análise aceita | registrado como falha individual |

Todos os seis resultados aceitos preservaram o idExecucao acima, IDs de solicitação distintos, identidade e referências documentais. O cliente e validador existentes exigem Task, Artifact analise-sofia-v1 e domínio 1.0. Segunda execução `95477508-fa32-45d1-9993-110df72bd268` cancelada com registroConfirmado=true.

Houve timeout transitório de descoberta ECD numa rodada e legislação na listagem inicial da rodada final, sob o timeout individual vigente de cinco segundos. O início de execução redescobriu o catálogo e ambos retornaram resultados válidos. O relatório preserva a falha transitória da listagem e os resultados posteriores; não representa uma descoberta estática garantida para todos os agentes em qualquer instante.

## Insumos de negócio e correções necessárias

Consulta adicional somente de resultados sintéticos já gravados, sem nova execução:

- ECF: erro `nenhuma_ecf`. A fixture ECD/XML não é uma ECF; a primeira linha esperada começa com |0000|LECF|. Comunicação e contrato estão validados, regras fiscais completas não foram exercitadas.
- Parecer Tributário: erro `LOTE_AUSENTE`; nenhum documento contém JSON lote.v1. É necessário fornecer lote estruturado previamente validado na origem, por exemplo como texto em TXT. Não afirmar que uma nota avulsa basta para produzir o parecer.
- Classificação Fiscal: erro `contexto-ausente`; exige contexto.empresa, uf, regimeTributario, dataVigencia, papel, finalidade e tipoOperacao. A orquestração atual não expõe esses campos específicos ao chat. A comunicação funciona, mas uma classificação utilizável pelo fluxo atual depende de evolução compatível dos campos de contexto ou adaptação do agente. Não inventar esses valores.
- Auditoria NF-e: publicar message/send no endpoint informado, hoje sem execução funcional. O próprio card anuncia somente descoberta publicada.
- DIMP: corrigir agente.nome do resultado para Agente Fiscal DIMP INFIS, igual ao card. Os IDs foram preservados; não é necessário enfraquecer a correlação na SOFIA.

Os oito agentes estão cadastrados/publicados. Quatro dos seis novos retornaram contrato válido no Render; isso não significa que os seis executem análises de negócio utilizáveis com qualquer arquivo. Nenhum documento real foi usado. O diagnóstico anterior do chat permaneceu interrompido e não foi incluído nesta alteração.
