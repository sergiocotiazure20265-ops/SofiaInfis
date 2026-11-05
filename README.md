# SOFIA — INFIS

Orquestrador de agentes de IA da INFIS, desenvolvido em **ASP.NET Core / .NET 10**.
Atualmente, a API responde perguntas com OpenAI, consulta conhecimentos por
similaridade no PostgreSQL/PgVector e extrai texto de documentos PDF e TXT.
A integração com agentes especializados via A2A está definida como evolução do projeto.

## Estado atual

| Capacidade | Estado |
|---|---|
| Perguntas com OpenAI e contexto RAG | Implementada no código |
| Ingestão textual, chunking e embeddings | Implementada no código |
| Busca semântica PostgreSQL/PgVector | Implementada; depende de esquema externo |
| Extração de PDF textual e TXT | Implementada |
| ID de execução, duração e tokens de chat | Implementados na resposta |
| Microsoft Agent Framework e comunicação A2A | Pacotes presentes; integração pendente |
| MongoDB e SQL Server | Infraestrutura configurada; sem uso pela aplicação |
| Memória conversacional e histórico persistente | Não implementados |
| Autenticação efetiva e telemetria distribuída | Não implementadas |
| Testes automatizados | Ainda não presentes |

O detalhamento e as evidências estão no
[relatório de arquitetura](RELATORIO_ARQUITETURA_ATUAL_SOFIA.md).
Esses estados resultam de análise estática; não representam validação ponta a ponta.

## Arquitetura

```text
Cliente → Controllers → SofiaService → extração PDF/TXT
                                  → ConhecimentoService → embeddings OpenAI
                                                        → PostgreSQL/PgVector
                                  → ChatClient OpenAI → resposta
```

```text
SofiaInfis.slnx
SofiaInfis.Api/
  Controllers/   # Perguntas, ingestão e busca
  DTOs/          # Contratos HTTP existentes
  Services/      # SOFIA, conhecimentos, embeddings e extração
  Extensions/    # DI, OpenAI e Swagger
.specify/        # Governança e workflow Spec Kit
.agents/skills/  # Skills e referências A2A do projeto
```

## Preparação local

Requisitos: SDK .NET 10, Docker com Compose para a infraestrutura e acesso à OpenAI.
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

O Compose inicia bancos e ferramentas administrativas; não inicia a API.
MongoDB e SQL Server ainda não são consumidos pelo código.
Esses serviços destinam-se ao desenvolvimento local.

### 3. Preparar a base vetorial

O banco `bd-sofia-infis` precisa ter a extensão `vector` habilitada e a tabela
`conhecimentos` com as colunas `id`, `titulo`, `texto`, `fonte`, `indice_chunk` e
`embedding`, compatíveis com os comandos em `ConhecimentoService` e vetores de
1.536 dimensões. **Não há scripts de criação/migração no repositório.**
Criar os containers não conclui essa preparação; as operações RAG dependem dela.

### 4. Executar a API

```powershell
dotnet restore SofiaInfis.slnx
dotnet build SofiaInfis.slnx --no-restore
dotnet run --project SofiaInfis.Api --launch-profile http
```

Swagger: `http://localhost:5151/swagger`.
O esquema Bearer exibido no Swagger é apenas documental no estado atual;
a API ainda não implementa autenticação. Configure proteção antes de expô-la
fora do ambiente de desenvolvimento.

## Endpoints existentes

| Método e rota | Entrada | Finalidade |
|---|---|---|
| `POST /api/v1/sofia/perguntar` | Multipart: `pergunta`, `arquivos` opcionais | Resposta com documentos e contexto RAG |
| `POST /api/v1/conhecimentos/ingest` | JSON: `titulo`, `texto`, `fonte` | Persistência de conhecimentos em chunks |
| `POST /api/v1/conhecimentos/buscar` | JSON: `pergunta`, `topK` | Recuperação semântica; `topK` padrão 3, máximo 20 |

Perguntas permitem até cinco anexos PDF/TXT, 5 MiB por arquivo e 20 MiB por
requisição. A extração limita o texto de cada arquivo a 20.000 caracteres e
sinaliza truncamento. Anexos não são automaticamente ingeridos na base RAG.
Use o Swagger para consultar os contratos completos.

## Evolução A2A

A turma adota contrato de negócio **SOFIA 1.0**, independente do perfil de transporte
**A2A v0.3.x / JSON-RPC 2.0**. SOFIA será cliente/orquestrador e cada agente
especializado será servidor A2A. O fluxo previsto inclui nome do usuário,
pergunta, documentos textuais, análise estruturada e IDs de correlação.

As referências normativas são:

- [Constituição do projeto](.specify/memory/constitution.md).
- [Skill de contrato A2A](.agents/skills/sofia-a2a-contrato/SKILL.md).
- [Contrato SOFIA 1.0](.agents/skills/sofia-a2a-contrato/references/contrato-sofia-v1.md).
- [Skill de implementação A2A](.agents/skills/sofia-a2a-implementacao/SKILL.md).
- [Skill de conectividade e CORS](.agents/skills/sofia-cors-rest-a2a/SKILL.md).

Ainda faltam catálogo/discovery de agentes, cliente A2A, roteamento, consolidação,
autenticação, timeouts, histórico e testes de interoperabilidade. A presença dos
pacotes A2A não significa que essa integração já esteja em execução.

## Contribuição e validação

Evolua o projeto com o fluxo Spec Kit: especificação, planejamento, tarefas,
implementação e validação. Preserve contratos e funcionalidades existentes;
alterações exigem justificativa e aprovação conforme a constituição.
Novas funcionalidades devem incluir testes automatizados.

Este repositório ainda não possui suíte de testes. O arquivo `.http` existente
é um exemplo residual e não valida os endpoints SOFIA. Documente os comandos e
resultados reais de build/testes em cada entrega.
