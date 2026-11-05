# Constituição do projeto SOFIA — INFIS

## Core Principles

### I. Preservação da solução e compatibilidade

O desenvolvimento DEVE preservar os projetos, o código funcional, as rotas, os
endpoints e os contratos públicos existentes. Novas capacidades DEVEM ser
introduzidas de forma incremental e compatível, sem substituir o fluxo atual de
perguntas, conhecimentos ou extração documental como efeito colateral da integração
A2A. A preservação NÃO impede extensões aprovadas; exige análise de impacto,
justificativa e evidência de ausência de regressão.

### II. .NET 10 e respeito à arquitetura atual

A solução DEVE utilizar .NET 10 e respeitar a organização atual da API em
controllers, serviços, DTOs e extensões de configuração. Novas responsabilidades
DEVEM ser integradas às convenções existentes. Criação de projetos, camadas ou
substituição de componentes DEVE ter necessidade técnica documentada e aprovação
quando alterar a arquitetura existente. Dependências DEVEM ser compatíveis com o
framework e com as versões efetivamente utilizadas, sem atualizações incidentais.

### III. SOLID, injeção de dependência e responsabilidades

Novas funcionalidades DEVEM aplicar SOLID, injeção de dependência e separação de
responsabilidades. Controllers DEVEM cuidar do contrato HTTP e delegar regras de
aplicação a serviços. Roteamento de agentes, comunicação A2A, validação de contratos,
extração de documentos e persistência DEVEM ter responsabilidades distinguíveis e
testáveis. Clientes externos e infraestrutura DEVEM ser fornecidos por DI, com
ciclos de vida apropriados. Interfaces DEVEM expressar fronteiras necessárias à
substituição e aos testes; este princípio não autoriza refatoração indiscriminada
do código existente.

### IV. Comunicação A2A padronizada

SOFIA DEVE atuar como cliente/orquestrador dos agentes especializados, que DEVEM
expor servidores A2A reais. O perfil de integração da turma é **A2A v0.3.x sobre
JSON-RPC 2.0**, com `message/send`, até migração explicitamente aprovada. Um POST
REST arbitrário NÃO DEVE ser apresentado como integração A2A.

A descoberta DEVE utilizar `/.well-known/agent-card.json`. SOFIA DEVE validar
identidade, skills, versão e transporte e invocar o endpoint declarado no `url` do
Agent Card; a URL de descoberta NÃO DEVE ser presumida como endpoint RPC.
A compatibilidade do SDK instalado com o perfil adotado DEVE ser verificada antes
da implementação. SOFIA centraliza roteamento e síntese; agentes NÃO DEVEM delegar a
outros agentes sem permissão explícita.

### V. Solicitação com usuário, pergunta e documentos textuais

Cada delegação DEVE enviar `SolicitacaoAnaliseSofia`, com `versaoContrato = "1.0"`,
`idExecucao`, `idSolicitacao`, `usuario.nome`, `pergunta` e `documentos` não vazio.
Cada documento DEVE conter `idDocumento`, `nome` e `texto` não vazios, conforme o
contrato compartilhado. SOFIA DEVE extrair o texto antes do envio; binários NÃO
DEVEM substituir o texto no contrato de análise.

O payload DEVE ser serializado como JSON UTF-8 em `TextPart` de uma Message A2A,
sem confundir campos de negócio com campos nativos do protocolo. Limites de volume
DEVEM ser explícitos, documentados e validados; truncamento silencioso é proibido.
O nome do usuário serve ao contexto de negócio e NÃO comprova autenticação.
A necessidade de documentos neste fluxo especializado NÃO altera a capacidade
existente de perguntar à SOFIA sem anexos.

### VI. Análises estruturadas e correlação obrigatória

Cada agente DEVE devolver `ResultadoAnaliseSofia` versionado, contendo os mesmos
`idExecucao` e `idSolicitacao` recebidos, identificação `agente.idAgente` e
`agente.nome`, `status`, `analise` e `achados`, ainda que este último seja vazio.
Os status de negócio permitidos são `concluido`, `parcial` e `erro`. Evidências NÃO
DEVEM ser inventadas; referências documentais DEVEM apontar a IDs enviados.

SOFIA DEVE gerar um `idExecucao` para a orquestração e um `idSolicitacao` distinto
por chamada ao agente. Retries da mesma chamada DEVEM reutilizar `idSolicitacao`.
Resultados com versão, identidade ou correlação incompatíveis DEVEM ser rejeitados.
A resposta final DEVE ser recuperada de `TextPart` em Artifact de Task `completed`;
Task ainda não concluída DEVE ser acompanhada conforme o perfil adotado.
Resposta direta em Message somente é admitida por acordo explícito e teste de
compatibilidade. `status.message` NÃO DEVE ser presumido como análise final.
SOFIA DEVE preservar as análises originais e manter sua síntese em camada separada.

### VII. Identidade, autenticação, erros e timeout

