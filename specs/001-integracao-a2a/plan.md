# Implementation Plan: 001 — Integração A2A da SOFIA

**Branch**: `main` (Git real) | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: `specs/001-integracao-a2a/spec.md`
**Feature context**: `001-integracao-a2a`, retornado em BRANCH pelo setup como fallback
de contexto; nenhuma branch nova criada.
**Status**: implementação 001 concluída e validada localmente; este documento
preserva o desenho que orientou a entrega. Evidências: [validation-results.md](validation-results.md).
O frontend posterior está no [plano 002](../002-interface-blazor/plan.md).

## Summary

Adicionar descoberta autorizada de Agent Cards, seleção explícita de capacidades,
solicitações SOFIA 1.0 em A2A 0.3.0/JSON-RPC e análises estruturadas correlacionadas.
Novas operações iniciam, consultam e cancelam execução, com histórico MongoDB,
isolamento por chamador e preservação de sucesso parcial.

Os três endpoints e serviços atuais permanecem funcionais. Como o SDK restaurado
é A2A v1, usar adaptador v0.3 explícito sem alterar pacotes existentes.
Decisões: [research.md](research.md). Dados: [data-model.md](data-model.md).
Interfaces: [contracts](contracts/README.md).

## Technical Context

**Language/Version**: C# / .NET 10, nullable e implicit usings atuais.

**Primary Dependencies**: ASP.NET Core atual; HttpClient/System.Text.Json,
Channels, TimeProvider e autenticação do shared framework; MongoDB.Driver 3.13.0
e OpenTelemetry já declarados. Manter Microsoft.Agents.AI.A2A
1.27.0-preview.261030.1 sem usar modelos v1 no adapter v0.3.
OpenAI/Dapper/Npgsql/PdfPig permanecem nos fluxos atuais.

**Storage**: MongoDB 8.0, banco de histórico existente, novas coleções
a2a_execucoes/a2a_chamadas. PostgreSQL/PgVector não muda.

**Testing**: projeto isolado xUnit 2.9.3, runner 3.1.4, Test SDK 17.14.1,
Mvc.Testing 10.0.9, JsonSchema.Net 7.3.4 adicionados ao projeto de testes;
fixtures HTTP, Mongo de teste e relógio injetável. A 002 acrescentou testes do
cliente Blazor e integração com PgVector/OpenAI sintética.

**Target Platform**: API Windows/Linux em instância única sem sobreposição de processos;
Docker para infraestrutura/agentes de referência. O frontend não pertence ao
escopo original001; foi acrescentado pela 002 em projeto próprio.

**Project Type**: serviço web existente com módulo aditivo e projeto de testes.

**Performance Goals**: descoberta <=5 s/agente, chamada <=60 s incluindo polling;
execução <=120 s + fechamento <=5 s; aceite com cinco execuções simultâneas.

**Constraints**: três chamadas ativas por execução; envelopes <=1 MiB;
1–5 documentos, 1–20.000 caracteres/texto; sem retry automático de análise,
persistência de textos brutos ou alteração de contratos legados.
Retenção 30 dias; exclusão <=24 h da expiração.

**Scale/Scope**: cinco execuções ativas e dez pendentes no perfil inicial.
Todos os agentes elegíveis são considerados dentro do prazo; chamadas ainda
não iniciadas ao vencer recebem causa de prazo. Multinstância, síntese final,
memória conversacional e servidores reais dos alunos ficam fora.

## Constitution Check

*GATE: avaliação antes da pesquisa e reavaliação após desenho.*

| Princípio | Antes da pesquisa | Depois do desenho |
|---|---|---|
| I. Preservação | PASS: escopo aditivo | PASS: legados intactos, regressão US6 |
| II. .NET/arquitetura | PASS: mesma solução | PASS: pastas atuais, só projeto de testes adicional justificado |
| III. SOLID/DI | PASS: fronteiras por responsabilidade | PASS: interfaces isolam I/O e regras |
| IV. A2A | PASS: perfil fixado | PASS: adapter explícito 0.3 sem inferir suporte do SDK v1 |
| V. Entrada textual | PASS: contrato disponível | PASS: schema, fixtures, validação e limites |
| VI. Resultado/IDs | PASS: invariantes disponíveis | PASS: validação contextual e análises originais |
| VII. Segurança/falhas | PASS: nova auth necessária | PASS: políticas locais, destinos e prazos |
| VIII. Rastreabilidade | PASS: novo registro previsto | PASS: Mongo/CAS, métricas e consumo nullable |
| IX. RAG/memória | PASS: preservar estado real | PASS: sem tocar RAG/extração ou inventar memória |
| X. Testes | PASS: obrigação incorporada | PASS: quatro níveis, não declarados executados |
| XI. Skills | PASS: três referências lidas | PASS: contrato, implementação e HTTP incorporados |
| XII. Aprovação | PASS: sem mudança antiga proposta | PASS: desenho aditivo, sem reestruturação incidental |

Nenhuma exceção constitucional. Alterar função antiga, perfil ou dependência de
produção exigirá justificativa/aprovação própria. Planejar a feature não autoriza
mudanças incidentais. O projeto de testes é necessário para o princípio X e não
reorganiza projetos de produção.

## Project Structure

### Documentation (this feature)

```text
specs/001-integracao-a2a/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── checklists/requirements.md
└── contracts/
    ├── README.md
    ├── sofia-v1.schema.json
    ├── orquestracao.openapi.json
    └── examples/
```

tasks.md será produzido somente por $speckit-tasks.

### Source Code (repository root)

Estrutura proposta, ainda não criada:

