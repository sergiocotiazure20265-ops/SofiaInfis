# SOFIA — INFIS

Orquestrador de agentes de IA da INFIS, desenvolvido em **ASP.NET Core / .NET 10**.
Atualmente, a API responde perguntas com OpenAI, consulta conhecimentos por
similaridade no PostgreSQL/PgVector e extrai texto de documentos PDF, TXT e XML.
A integração com agentes especializados via A2A 0.3.0 está implementada em módulo
opcional, desativado por padrão, com testes locais de contrato e integração.

Os agentes publicados NFS-e e ECD possuem cadastro aditivo e validação externa
com documentos sintéticos. Para habilitação segura, credenciais e MongoDB,
consulte o [guia dos agentes dos alunos](specs/006-agentes-alunos/quickstart.md)
e as [evidências e pendências de publicação](specs/006-agentes-alunos/validation-results.md).
O catálogo foi ampliado para oito agentes na [entrega 007](specs/007-expansao-catalogo-a2a/spec.md),
com descoberta paralela limitada e [resultados individuais](specs/007-expansao-catalogo-a2a/validation-results.md).

## Execução integrada (RAG, API e frontend)

A execução atual usa a API RAG/OpenAI tanto para perguntas livres quanto para
anexos **PDF, TXT e XML**, sem chamadas a outros agentes. O módulo especializado
permanece opcional. A base existente é preservada; extensão, tabela e índice
vetorial são preparados automaticamente no início da API no Docker.

Com Docker Desktop iniciado e a chave OpenAI configurada em
`SofiaInfis.Api/appsettings.json`, no ambiente `SOFIA_OPENAI_API_KEY` ou no `.env`:

```powershell
./scripts/Start-Sofia.ps1
```

O script constrói e sobe todos os serviços e verifica a API e a interface.
As credenciais locais não são incluídas nas imagens. Em clone novo, copie
`.env.example` para `.env`, preencha os valores e use `docker-compose.example.yml`
como `docker-compose.yml` antes de executar o script.