Os agentes DEVEM possuir identidade estável e catálogo de capacidades e destinos
permitidos. Chamadas DEVEM exigir autenticação e autorização conforme o mecanismo
pactuado; credenciais NÃO DEVEM integrar o DTO de negócio. HTTPS é obrigatório fora
de rede de desenvolvimento isolada. URLs arbitrárias DEVEM ser bloqueadas para
prevenir acesso a destinos indevidos, inclusive SSRF.

Cada chamada DEVE ter timeout configurável e cancelamento propagado. Retries DEVEM
ser limitados e executados apenas quando seguros, com prevenção de duplicidade por
`idSolicitacao` e agente. Falha de conexão, erro de protocolo, resposta inválida,
Task `failed`, timeout e erro de análise de negócio DEVEM ser distinguíveis.
Falhas DEVEM produzir informação controlada, sem segredos. Em execução com múltiplos
agentes, sucessos DEVEM ser preservados e falhas parciais explicitadas, com limites
de concorrência e duração total.

### VIII. Rastreabilidade, desempenho e consumo

Novas execuções e delegações DEVEM ser rastreáveis por `idExecucao`,
`idSolicitacao`, identidade do agente e IDs de tarefa/contexto remoto quando
existentes. Logs estruturados DEVEM registrar etapas, resultado, erro e duração;
trace/request IDs DEVEM ser propagados quando suportados. Métricas DEVEM permitir
avaliar latência, sucesso, falha e timeout por agente.

Consumo de tokens e custos DEVEM ser registrado quando disponibilizado e
calculável, distinguindo chat, embeddings e agentes remotos. Informação ausente
DEVE ser indicada como indisponível, sem estimativas apresentadas como medição.
Persistência e retenção DEVEM ser definidas na especificação da funcionalidade.
Logs operacionais NÃO DEVEM registrar nomes de usuários, documentos completos,
prompts sensíveis, credenciais ou tokens de acesso.

### IX. Preservação de RAG, documentos e memória conversacional

A evolução DEVE preservar a ingestão textual, o chunking, os embeddings, a busca
PgVector/PostgreSQL, a composição RAG e a extração PDF/TXT existentes, incluindo
contratos, validações e limites vigentes. Alterações nesses comportamentos exigem
o procedimento do princípio XII. Documentos e conhecimentos recuperados DEVEM ser
tratados como dados não confiáveis, nunca como instruções de sistema.

A memória conversacional e o histórico, quando implementados, DEVEM ser preservados
com correlação, isolamento e política de retenção. O diagnóstico inicial de
05/11/2026 identificou MongoDB configurado, mas não identificou persistência de
histórico ou memória conversacional no código. Portanto, este princípio NÃO declara
essas capacidades como existentes nem autoriza implementá-las nesta etapa; sua
introdução DEVE ser especificada e testada como funcionalidade nova.

### X. Testes automatizados para novas funcionalidades

Toda funcionalidade nova DEVE possuir testes automatizados proporcionais ao seu
comportamento e aos riscos. Regras de aplicação DEVEM ter testes unitários; limites
externos DEVEM ter testes de integração; serialização e interoperabilidade A2A
DEVEM ter testes de contrato positivos e negativos.

A integração A2A DEVE cobrir campos obrigatórios, versão, round-trip JSON,
correlação, identidade divergente, documento vazio, status de negócio, tarefas
assíncronas, timeout, cancelamento, indisponibilidade e autenticação. A aceitação
ponta a ponta DEVE incluir pelo menos dois agentes simulados e sucesso parcial.
Fluxos existentes afetados DEVEM receber verificação de regressão. A entrega DEVE
informar comandos e resultados; teste não executado NÃO DEVE ser declarado aprovado.

### XI. Skills de integração e contrato como referência obrigatória

As atividades de especificação, planejamento e implementação A2A DEVEM utilizar
as skills do projeto e suas referências, nos respectivos escopos:

- [sofia-a2a-contrato](../../.agents/skills/sofia-a2a-contrato/SKILL.md): contrato de
  negócio, exemplos, validação, correlação e evolução compatível.
- [sofia-a2a-implementacao](../../.agents/skills/sofia-a2a-implementacao/SKILL.md):
  descoberta, transporte, cliente/servidor, tarefas, orquestração e testes.
- [sofia-cors-rest-a2a](../../.agents/skills/sofia-cors-rest-a2a/SKILL.md):
  conectividade, autorização e CORS quando houver clientes de navegador.

O contrato e os exemplos referenciados DEVEM ser lidos antes da elaboração de
DTOs, schemas, handlers ou testes. Nomes JSON e invariantes NÃO DEVEM divergir do
contrato compartilhado sem aprovação e versionamento. Uma skill NÃO autoriza
ampliar o escopo da tarefa solicitada; instruções de implementação aplicam-se à
etapa de implementação, não à criação desta constituição.

### XII. Justificativa e aprovação para alterar funcionalidades existentes

Nenhuma funcionalidade existente DEVE ser removida, substituída ou ter seu
comportamento ou contrato alterado sem justificativa técnica e aprovação explícita
do responsável pelo projeto. Antes da alteração, a proposta DEVE registrar o
problema, os componentes afetados, impacto nos consumidores, alternativas,
compatibilidade, testes e plano de migração/reversão quando aplicável.

