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

Na execução no Render em 2026-11-10 15:30 UTC, o transporte protegido observou NFS-e `216.198.79.131,64.29.17.131` e ECD `216.198.79.195,64.29.17.195`. O diagnóstico `A2A_DESTINATION_ADDRESS_REJECTED` registrou somente ID público do agente, hostname configurado e resposta DNS. Esses endereços públicos foram acrescentados individualmente após observar o bloqueio; nenhuma faixa foi liberada e a requisição recusada não transmitiu documentos.

Na rodada seguinte o Render observou ambos em `216.198.79.67,64.29.17.67`. Consultas Google Public DNS com contexto EDNS das redes de saída autorizadas confirmaram para NFS-e os pares terminados em .195 (sem contexto regional), .67 (74.220.49.0/24) e .3 (74.220.57.0/24); para ECD confirmaram .131 (sem contexto e 74.220.57.0/24) e .67 (74.220.49.0/24). Cloudflare confirmou os pares .194 NFS-e e .2 ECD. Somente respostas efetivamente observadas por agente foram acrescentadas; não presumimos que todos os IPs de um domínio se aplicam ao outro.

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
- Regressão final após diagnóstico DNS: 93 testes .NET aprovados, zero falhas, 36 s; 12 testes JavaScript de voz aprovados. Casos adicionais de política de saída/privacidade/descoberta: 10 aprovados; bloqueio de IP não autorizado confirmado.
- Navegador real, Web local sem DemoMode: ambos os agentes disponíveis no catálogo; envio de TXT fictício; duas contribuições exibidas e expandidas, ECD concluído e NFS-e parcial com achado e evidências. Execução UI `395c055c-bf29-4d00-84a3-01e2d300abf1`. Não acionada síntese OpenAI. Prévia encerrada após o teste.

## Render e pendências

No serviço `srv-dbphv12jnfac739g79o0` do workspace confirmado foram cadastrados por merge: segredo de saída SOFIA_A2A_TOKEN, token independente Sofia__ServiceToken e seu hash, cliente sofia-web no índice 1000, quatro permissões, capacidade analisar_documentos, IncludeStudentAgents=true e MongoDatabase=sofia-a2a. Valores sensíveis não são reproduzidos aqui. Configurações existentes de acesso, RAG e OpenAI preservadas.

O Atlas inicialmente recusou MCP porque ambas as organizações bloqueavam clientes de IA. O responsável habilitou “Allow users to connect AI clients” no painel aberto no Codex, e o conector passou a acessar a organização `5df510ddcf09a270a619c94d`.

Criado projeto separado `SOFIA-INFIS` (`6af335cc9aa190c17fa1f8de`), cluster gratuito `sofia-a2a`, AWS US_EAST_1, MongoDB 8.0.37. Usuário `sofia_app` restrito a readWrite no banco `sofia-a2a` e ao cluster. URI com TLS armazenada somente em `A2A__MongoConnectionString` no Render; habilitados `A2A__Enabled=true` e `Sofia__UseSpecialistAgents=true`. Nenhuma pendência de cadastro de variáveis no painel.

No Atlas foram autorizadas somente as redes de saída `74.220.49.0/24` e `74.220.57.0/24`, verificadas no painel do serviço Render. A revisão automática inicialmente rejeitou a ampliação de acesso por serem redes compartilhadas; o responsável aprovou explicitamente essas duas redes após explicação do alcance. Não foi liberado 0.0.0.0/0. TLS e autenticação do usuário limitado continuam obrigatórios. As organizações/projetos anteriores foram preservados.

## Validação no Render concluída para API e agentes

Revisão runtime `c678f2016110b775d75e2f586a92ff0497ab2091`, deploy `dep-dbpjjv2jnfac739kakvg`, confirmado live em 2026-11-10 15:37:22 UTC. Comando `./scripts/Test-SofiaCloudA2A.ps1` executado contra `https://sofia-infis.onrender.com`, com duas credenciais de entrada distintas da chave compartilhada de saída. Credenciais recebidas por prompt protegido.

Às 15:38:46 UTC, o teste confirmou:

- Sem token de serviço ou com token incorreto: 401 mesmo com a chave externa de acesso correta.
- Catálogo autenticado retornou ambos elegíveis com capacidade analisar_documentos. A inicialização de armazenamento estava pronta, não apenas health do RAG.
- POST/consulta de execução e consulta de cada análise funcionaram no serviço publicado, com persistência no Atlas. Texto ECD e XML NFS-e inteiramente sintéticos.
- Execução `c5becf4b-a487-441c-864e-5b340043007b`, estado agregado parcial, resultados de ambos aceitos pelo cliente Task/Artifact e pelo validador de domínio da SOFIA.
- ECD: status parcial, 1 achado, idSolicitacao `b9854091-e4b7-4175-a438-6a357326c18e`.
- NFS-e: status parcial, 2 achados, idSolicitacao `bcef2aae-0c78-4d6e-a855-8b1840f8241e`.
- Ambos preservaram idExecucao/idSolicitacao, identidade e referências documentais. Parcial é resultado de domínio válido para a fixture mista; não foi tratado como falha técnica nem certificado de conformidade.
- Segunda execução sintética `89e3ef5b-2566-43d8-b518-444b2fba3f79`: cancelada, registroConfirmado=true. Permissões de listagem, execução, consulta e cancelamento verificadas no Render.
- Sem novos eventos A2A_STORAGE_UNAVAILABLE ou DESTINATION_ADDRESS_REJECTED após a revisão de DNS na janela final consultada.

Smoke público adicional `Test-SofiaCloud.ps1 -TestVoiceAssets`: health200, anônimo401, frontend autenticado200, conhecimentos200, Swagger válido e módulos de voz publicados. Não houve chamada OpenAI. API e frontend locais reconstruídos/iniciados no Docker, volumes preservados; sete serviços em execução.

## Limite da verificação visual pública

O catálogo e os resultados foram verificados em navegador real no frontend local usando os agentes publicados, conforme evidência acima. A navegação automatizada ao frontend público encontrou a autenticação HTTP Basic; a tentativa de usar o Chrome já autenticado foi interrompida pelo controle de computador porque não conseguiu determinar a URL ativa com confiança. Nenhuma proteção foi removida para automatizar esse acesso. Portanto, o HTTP do frontend público e a API/cloud A2A estão validados, mas a inspeção visual interativa da versão pública permanece pendente. Não confundir isso com pendência de configuração no Render.

Novos IPs da Vercel ainda podem exigir revisão futura da lista exata. Para novos alunos, seguir quickstart e manter IDs únicos, referências de credencial, revisão de card/capacidades e testes externos. A capacidade genérica atual envia os documentos a ambos os especialistas; distinguir formatos requer evolução explícita do roteamento. A resposta ECD exibiu referência a validação SAMPLE: a integração técnica não prova a implementação de todas as regras ECD pelo aluno.