```text
SofiaInfis.Api/
├── Controllers/
│   ├── SofiaController.cs                 # preservar
│   ├── ConhecimentosController.cs         # preservar
│   ├── OrquestracoesController.cs          # novo
│   └── AgentesController.cs                # novo
├── DTOs/A2A/
├── Services/A2A/
│   ├── Abstractions/
│   ├── Domain/
│   ├── Transport/
│   ├── Persistence/
│   ├── Security/
│   └── Workers/
├── Extensions/A2AConfigExtension.cs
└── Program.cs                             # apenas ligação aditiva
SofiaInfis.Api.Tests/
├── Unit/
├── Contract/
├── Integration/
├── Regression/
└── Fixtures/
```

**Structure Decision**: um projeto de produção. Application/Domain/Infrastructure/API
são responsabilidades lógicas nas pastas, não novos projetos obrigatórios.
O projeto de testes isola dependências de desenvolvimento.

### Fronteiras e DI

| Componente planejado | Responsabilidade / ciclo |
|---|---|
| IAgentRegistry / ConfiguredAgentRegistry | Catálogo imutável validado, singleton |
| IAgentDiscovery / AgentDiscoveryService | Card/capacidades, scoped |
| IA2AAgentClient / JsonRpc03AgentClient | Wire HTTP 0.3, named/typed client |
| ISofiaAnalysisValidator | Domínio/correlação, sem I/O |
| IExecutionRepository / MongoExecutionRepository | CAS/consulta/TTL, scoped, MongoClient singleton |
| IOrchestrationService | Aceitar, consultar, cancelar, scoped |
| OrchestrationWorker | Fila; escopo DI por execução, hosted singleton |
| ExecutionCoordinator | Gate, ownerRunId, prazo e cancelamento |
| SofiaA2AServiceHandler | Token opaco, claims e políticas |
| A2AObservability | ActivitySource/Meter e logs sanitizados |

IAgentTaskHandler é responsabilidade do servidor especialista; aqui somente
fixture de teste. Não criar servidor A2A de produção da SOFIA.

### Fluxo e consistência

1. Autenticar/autorizar nova operação e validar body/volume antes de aceitar.
2. Reservar vaga limitada, gerar idExecucao e gravar em_andamento com ownerClientId.
3. Enfileirar payload transitório e retornar 202/Location. Falha de fila/registro
   gera erro sem disparar análise; não confirmar aceitação operacional.
4. Worker descobre cards no orçamento e cruza skills com capacidades solicitadas
   e permissões; sem classificar pergunta com LLM.
5. Criar idSolicitacao/registro antes de enviar; máximo três chamadas simultâneas.
6. Mapear domínio para Message/TextPart, acompanhar Task, validar Artifact/JSON,
   IDs/agente/documentos e persistir chamada por atualização condicional.
7. Aguardar gravações, calcular estado/expiração e fechar execução. Retorno tardio
   não altera terminal. Falha de gravação não é salvo.
8. Consulta autorizada devolve metadados e resultados paginados; não reanalisa.
   Cancelamento fecha localmente e tenta cancelar remoto dentro do prazo.
9. Reinício sem sobreposição reconcilia abertos; preserva terminais e não reenvia.

Mapeamento: operação local → DTO de orquestração → domínio SOFIA 1.0
→ Message/TextPart A2A → análise do agente → Task/Artifact/TextPart
→ ResultadoAnaliseSofia validado → armazenamento/consulta.
A API local não é apresentada como servidor A2A.

### Configuração e integração planejadas

Novo bloco A2A, Enabled=false por padrão, timeouts/limites da spec, Agents
(idAgente/cardUri/expectedName/destinos/permissões/credentialRef), InboundClients
(clientId/tokenHash/permissões/capacidades), fila, coleções, retenção e CORS opcional.
Segredos via ambiente, nunca DTO/Git.

Ligações aditivas em Program/DI sem remover registros atuais. Validar opções do
módulo quando habilitado. Desabilitado: 503 somente nas operações novas, sem
exigir Mongo/cards/segredos/origens para iniciar os fluxos antigos.
Filtro de recurso impede criar infraestrutura desabilitada; não aplicar fallback
de autorização global. Nenhuma configuração atual será sobrescrita.

Falha de validação das opções, inicialização de índices ou indisponibilidade Mongo
deve degradar somente o módulo novo para 503, com diagnóstico sanitizado. Workers
devem tratar essas falhas sem encerrar o host nem bloquear o startup/fluxo legado.
Não aplicar ValidateOnStart fatal global às opções novas.

### Rastreabilidade e estratégia de validação

| Requisitos | Evidência de implementação planejada |
|---|---|
| FR-001–003 | Registry, discovery, skills/cobertura e card inválido |
| FR-004, 023–024 | Auth/policies/owner, allowlist, HTTP sem Origin e CORS condicional |
| FR-005–010, 013, 026 | Schema, validação contextual, fixtures e compatibilidade |
| FR-011–012 | Adapter, Task/Artifact, polling e cancelamento |
| FR-014–017, 020 | Worker, agregação, limites, falhas e reinício |
| FR-018–019, 022 | Mongo/CAS/TTL, consulta, falha de gravação e retenção |
| FR-021 | Métricas/spans e consumo disponível ou nulo |
| FR-025, 027 | Regressão e >=2 agentes; parcial com 3 |

Gates futuros: build net10, domínio/wire, HTTP real de teste, Mongo/CAS/reinício,
cinco execuções simultâneas, inspeção de logs e regressão com A2A desabilitada
e indisponível. [quickstart.md](quickstart.md) contém comandos futuros/expectativas.

## Complexity Tracking

Nenhuma violação identificada. Adapter específico e projeto de testes são
necessidades demonstradas, não exceções. Sem banco novo, barramento,
transações distribuídas, frontend ou reestruturação dos projetos atuais.