- Chat: [http://localhost:5275](http://localhost:5275).
- Alimentação da base: [Conhecimentos](http://localhost:5275/conhecimentos).
- API: [Swagger](http://localhost:5151/swagger); prontidão: `/health`.
- Novo endpoint: `POST /api/v1/conhecimentos/ingest/arquivo`, multipart `titulo`,
  `fonte`, `arquivo`. Aceita PDF textual, TXT e XML, até 5 MB e 20.000 caracteres.
  Não ingere documentos truncados. O endpoint JSON `/ingest` continua disponível.
- Anexos no chat contextualizam a pergunta; não alimentam a base automaticamente.
- As referências já presentes na base ficam disponíveis para busca e respostas.
- XML preserva nomes de campos, atributos e namespaces; DTD/entidades externas
  são recusados. PDF escaneado precisa de OCR externo.
- `Sofia:UseSpecialistAgents=false` e `A2A:Enabled=false` na stack atual.
  Para a integração futura, habilite ambas as opções e configure catálogo/token.
- `Sofia:AllowInternalHttp=true` autoriza a URL fixa `http://api:8080` no Docker;
  fora dessa rede local, mantenha HTTPS e a opção desativada.
- Para SDK sem Docker na API, configure `Rag__InitializeDatabase=true` e as
  conexões locais antes de `dotnet run`.

Detalhes desta entrega: [especificação 003](specs/003-rag-integrado/spec.md).
As seções A2A abaixo documentam o módulo opcional e sua infraestrutura.

## Publicação em nuvem

A aplicação foi publicada no Render Free, com Blazor e API no mesmo processo e
35 conhecimentos preservados no Neon/PgVector:

- [Frontend SOFIA](https://sofia-infis.onrender.com/).
- [Swagger da API](https://sofia-infis.onrender.com/swagger).
- [Base de conhecimentos](https://sofia-infis.onrender.com/conhecimentos).

A interface abre diretamente, sem usuário, senha ou popup do navegador. A API
continua protegida por `Deployment__AccessKey` no cabeçalho `X-Sofia-Access-Key`,
enviado pelo servidor Blazor. Segredos não são incluídos no repositório.
O plano gratuito pode hibernar por inatividade; OpenAI continua
consumindo a chave configurada. Consulte o [guia](docs/DEPLOY_GRATUITO.md) e a
[validação pública](specs/004-publicacao-gratuita/validation-results.md).

## Seleção de especialistas e rastreamento

A SOFIA seleciona agentes pela especialidade da pergunta e pelas assinaturas do
conteúdo dos arquivos, após validar disponibilidade e permissões A2A. A capacidade
genérica `analisar_documentos` não envia mais a solicitação a todos. Cada agente
recebe somente anexos compatíveis; uma pergunta específica sem anexo compatível
segue como contexto textual identificado. Sem correspondência, a SOFIA usa o RAG.

O catálogo e os resultados exibem nomes em caixa alta, desenvolvedor e avatar de
iniciais. O rastreamento mostra os agentes realmente chamados, motivo da seleção,
quantidade de documentos, estado e duração. Novos agentes precisam de perfis
`RoutingTerms`/`DocumentTerms` revisados e `Developer` no catálogo; extensões como
PDF/TXT/XML não bastam. O roteamento local usa regras explícitas de domínio, sem
classificador LLM ou custo adicional de classificação. Perguntas ambíguas podem
precisar mencionar a especialidade. Consulte a [entrega 008](specs/008-roteamento-especialistas/spec.md).

## Estado atual

| Capacidade | Estado |
|---|---|
| Interface Blazor .NET 10 | Implementada; apresentação, identificação e chat responsivo |
| Anexos no chat | PDF/TXT/XML enviados à API; delegação opcional |
| Entrada de voz | Ditado pt-BR, revisão antes do envio e fallback para texto |
| Resultados na interface | Análises individuais, síntese sob demanda e rastreamento expansível |
| Perguntas com OpenAI e contexto RAG | Implementada no código |
| Ingestão textual, chunking e embeddings | Implementada no código |
| Busca semântica PostgreSQL/PgVector | Implementada; esquema inicializado automaticamente no Docker |
| Extração de PDF textual, TXT e XML | Implementada |
| ID de execução, duração e tokens de chat | Implementados na resposta |
| Comunicação A2A com especialistas | Implementada por adaptador JSON-RPC 2.0 / A2A 0.3.0 |
| Microsoft Agent Framework | Pacotes presentes; sem pipeline funcional do framework |
| MongoDB | Histórico A2A, concorrência otimista, recuperação e retenção |
| SQL Server | Configurado; sem uso pela aplicação |
| Memória conversacional | Não implementada; histórico A2A não representa memória de chat |
| Autenticação e telemetria | Implementadas nas novas operações A2A |
| Testes automatizados | Unitários, contratos, integração e regressão; infraestrutura isolada |

O estado consolidado está na [arquitetura atual](docs/ARQUITETURA_SOFIA.md).
O [diagnóstico inicial](RELATORIO_ARQUITETURA_ATUAL_SOFIA.md) permanece como baseline
histórica. Especificações: [001 — A2A](specs/001-integracao-a2a/spec.md) e
[002 — Interface Blazor](specs/002-interface-blazor/spec.md).
As [telas](docs/INTERFACE_SOFIA.md) foram capturadas com dados de demonstração.

## Arquitetura

```text
Navegador → Blazor Interactive Server (SofiaInfis.Web)
  ├─ pergunta sem anexo → API perguntar → RAG + OpenAI
  ├─ PDF/TXT/XML → API perguntar → extração + RAG + OpenAI
  ├─ base de conhecimentos → ingestão textual/arquivo → embeddings → PgVector
  ├─ A2A opcional → API orquestrações → especialistas A2A → MongoDB
  └─ síntese sob demanda → API perguntar → RAG + OpenAI → resposta final
```

O frontend acompanha resultados pelo servidor, com a credencial fora do navegador.
As análises originais e o ID A2A são preservados; a síntese possui outro ID.

```text
Cliente → Controllers → SofiaService → extração PDF/TXT/XML
                                  → ConhecimentoService → embeddings OpenAI
                                                        → PostgreSQL/PgVector
                                  → ChatClient OpenAI → resposta
```

```text
Backend autenticado → AgentesController / OrquestracoesController
  → catálogo + descoberta de Agent Cards → fila limitada em memória
  → message/send → tasks/get (quando necessário) → Task / Artifact / JSON SOFIA 1.0
  → validação de correlação → MongoDB → consulta e consolidação pelo consumidor
```

As operações A2A recebem texto previamente extraído. Os três endpoints existentes
continuam com seus contratos e serviços originais.

| Operação nova | Permissão |
|---|---|
| GET `/api/v1/agentes` | `a2a:agents:read` |
| POST `/api/v1/orquestracoes` | `a2a:execute` |
| GET `/api/v1/orquestracoes/{idExecucao}` | `a2a:read` |
| GET `/api/v1/orquestracoes/{idExecucao}/chamadas/{idSolicitacao}/resultado` | `a2a:read` |
| POST `/api/v1/orquestracoes/{idExecucao}/cancelar` | `a2a:cancel` |

Use Bearer de serviço externo, cujo SHA-256 fica em `InboundClients.TokenHash`.
`ClientId` define propriedade do histórico, independentemente do nome do usuário.
Rotação pode usar outro hash para o mesmo ClientId; aplique a configuração em
reinício controlado, sem instâncias sobrepostas. Capacidades exigem autorização
explícita. As credenciais por agente são referências a variáveis de ambiente.

O [exemplo A2A](SofiaInfis.Api/appsettings.a2a.example.json) é documentação e não
é carregado automaticamente. Transfira a seção para configuração local ou use
variáveis `A2A__...`; substitua os placeholders antes de habilitar. HTTPS é
obrigatório fora de Development/Testing. Configure URLs exatas e IPs aprovados:
redirecionamentos, proxy e endereços DNS fora da lista são bloqueados. CORS só
se aplica às novas rotas e origens explicitamente autorizadas, sem cookies.

Operação inicial: **uma instância**, cinco execuções ativas, dez pendentes e três
chamadas simultâneas por execução. Descoberta 5 s, chamada 60 s, execução 120 s,
fechamento 5 s; envelope 1 MiB. Retenção 30 dias, índices TTL e limpeza periódica.
Mongo precisa permitir índices e escrita confirmada com journal. Proteja acesso,
backups e dados em repouso no ambiente. Execuções interrompidas são encerradas
sem reenvio automático; documentos e nome permanecem transitórios na fila.
Análises originais são persistidas. Consumo desconhecido permanece `null`.

Domínio SOFIA **1.0** e transporte A2A **0.3.0** são versões distintas. Os pacotes
existentes do Agent Framework foram preservados; modelos v1 do SDK não são usados
como wire 0.3.0. Contratos e exemplos: [contracts](specs/001-integracao-a2a/contracts/README.md).
Testes reproduzíveis: [fixtures](SofiaInfis.Api.Tests/Fixtures/README.md).

```text
SofiaInfis.slnx
SofiaInfis.Api/
  Controllers/   # Perguntas, conhecimentos, agentes e orquestrações
  DTOs/          # Contratos legados e domínio A2A
  Services/      # RAG/OpenAI, extração e módulo A2A
  Extensions/    # DI, OpenAI, Swagger e composição A2A
SofiaInfis.Web/
  Components/    # Apresentação, identificação, chat e rastreamento
  Services/      # Cliente real, demonstração e preparo de documentos
  wwwroot/       # Estilos, interação JS e artes da SOFIA
SofiaInfis.Api.Tests/ # Testes da API e do frontend
specs/           # Especificações 001 e 002
docs/            # Arquitetura atual e galeria da interface
.specify/        # Governança e workflow Spec Kit
.agents/skills/  # Skills e referências A2A do projeto
```

## Preparação local

A solução também inclui o frontend **Blazor .NET 10** em `SofiaInfis.Web`, com
apresentação da SOFIA, chat, arraste de PDF/TXT/XML, análises por agente, síntese final
e rastreamento. Para conhecer a interface sem serviços externos:

```powershell
dotnet run --project SofiaInfis.Web --launch-profile demo
```

Abra `http://localhost:5275`. O perfil de demonstração é identificado na interface
e usa respostas simuladas. Para conectar à API real, consulte o
[guia do frontend](SofiaInfis.Web/README.md). A credencial permanece no servidor
Blazor; não é necessário abrir CORS para essa comunicação.

Para a demonstração do frontend, basta o SDK .NET 10. Para os fluxos reais da API:
SDK .NET 10, infraestrutura configurada e acesso à OpenAI; análises A2A exigem
também MongoDB e especialistas autorizados disponíveis.
Execute os comandos PowerShell a partir da raiz da solução.

### 1. Criar configurações locais

Em um clone novo:

```powershell
Copy-Item SofiaInfis.Api/appsettings.example.json SofiaInfis.Api/appsettings.json
Copy-Item docker-compose.example.yml docker-compose.yml
Copy-Item .env.example .env
```

Se esses arquivos já existirem, preserve suas configurações locais. Edite `.env`
com senhas locais distintas e configure `ConnectionStrings:PostgreSQL` no
`appsettings.json` com a mesma senha do PostgreSQL. Os arquivos reais e `.env`
são ignorados pelo Git. Os arquivos `.example` não contêm credenciais reais.

Forneça a chave OpenAI por variável de ambiente no terminal da API:

```powershell
$env:OpenAI__ApiKey = 'SUA_CHAVE_OPENAI'
```

Não grave sua chave no README, em scripts compartilhados ou no histórico Git.
Modelos padrão do exemplo: `gpt-4.1-mini` e `text-embedding-3-small`.

### 2. Iniciar a infraestrutura

```powershell
docker compose up -d
docker compose ps
```

| Serviço | Endereço local |
|---|---|
| PostgreSQL | `localhost:5435` |
| pgAdmin | `http://localhost:5052` |
| MongoDB | `localhost:27018` |
| Mongo Express | `http://localhost:5053` |
| SQL Server | `localhost:1434` |

O Compose atualizado inicia bancos, ferramentas administrativas, API e frontend.
Use o script acima para carregar a credencial local da OpenAI.
MongoDB é consumido quando o módulo A2A é habilitado; SQL Server permanece sem uso.
Esses serviços destinam-se ao desenvolvimento local.

### 3. Preparar a base vetorial

O banco `bd-sofia-infis` precisa ter a extensão `vector` habilitada e a tabela
`conhecimentos` com as colunas `id`, `titulo`, `texto`, `fonte`, `indice_chunk` e
`embedding`, compatíveis com os comandos em `ConhecimentoService` e vetores de
1.536 dimensões. A API prepara o esquema e índice automaticamente quando
`Rag:InitializeDatabase=true`, como configurado no Compose. A inicialização é
idempotente e preserva os conhecimentos existentes.

### 4. Executar a API

```powershell
dotnet restore SofiaInfis.slnx
dotnet build SofiaInfis.slnx --no-restore
dotnet run --project SofiaInfis.Api --launch-profile http
```

Swagger: `http://localhost:5151/swagger`.
As novas operações A2A exigem autenticação Bearer efetiva. As três operações
legadas preservam a política de acesso anterior e não ganharam autenticação
obrigatória nesta feature.

## Endpoints existentes

| Método e rota | Entrada | Finalidade |
|---|---|---|
| `POST /api/v1/sofia/perguntar` | Multipart: `pergunta`, `arquivos` opcionais | Resposta com documentos e contexto RAG |
| `POST /api/v1/conhecimentos/ingest` | JSON: `titulo`, `texto`, `fonte` | Persistência de conhecimentos em chunks |
| `POST /api/v1/conhecimentos/buscar` | JSON: `pergunta`, `topK` | Recuperação semântica; `topK` padrão 3, máximo 20 |

Perguntas permitem até cinco anexos PDF/TXT/XML, 5 MiB por arquivo e 20 MiB por
requisição. A extração limita o texto de cada arquivo a 20.000 caracteres e
sinaliza truncamento. Anexos não são automaticamente ingeridos na base RAG.
Use o Swagger para consultar os contratos completos.

## Referências A2A

A turma adota contrato de negócio **SOFIA 1.0**, independente do perfil de transporte
**A2A 0.3.0 / JSON-RPC 2.0**. SOFIA atua como cliente/orquestrador e cada agente
especializado oferece seu servidor A2A. O fluxo implementado inclui nome do usuário,
pergunta, documentos textuais, análise estruturada e IDs de correlação.

As referências normativas são:

- [Constituição do projeto](.specify/memory/constitution.md).
- [Skill de contrato A2A](.agents/skills/sofia-a2a-contrato/SKILL.md).
- [Contrato SOFIA 1.0](.agents/skills/sofia-a2a-contrato/references/contrato-sofia-v1.md).
- [Skill de implementação A2A](.agents/skills/sofia-a2a-implementacao/SKILL.md).
- [Skill de conectividade e CORS](.agents/skills/sofia-cors-rest-a2a/SKILL.md).

Catálogo, descoberta, seleção, cliente A2A, autenticação, timeouts, histórico e
consulta estão implementados. O módulo A2A disponibiliza análises originais para
consolidação. No frontend, “Gerar síntese final” aciona o endpoint existente de
perguntas com as análises em TXT; essa operação usa RAG/OpenAI e pode consumir
tokens. A síntese depende de ação do usuário e não altera os resultados A2A.
Não há memória conversacional de LLM.
Agentes reais dos alunos precisam ser cadastrados e validados no ambiente da turma.

A [feature 005](specs/005-comando-voz/spec.md) adiciona entrada de voz ao compositor.
Clique no microfone, fale em português, revise e envie. A disponibilidade depende
do navegador e da permissão do microfone; a API continua recebendo somente texto.

## Contribuição e validação

Evolua o projeto com o fluxo Spec Kit: especificação, planejamento, tarefas,
implementação e validação. Preserve contratos e funcionalidades existentes;
alterações exigem justificativa e aprovação conforme a constituição.
Novas funcionalidades devem incluir testes automatizados.

Execute `dotnet test SofiaInfis.slnx` após preparar a infraestrutura de teste
descrita em [Fixtures/README.md](SofiaInfis.Api.Tests/Fixtures/README.md).
O arquivo `.http` residual não substitui a suíte. A validação integrada atual, com testes, containers, OpenAI e preservação da base,
está em [resultados 003](specs/003-rag-integrado/validation-results.md).
As evidências anteriores permanecem em [A2A](specs/001-integracao-a2a/validation-results.md)
e [frontend](SofiaInfis.Web/VALIDACAO.md).
