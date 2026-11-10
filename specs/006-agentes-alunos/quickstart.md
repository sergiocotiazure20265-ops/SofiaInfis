# Operação dos agentes dos alunos

Catálogo revisado: `SofiaInfis.Api/a2a-student-agents.json`. É distribuído também no host Web integrado. Para novos alunos, revisar card autenticado, RPC, identidade, skills e todos os IPs A/AAAA retornados. Acrescentar uma entrada única e testes; não liberar IPs por padrão de endereço. DNS da Vercel pode variar por tempo/região: confirmar também a partir do Render. Mudança de IP deve falhar fechada até nova revisão.

## Configuração do servidor Render

Serviço `sofia-infis`, workspace confirmado “Sergio's workspace”. Dashboard: https://dashboard.render.com/web/srv-dbphv12jnfac739g79o0/env

Não substituir a coleção de variáveis existente. Usar merge. Nenhuma alteração no RAG, chave de acesso externa ou OpenAI.

| Variável | Valor / origem |
|---|---|
| `SOFIA_A2A_TOKEN` | Segredo compartilhado informado pelo responsável, somente ambiente protegido |
| `A2A__IncludeStudentAgents` | `true` |
| `A2A__Enabled` | `true` somente após validar Mongo persistente |
| `Sofia__UseSpecialistAgents` | `true` após o módulo estar pronto |
| `A2A__MongoConnectionString` | URI Mongo Atlas com TLS e usuário limitado ao banco de orquestração |
| `A2A__MongoDatabase` | `sofia-a2a` (banco separado) |
| `Sofia__ServiceToken` | Token forte aleatório exclusivo do frontend servidor, independente de SOFIA_A2A_TOKEN |
| `A2A__InboundClients__N__ClientId` | `sofia-web` em índice N livre; preservar clientes existentes |
| `A2A__InboundClients__N__TokenHash` | SHA-256 hexadecimal de Sofia__ServiceToken |
| `A2A__InboundClients__N__Permissions__0` | `a2a:agents:read` |
| `A2A__InboundClients__N__Permissions__1` | `a2a:execute` |
| `A2A__InboundClients__N__Permissions__2` | `a2a:read` |
| `A2A__InboundClients__N__Permissions__3` | `a2a:cancel` |
| `A2A__InboundClients__N__Capabilities__0` | `analisar_documentos` |
| `Sofia__Capabilities__0` | `analisar_documentos` |
| `Sofia__CatalogTimeoutSeconds` | `30` |

O cliente Blazor envia Bearer e X-Sofia-Access-Key em chamadas servidor-servidor. O navegador usa o acesso externo existente; nunca recebe ServiceToken. Em implantação separada, definir `Sofia__ApiBaseUrl` para a API real; no Render integrado preservar o loopback existente.

Atlas: habilitar acesso de clientes IA/MCP na organização; criar/selecionar cluster gratuito, usuário readWrite somente no banco `sofia-a2a`, autorizar apenas a rede de saída do Render e fornecer a URI como segredo. Não usar Mongo dentro do filesystem efêmero do Render Free. Aplicar variáveis requer deploy; conferir readiness do módulo por catálogo autenticado, pois `/health` verifica RAG e não garante Mongo/A2A.

## Validação reproduzível

Iniciar containers descartáveis conforme `SofiaInfis.Api.Tests/Fixtures/README.md`, compilar e executar:

```powershell
dotnet build SofiaInfis.slnx
dotnet test SofiaInfis.slnx --no-build --filter 'FullyQualifiedName!~StudentAgentsLiveTests'
./scripts/Test-StudentAgents.ps1
```

O script pede o segredo sem exibição e o transmite aos processos via ambiente. Somente documento ECD/TXT e NFS-e/XML fictícios são enviados aos destinos previamente aprovados pelo módulo. Não aceita arquivo real por parâmetro. `artifacts/student-agents-live.json` guarda evidência sanitizada; arquivos em artifacts são ignorados pelo Git. O teste externo é ignorado em execuções normais para não depender de disponibilidade ou credenciais dos alunos.

No ambiente publicado, testar GET `/api/v1/agentes`, POST `/api/v1/orquestracoes`, GET execução/resultados e cancelamento de uma execução sintética. Usar os dois headers distintos acima. Confirmar no navegador catálogo, disponibilidade, contribuições individuais e estado da análise. Não declarar concluído só pelo teste local com Vercel.