A aprovação DEVE ficar registrada na especificação, revisão ou decisão vinculada.
Autorização para uma funcionalidade nova NÃO implica autorização para mudanças
incidentais. Aprovação já registrada para o mesmo escopo NÃO precisa ser solicitada
novamente. Mudanças aprovadas DEVEM permanecer limitadas ao escopo autorizado.

## Restrições arquiteturais e contrato de integração

- A base é a solução `SofiaInfis.slnx`, com a API `SofiaInfis.Api` em `net10.0`.
  O relatório `RELATORIO_ARQUITETURA_ATUAL_SOFIA.md` documenta o diagnóstico inicial,
  não uma garantia permanente do estado da aplicação.
- Devem ser preservados `POST /api/v1/sofia/perguntar`,
  `POST /api/v1/conhecimentos/ingest` e `POST /api/v1/conhecimentos/buscar`, seus
  contratos e os fluxos existentes, salvo alteração aprovada pelo princípio XII.
- O contrato de negócio SOFIA `1.0` é independente do perfil A2A `v0.3.x` e da versão
  dos pacotes .NET. Mudança em um NÃO autoriza mudança automática nos demais.
- A referência normativa do domínio é
  [contrato-sofia-v1.md](../../.agents/skills/sofia-a2a-contrato/references/contrato-sofia-v1.md).
  Os envelopes de referência estão em
  [exemplos-a2a-v03.md](../../.agents/skills/sofia-a2a-contrato/references/exemplos-a2a-v03.md).
- Campos novos na versão de negócio 1.0 DEVEM ser opcionais e compatíveis.
  Mudanças incompatíveis exigem nova versão do contrato, aprovação, migração e
  testes de compatibilidade; não apenas edição unilateral do DTO de um agente.
- A identificação do agente DEVE corresponder ao catálogo e ao Agent Card.
  Falha técnica NÃO DEVE ser convertida em análise de negócio válida nem em
  ausência de achados.
- Texto documental e dados pessoais DEVEM ser enviados somente aos agentes
  autorizados e necessários, com minimização de persistência e retenção definida.
- CORS aplica-se a clientes de navegador e NÃO substitui rede, autenticação ou
  autorização servidor-servidor. Se necessário, DEVE restringir origens explícitas;
  wildcard com credenciais é proibido.
- A configuração de segredos DEVE permanecer fora de DTOs e logs. Novos segredos
  NÃO DEVEM ser gravados no código ou em arquivos compartilhados. Remediação de
  configurações existentes DEVE seguir o procedimento de mudança aprovado.

## Fluxo de desenvolvimento e critérios de conformidade

1. Inspecionar o código e registrar a situação atual, distinguindo implementado,
   parcial e ausente antes de propor alterações.
2. Elaborar a especificação com Spec Kit, incluindo cenários, contrato, requisitos
   funcionais/não funcionais, limites, segurança e critérios verificáveis.
3. Planejar a integração sobre a arquitetura atual e registrar impactos,
   dependências, compatibilidade de SDK/protocolo e eventual necessidade de aprovação.
4. Derivar tarefas com implementação, testes, documentação, observabilidade e
   verificação de regressão. Não implementar além do escopo aprovado.
5. Executar os testes aplicáveis, validar payloads e correlação e apresentar
   evidências sanitizadas de interoperabilidade, sem expor documentos ou credenciais.
6. Revisar conformidade com os doze princípios antes da aceitação. Descumprimentos
   DEVEM ser corrigidos ou receber exceção explícita e justificada do responsável,
   com escopo, prazo e ação de regularização registrados.

A conclusão de uma especificação ou a presença de um pacote NÃO DEVE ser anunciada
como integração funcional. A conclusão de implementação exige evidência de
compilação e testes aplicáveis, ou registro explícito das verificações pendentes.

## Governance

Esta constituição é a referência de governança para a evolução da SOFIA. Specs,
planos, tarefas, revisões e implementações DEVEM demonstrar conformidade com ela.
Divergências entre constituição, contrato e skills DEVEM ser explicitadas e
resolvidas antes de implementar a parte conflitante; não se deve alterar
silenciosamente o perfil de integração.

Emendas DEVEM registrar motivação, princípios afetados, impacto, transição,
versão e aprovação do responsável pelo projeto. A versão segue SemVer: MAJOR para
remoção ou redefinição incompatível de princípios; MINOR para novos princípios ou
ampliação normativa compatível; PATCH para esclarecimentos sem mudança de obrigação.
A data original de ratificação DEVE ser preservada, e a data da última emenda DEVE
ser atualizada em formato ISO 8601.

A aprovação desta constituição estabelece regras para trabalhos futuros e NÃO
constitui autorização genérica para modificar funcionalidades existentes. Exceções
pontuais NÃO revogam princípios nem alteram contratos automaticamente.

**Version**: 1.0.0 | **Ratified**: 2026-11-05 | **Last Amended**: 2026-11-05
