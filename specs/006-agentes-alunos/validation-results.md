# Evidências — agentes publicados dos alunos

Data: 2026-11-10. Somente documentos sintéticos; nenhuma chamada a OpenAI nesta validação. A síntese opcional não foi acionada. Resultados descrevem a integração técnica e não certificam conformidade fiscal/contábil dos documentos nem completude das regras dos agentes.

## Inspeção e mudanças

Lidas as três skills A2A do projeto e suas referências, constituição 1.0.0 e implementação de opções, registry, descoberta, credenciais, autorização, política exata de URL/IP, cliente JSON-RPC, workers/Mongo, endpoints e gateway Blazor. Nenhuma proteção de destino foi desativada. O catálogo aditivo usa a flag `A2A:IncludeStudentAgents`; entradas explícitas existentes prevalecem por ID. Credenciais referenciadas por nome de variável, sem valores versionados.

Corrigido o prazo fixo de 10 s do catálogo no gateway para prazo configurável de 30 s, limitado a 5–120 s. Falha de pinning encapsulada em HttpRequestException mantém o código técnico sanitizado em vez de aparecer somente como card indisponível. Catálogo distribuído tanto na API quanto no host Web integrado.

## DNS e descoberta remota

Consultas A e AAAA executadas a partir deste computador. AAAA não retornou endereços. Todos os IPs abaixo foram efetivamente observados; não inferidos por faixa:

| Agente | Primeira consulta A | Consulta posterior A |
|---|---|---|
| nfse-validador | 216.198.79.130, 64.29.17.130 | 216.198.79.194, 64.29.17.194 |
| agente-validacao-ecd | 216.198.79.2, 64.29.17.2 | 216.198.79.130, 64.29.17.130 |

Na primeira tentativa a lista inicial ficou desatualizada durante a rotação DNS, e o módulo bloqueou as conexões. Após confirmar e acrescentar os endereços novos, o fluxo real passou. Novos endereços/regiões exigem revisão; o teste local não comprova o DNS do Render.

O primeiro card NFS-e ainda declarava apiKey/X-Api-Key. Durante o trabalho, o deploy externo mudou para Bearer; a nova rodada passou sem mudar a política de autenticação da SOFIA. Na verificação final ambos retornaram protocolo 0.3.0, JSONRPC, nome exato, RPC HTTPS esperado, modos text/plain, skill analisar_documentos e segurança HTTP Bearer reconhecida pela implementação.

## Autenticação remota

GET card e POST tasks/get com ID inexistente sintético, usando o transporte protegido da própria SOFIA. Nenhum documento nessas sondagens.

| Agente | Card sem token / inválido / correto | RPC sem token / inválido / correto |
|---|---|---|
| NFS-e | 401 / 401 / 200 | 401 / 401 / 200 |
| ECD | 401 / 401 / 200 | 401 / 401 / 200 |

O HTTP 200 do tasks/get indica autenticação aceita, não existência da tarefa. A análise foi validada separadamente pelo fluxo abaixo.

## Execução ponta a ponta com serviços externos reais

Comando: `./scripts/Test-StudentAgents.ps1`, após compilar e iniciar Mongo/PgVector isolados. Gateway LiveSofiaChat → API real em TestServer → Mongo descartável → descoberta HTTPS e RPC Vercel reais → Task/Artifact → validação de domínio → consulta do resultado pelo gateway. Não há agentes remotos simulados neste teste; o host SOFIA ainda é local.

Rodada com TXT ECD e XML NFS-e fictícios, 14:59 UTC: 1 teste aprovado, 22 s. Execução `9dc09616-3f33-4237-8972-faf7910fb374`, estado agregado parcial.

| Agente | Descoberta | Task/Artifact/contrato | Resultado | idSolicitacao |
|---|---|---|---|---|
| agente-validacao-ecd | Elegível | Aceitos pelo cliente e validador existentes | concluido, 0 achados | 864e976d-bc46-4695-92cc-98a6cd5f5ea2 |
| nfse-validador | Elegível | Aceitos pelo cliente e validador existentes | parcial, 2 achados | ed202ee4-fb34-4e5f-a040-757190daad24 |

Os resultados preservaram o idExecucao acima e idSolicitacao distintos, identidade esperada e referências documentais válidas. Task completed com Artifact `analise-sofia-v1`, TextPart JSON e versão de domínio 1.0 são requisitos do cliente/validador real utilizados; retornos inválidos não seriam expostos como análises.

NFS-e recebe também o TXT ECD porque ambos declaram a capacidade genérica analisar_documentos. O resultado parcial e apontamentos sobre documento fora do formato são esperados; não filtramos documentos silenciosamente. O ECD respondeu sem achados na fixture, o que não demonstra cobertura integral das regras ECD. Se a turma quiser roteamento por formato/especialidade, será necessária capacidade/metadata mais específica e especificação adicional.

## Regressão e UI

- Build .NET aprovado, 0 erros. Avisos NU1902/NU1903 preexistentes em Pkcs 9.0.18 permanecem.
- Suíte completa sem opt-in externo: 92 aprovados, 0 falhas, 50 s. Inclui dois agentes simulados, sucesso parcial, autenticação/permissões/ownership, correlação, cancelamento, persistência e regressão RAG/Web.
- Após ajustes finais: 12 testes de catálogo, segurança e Web aprovados, 8 s.
- Navegador real, Web local sem DemoMode: ambos os agentes disponíveis no catálogo; envio de TXT fictício; duas contribuições exibidas e expandidas, ECD concluído e NFS-e parcial com achado e evidências. Execução UI `395c055c-bf29-4d00-84a3-01e2d300abf1`. Não acionada síntese OpenAI. Prévia encerrada após o teste.

## Render e pendências

No serviço `srv-dbphv12jnfac739g79o0` do workspace confirmado foram cadastrados por merge: segredo de saída SOFIA_A2A_TOKEN, token independente Sofia__ServiceToken e seu hash, cliente sofia-web no índice 1000, quatro permissões, capacidade analisar_documentos, IncludeStudentAgents=true e MongoDatabase=sofia-a2a. Valores sensíveis não são reproduzidos aqui. Configurações existentes de acesso, RAG e OpenAI preservadas.

O Atlas recusou MCP porque ambas as organizações bloqueavam clientes de IA. Aberto no navegador interno o painel Organization Settings → App Connections → AI Clients; opção “Allow users to connect AI clients” localizada para o responsável habilitar.

Pendente: URI de Mongo Atlas persistente com usuário limitado/rede autorizada em `A2A__MongoConnectionString`; após validar armazenamento, definir `A2A__Enabled=true` e `Sofia__UseSpecialistAgents=true` e publicar. As flags permanecem desligadas enquanto faltar essa dependência. Não usar URI local ou disco efêmero do Render como substituto.

Ainda pendentes os testes de DNS/descoberta/RPC a partir do Render, consulta/cancelamento e interface da versão publicada. A integração cloud não é declarada concluída com base nas evidências locais acima.
