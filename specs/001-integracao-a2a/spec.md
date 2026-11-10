# Feature Specification: 001 — Integração A2A da SOFIA

**Feature Branch**: `main` — branch atual; nenhum hook de criação de branch configurado.

**Created**: 2026-11-05

**Status**: Implementada e validada localmente com fixtures; agentes reais dos
alunos e implantação externa ainda não validados. Evidências: [validation-results.md](validation-results.md).

**Atualização de contexto (2026-11-05)**: o consumidor visual foi implementado em
`SofiaInfis.Web` e é especificado separadamente na
[002 — Interface Blazor](../002-interface-blazor/spec.md). Ele envia textos à
orquestração, consulta contribuições e oferece síntese sob demanda pelo endpoint
legado de perguntas. Esta vinculação não altera requisitos, wire A2A ou contratos
da 001; a síntese não integra o worker A2A nem sobrescreve análises originais.

**Input**: User description: "Desenvolver a integração A2A da SOFIA com os agentes
especializados dos alunos da INFIS. Descobrir agentes por Agent Cards, identificar
capacidades e encaminhar análises com identificador da execução, nome do usuário,
pergunta e documentos textuais. Receber identificação, status, resultado, achados e
erros, correlacionar respostas, registrar a execução e disponibilizar resultados
para consolidação. Preservar endpoints, serviços e integrações existentes e usar
as skills de implementação A2A, contrato A2A e comunicação HTTP."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Identificar especialistas disponíveis (Priority: P1)

Como responsável pela operação da SOFIA, quero identificar quais agentes autorizados
oferecem as especialidades solicitadas, para encaminhar documentos somente a
especialistas compatíveis e habilitados.

**Why this priority**: A escolha confiável do destinatário precede o envio de dados.

**Independent Test**: Disponibilizar cards de agentes de referência e consultar
suas capacidades, sem realizar uma análise de documentos.

**Acceptance Scenarios**:

1. **Given** dois agentes habilitados com cards válidos e especialidades distintas,
   **When** SOFIA descobre suas capacidades, **Then** identifica cada agente,
   suas especialidades e o endereço de atendimento declarado no respectivo card.
2. **Given** uma especialidade solicitada e um agente habilitado que a oferece,
   **When** os candidatos são selecionados, **Then** somente agentes compatíveis e
   autorizados para a operação podem receber os documentos.
3. **Given** card inválido, versão incompatível, identidade duplicada ou destino
   não permitido, **When** ocorre a descoberta, **Then** o agente fica inelegível,
   a causa é registrada sem segredos e nenhum documento é enviado a esse destino.
4. **Given** nenhum agente compatível, **When** a execução é solicitada,
   **Then** SOFIA informa a ausência de especialista, registra a execução sem
   análise realizada e não inventa resultado.

---

### User Story 2 - Obter análise de um especialista (Priority: P1)

Como usuário da SOFIA, quero encaminhar minha pergunta e documentos já convertidos
em texto a um especialista, para receber uma análise de sua área com evidências.

**Why this priority**: É a entrega mínima de valor da integração com os alunos.

**Independent Test**: Enviar uma solicitação válida a um agente de referência
conhecido e conferir seu resultado, sem depender de vários agentes ou síntese final.

**Acceptance Scenarios**:

1. **Given** usuário identificado pelo nome, pergunta válida e dois documentos
   textuais, **When** SOFIA solicita análise a um agente elegível, **Then** o agente
   recebe os mesmos textos e IDs documentais, a pergunta, o nome, a versão do
   contrato e os identificadores de execução e solicitação.
2. **Given** análise concluída com divergências, **When** o agente responde,
   **Then** SOFIA aceita somente resultado estruturado com agente identificado,
   status, análise e achados vinculados aos documentos da solicitação original.
3. **Given** documentos analisados sem divergências, **When** o agente responde
   com análise explicativa e achados vazios, **Then** SOFIA registra sucesso sem
   transformar ausência de achados em falha.
4. **Given** tarefa ainda em processamento, **When** a resposta inicial chega,
   **Then** SOFIA acompanha a tarefa até resultado final ou limite de espera,
   sem apresentar mensagem intermediária como análise concluída.
5. **Given** campo obrigatório ausente, texto vazio ou volume acima do limite,
   **When** a solicitação é validada, **Then** a delegação é rejeitada com causa
   identificável, sem envio parcial nem truncamento silencioso.

---

### User Story 3 - Reunir respostas e preservar sucesso parcial (Priority: P1)

Como consumidor dos resultados da SOFIA, quero receber as contribuições individuais
mesmo quando um agente falha, para consolidar as evidências disponíveis sem perder
as análises bem-sucedidas.

**Why this priority**: A indisponibilidade de um aluno não pode inutilizar o trabalho
concluído pelos demais especialistas.

**Independent Test**: Fornecer respostas controladas de três agentes, incluindo um
indisponível, e verificar o conjunto correlacionado disponibilizado à consolidação.

**Acceptance Scenarios**:

1. **Given** três agentes selecionados, dois concluindo e um indisponível,
   **When** a execução termina, **Then** as duas análises originais são preservadas,
   a falha do terceiro é explicitada e a execução fica marcada como parcial.
2. **Given** resposta com outra execução, solicitação, agente ou documento,
   **When** SOFIA valida o retorno, **Then** não o incorpora como análise válida
   e registra falha de correlação sem afetar os outros resultados.
3. **Given** respostas fora de ordem ou duplicadas, **When** chegam à SOFIA,
   **Then** cada chamada tem no máximo um resultado terminal aceito e nenhuma
   análise é associada ao agente errado.
4. **Given** um agente excedendo o prazo ou cancelamento solicitado,
   **When** o limite ou cancelamento é aplicado, **Then** SOFIA encerra a espera,
   registra a condição e mantém os resultados já obtidos. Retornos posteriores
   não alteram uma execução encerrada.
5. **Given** todos os agentes falhando ou um agente devolvendo erro de análise,
   **When** o conjunto é encerrado, **Then** SOFIA distingue falha técnica de erro
   de negócio e não apresenta nenhum deles como análise sem divergências.

---

### User Story 4 - Consultar a execução para consolidação (Priority: P1)

Como responsável autorizado pela consolidação, quero recuperar uma execução pelo
identificador original, para consultar o andamento e os resultados dos especialistas.

**Why this priority**: O registro torna a análise recuperável além da resposta imediata.

**Independent Test**: Registrar resultados de referência, reiniciar a aplicação e
consultar a mesma execução, sem repetir chamadas aos agentes.

**Acceptance Scenarios**:

1. **Given** execução aceita, **When** seu acompanhamento é consultado,
   **Then** são apresentados identificação, estado, horários, agentes selecionados,
   situação de cada chamada e resultados ou falhas já registrados.
2. **Given** execução encerrada e registrada, **When** a aplicação é reiniciada,
   **Then** o consumidor autorizado recupera as mesmas análises individuais e
   correlações, sem nova cobrança ou chamada de análise aos agentes.
3. **Given** resultado parcial ou erro de negócio, **When** a execução é consultada,
   **Then** essas condições permanecem explícitas e a análise original é preservada,
   sem ser substituída por uma síntese da SOFIA.
4. **Given** consumidor sem autorização ou ID desconhecido/expirado,
   **When** solicita consulta, **Then** não recebe dados da execução nem indícios
   que exponham dados de outro solicitante.
5. **Given** falha ao registrar o início, **When** o processamento é solicitado,
   **Then** nenhum agente é acionado. Se o registro falhar após uma análise,
   SOFIA informa a falha de gravação e não declara o resultado como salvo.
6. **Given** execução em andamento interrompida por reinício,
   **When** o acompanhamento é recuperado, **Then** a interrupção fica explícita,
   resultados já persistidos são preservados e análises não são repetidas
   automaticamente.

---

### User Story 5 - Proteger a comunicação e os documentos (Priority: P1)

Como responsável pela INFIS, quero que somente participantes autorizados troquem
solicitações e resultados, para proteger documentos e a identidade dos usuários.

**Why this priority**: O fluxo envolve texto documental e dados pessoais.

**Independent Test**: Testar credenciais válidas, ausentes e sem permissão, destinos
bloqueados e documentos adversariais com agentes de referência.

**Acceptance Scenarios**:

1. **Given** credencial válida e permissão para o agente, **When** SOFIA delega
   análise, **Then** a chamada autenticada é aceita. Sem credencial ou permissão,
   a chamada é recusada e não há análise nem exposição de resultados protegidos.
2. **Given** chamada entre SOFIA e agente sem indicação de origem de navegador,
   **When** rede e autenticação estão corretas, **Then** a comunicação funciona
   independentemente de autorização CORS.
3. **Given** documento com comandos para revelar segredos ou chamar outro agente,
   **When** é analisado, **Then** esses comandos são tratados como conteúdo do
   documento e não alteram permissões ou provocam delegações adicionais.
4. **Given** uma execução com dados de teste identificáveis,
   **When** seus logs operacionais são revisados, **Then** contêm correlação,
   status e tempos, sem nomes de usuários, documentos, credenciais ou análises integrais.

---

### User Story 6 - Continuar usando as funcionalidades atuais (Priority: P2)

Como cliente atual da SOFIA, quero continuar perguntando, enviando anexos e mantendo
conhecimentos sem adaptar minha integração para a nova funcionalidade A2A.

**Why this priority**: A integração deve ampliar o produto e preservar seus consumidores.

**Independent Test**: Executar os cenários atuais com a integração A2A desabilitada
e com os agentes indisponíveis, comparando contratos e comportamento esperado.

**Acceptance Scenarios**:

1. **Given** cliente usando os contratos atuais, **When** pergunta sem anexos,
   pergunta com PDF/TXT, ingere texto ou busca conhecimento, **Then** as operações
   continuam aceitando os mesmos campos e devolvendo os contratos existentes.
2. **Given** nenhum agente configurado ou todos indisponíveis,
   **When** o cliente usa os fluxos existentes, **Then** a ausência de A2A não
   introduz falha, novo campo obrigatório ou exigência de nome nesses fluxos.
3. **Given** serviços e integrações existentes, **When** a nova capacidade é
   disponibilizada, **Then** RAG, embeddings, extração, limites e metadados atuais
   permanecem disponíveis, sem substituição incidental.

### Edge Cases

- Card inacessível, incompleto, incompatível ou com endereço redirecionado para
  destino não permitido: registrar inelegibilidade e não enviar dados.
- Identidade duplicada no catálogo ou seleção repetida do mesmo agente: bloquear
  cadastro ambíguo e enviar no máximo uma chamada por agente em cada execução.
- Nome/pergunta contendo apenas espaços; documentos repetindo ID; texto vazio ou
  sabidamente truncado: rejeitar a nova delegação e orientar correção da entrada.
- Pergunta sem documentos continua válida no fluxo atual, mas não no novo fluxo
  especializado, que exige ao menos um documento textual.
- Retorno malformado, versão divergente, campo obrigatório ausente, status desconhecido
  ou referência a documento não enviado: rejeitar resultado e registrar causa.
- Tarefa encerrada sem análise final, falha protocolar, solicitação de informação
  adicional ou cancelamento remoto: registrar causa distinta; não inventar conclusão.
- Resposta válida com status de negócio `erro` em tarefa concluída: preservar o erro
  de negócio, distinguindo-o de falha de transporte e de análise sem achados.
- Resposta tardia, duplicada ou obtida após cancelamento: não sobrescrever resultado
  terminal nem reabrir execução encerrada.
- Reinício, falha de gravação, expiração do registro ou consulta não autorizada:
  não declarar persistência inexistente nem expor registros de outros consumidores.
- Consumo de tokens não informado pelo agente: indicar indisponibilidade, não zero
  medido nem estimativa apresentada como consumo real.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: SOFIA DEVE manter relação de agentes autorizados com identidade estável,
  localização do Agent Card, habilitação e permissões de uso; cards não autorizam
  automaticamente um destino a receber documentos. Aceite: US1.1–US1.3.
- **FR-002**: SOFIA DEVE descobrir e validar identidade, capacidades, versão,
  transporte, endpoint e requisitos de autenticação anunciados por cada agente;
  falha de descoberta torna somente esse agente inelegível. Aceite: US1.1–US1.3.
- **FR-003**: A solicitação de orquestração DEVE indicar as capacidades desejadas;
  SOFIA DEVE selecionar todos os agentes habilitados, autorizados e compatíveis
  com ao menos uma delas, uma chamada por agente, e informar capacidades sem
  cobertura. Nenhum agente elegível implica execução falha sem análise; cobertura
  incompleta com análises válidas implica execução parcial. Aceite: US1.2–US1.4, US3.1.
- **FR-004**: SOFIA DEVE aceitar o novo fluxo apenas de um chamador autenticado e
  autorizado. O nome do usuário é contexto de negócio, não credencial nem chave
  de autorização. As consultas DEVEM respeitar o vínculo de acesso do chamador
  autenticado à execução. Aceite: US4.4, US5.1.
- **FR-005**: A solicitação ao agente DEVE obedecer ao contrato SOFIA 1.0, contendo
  `versaoContrato`, `idExecucao`, `idSolicitacao`, `usuario.nome`, `pergunta` e
  documentos identificados com `idDocumento`, `nome` e `texto`. `tipo` e `contexto`
  permanecem opcionais. Aceite: US2.1, US2.5.
- **FR-006**: SOFIA DEVE validar campos obrigatórios, IDs, unicidade documental,
  limites e texto não vazio antes de enviar dados. Não DEVE enviar binários,
  truncar silenciosamente ou delegar texto sabidamente incompleto sem rejeição
  explícita. Aceite: US2.5 e casos de borda documentais.
- **FR-007**: SOFIA DEVE preservar um identificador de execução comum e gerar um
  identificador de solicitação distinto por agente; repetição segura da mesma
  chamada DEVE reutilizar a solicitação e impedir processamento duplicado.
  Aceite: US2.1, US3.2–US3.3.
- **FR-008**: Os agentes participantes DEVEM analisar os documentos conforme suas
  capacidades declaradas e devolver análise, achados e evidências pertinentes,
  sem inventar trechos nem delegar a outros agentes sem autorização explícita.
  Aceite: US2.2–US2.3, US5.3.
- **FR-009**: SOFIA DEVE validar cada retorno com versão, IDs, `agente.idAgente`,
  `agente.nome`, `status`, `analise` e `achados` obrigatórios. A análise textual é
  o campo `analise`, não um novo campo `resultado`. Erro de negócio DEVE trazer
  código e mensagem não sensíveis. Aceite: US2.2–US2.3, US3.5.
- **FR-010**: Os status de negócio aceitos são `concluido`, `parcial` e `erro`.
  Achados DEVEM manter identificação, descrição, referências documentais e
  evidências conforme o contrato; referências inexistentes DEVEM ser rejeitadas.
  Aceite: US2.2–US2.3, US3.2, US4.3.
- **FR-011**: A integração DEVE manter o perfil A2A da turma e recuperar a análise
  final no artefato de uma tarefa concluída. Mensagens intermediárias não são
  resultado final; resposta direta em Message depende de acordo e teste específico.
  Aceite: US2.2, US2.4 e restrições de interoperabilidade.
- **FR-012**: SOFIA DEVE acompanhar tarefas em andamento dentro do prazo da chamada.
  Tarefa falha, cancelada, sem artefato válido ou solicitando informação adicional
  DEVE ter condição registrada; interação adicional com usuário fica fora desta
  primeira entrega e não pode ser apresentada como sucesso. Aceite: US2.4 e casos de borda.
- **FR-013**: SOFIA DEVE rejeitar resultados de outra execução, solicitação, agente
  ou documento, inclusive quando formalmente válidos. Aceite: US3.2.
- **FR-014**: SOFIA DEVE reunir resultados por agente, preservar análises originais
  e disponibilizar separadamente falhas e limitações. A consolidação posterior
  não DEVE sobrescrever os resultados originais. Aceite: US3.1, US4.3.
- **FR-015**: A execução DEVE concluir como `concluida` quando todos os agentes
  selecionados concluírem integralmente e houver cobertura das capacidades;
  `parcial` quando houver ao menos uma análise utilizável e alguma limitação;
  `falhou` quando não houver análise utilizável. Cancelamento e interrupção DEVEM
  manter estados próprios `cancelada` e `interrompida`. Análise sem achados conta
  como utilizável. Resultado de negócio `concluido` ou `parcial` com análise válida
  é utilizável; resultado `erro` não é. Aceite: US1.4, US2.3, US3.1, US3.4–US3.5, US4.6.
- **FR-016**: SOFIA DEVE aplicar limites configuráveis de descoberta, chamada,
  execução e concorrência; cancelamento e timeout DEVEM encerrar a espera e
  impedir novas delegações daquela execução. Não DEVE presumir que parar a espera
  garante que o agente remoto interrompeu seu trabalho. Aceite: US3.4.
- **FR-017**: Falhas de conexão, autenticação, autorização, protocolo, validação,
  correlação, prazo e negócio DEVEM ter classificações distinguíveis, sem vazar
  segredos. Retry somente é permitido quando seguro e dentro dos mesmos limites;
  não há retry automático de análise na configuração inicial. Aceite: US3.2–US3.5, US5.1.
- **FR-018**: SOFIA DEVE registrar duravelmente o início antes da delegação e atualizar
  cada chamada e o encerramento, com IDs, estado, horários, agente, resultado/falha,
  duração e consumo disponível. Uma falha de gravação NÃO DEVE ser informada como
  persistência bem-sucedida. Aceite: US4.1–US4.3, US4.5.
- **FR-019**: Consumidores autorizados DEVEM conseguir consultar execução e
  resultados registrados pelo ID original, inclusive após reinício, sem novas
  chamadas aos agentes. Consulta a registro inexistente/expirado ou sem acesso
  NÃO DEVE revelar dados de terceiros. Aceite: US4.1–US4.4.
- **FR-020**: Execuções interrompidas DEVEM preservar registros já gravados,
  identificar chamadas sem conclusão e não repetir automaticamente análises.
  Respostas duplicadas/tardias NÃO DEVEM alterar estado terminal. Aceite: US3.3–US3.4, US4.6.
- **FR-021**: SOFIA DEVE registrar logs operacionais correlacionados e métricas de
  duração, estado, erro e timeout por agente. Tokens e custos somente DEVEM ser
  registrados quando informados/calculáveis, separando consumo local e remoto
  e identificando ausências. Aceite: US4.1, US5.4 e caso de tokens indisponíveis.
- **FR-022**: O registro durável DEVE manter apenas metadados necessários, referências
  documentais e análises para consulta; nome, pergunta original e textos documentais
  não DEVEM ser persistidos por padrão neste novo histórico. Análises podem conter
  dados sensíveis e DEVEM ter acesso restrito e retenção limitada. Logs operacionais
  NÃO DEVEM conter documentos, análises integrais, nomes ou credenciais. Aceite:
  US4.4, US5.4 e testes de retenção.
- **FR-023**: Dados DEVEM ser enviados somente a destinos aprovados, com
  autenticação/autorização pactuadas e proteção do transporte. Destinos alterados
  por cards ou redirecionamentos DEVEM ser revalidados antes do envio de dados ou
  credenciais. Conteúdo documental NÃO DEVE mudar instruções ou permissões.
  Aceite: US1.3, US5.1, US5.3.
- **FR-024**: A comunicação SOFIA–agente DEVE funcionar na rede de execução com
  descoberta e análise autenticada, sem depender de permissões CORS de navegador.
  Se houver consumidor web autorizado, somente suas origens explícitas podem
  receber permissão; CORS não substitui autenticação. Aceite: US5.2 e verificação
  condicional de origens permitidas e desconhecidas.
- **FR-025**: A entrega DEVE preservar os contratos de `POST /api/v1/sofia/perguntar`,
  `POST /api/v1/conhecimentos/ingest` e `POST /api/v1/conhecimentos/buscar`, todos os
  serviços e integrações atuais, inclusive perguntas sem anexos, RAG, ingestão,
  embeddings, extração PDF/TXT e metadados de resposta. A nova capacidade DEVE ser
  adicional e sua indisponibilidade não pode afetar os fluxos antigos. Aceite: US6.1–US6.3.
- **FR-026**: Campos adicionais do contrato de negócio 1.0 DEVEM ser opcionais e
  compatíveis; alterações incompatíveis exigem nova versão e aprovação. Nenhuma
  mudança de perfil de transporte ou funcionalidade existente é autorizada por
  esta especificação. Aceite: testes de contrato/compatibilidade e US6.
- **FR-027**: A aceitação DEVE incluir testes automatizados de regras, contratos,
  integração com pelo menos dois agentes simulados e regressão dos fluxos atuais;
  falhas parciais DEVEM ser demonstradas com três agentes. Evidências DEVEM conter
  entradas/saídas e registros sanitizados, sem afirmar execução de teste não realizado.
  Aceite: todos os cenários US1–US6 e SC-001–SC-009.

### Key Entities *(include if feature involves data)*

- **Agente especializado**: identidade estável, nome, especialidades, habilitação,
  referência ao card e permissões; mantido pelo responsável da INFIS.
- **Agent Card**: declaração de identidade, capacidades, modos aceitos, versão,
  endpoint de atendimento e requisitos de segurança do agente; sujeito a validação.
- **Execução de orquestração**: identificação original, chamador autorizado,
  capacidades desejadas, referências aos documentos e agentes, horários, estado
  geral e conjunto de chamadas. Inicia `em_andamento` e termina conforme FR-015.
- **Solicitação de análise**: chamada individual de uma execução a um agente,
  contendo versão do contrato, nome do usuário, pergunta e documentos textuais;
  uma repetição segura preserva a identidade da mesma chamada.
- **Documento textual**: identificação única na execução, nome e conteúdo já
  extraído; tipo opcional; referências dos achados apontam para essa identificação.
- **Resultado de análise**: contribuição original de um agente, status de negócio,
  análise, achados, observações e erro opcional, vinculados à execução/solicitação.
- **Achado**: identificação, descrição, documentos referenciados e evidências;
  severidade opcional conforme o contrato compartilhado.
- **Registro de chamada**: andamento, IDs de tarefa/contexto remoto quando existirem,
  horários, duração, falha técnica ou resultado e consumo informado.
- **Conjunto para consolidação**: resultados originais por agente, limitações,
  capacidades não cobertas e falhas; não equivale a síntese final gerada.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Nos testes de descoberta com dois especialistas válidos e um inválido,
  100% dos válidos são identificados corretamente e nenhum documento é enviado ao
  inválido; cada descoberta termina em até 5 segundos no perfil inicial.
- **SC-002**: Em 100% dos cenários válidos de uma pergunta com dois documentos,
  o especialista recebe os textos integrais aceitos e a identificação correta do
  usuário, execução e documentos, sem troca entre solicitações.
- **SC-003**: Em 100% dos cenários de retorno incorreto, duplicado ou tardio,
  nenhuma análise é atribuída à execução/agente errado nem sobrescreve resultado
  terminal; todos os retornos rejeitados têm motivo identificável.
- **SC-004**: Com três especialistas, dois respondendo e um indisponível ou lento,
  as duas contribuições ficam disponíveis integralmente e a falha fica explícita
  em até 125 segundos desde a aceitação, sob as condições iniciais de aceite.
- **SC-005**: Em 100% dos casos de sucesso, parcialidade, ausência de achados e erro,
  o consumidor distingue o desfecho e identifica a contribuição de cada agente
  sem consultar logs operacionais.
- **SC-006**: Após reinício, 100% das execuções encerradas e confirmadas como salvas
  são recuperáveis por consumidor autorizado, sem repetir análises; acessos
  não autorizados não expõem seus dados.
- **SC-007**: 100% dos cenários de regressão das operações atuais mantêm entradas,
  saídas e limites esperados, com a nova capacidade desabilitada ou indisponível.
- **SC-008**: Em 100% dos testes de autorização e inspeção de logs, chamadas sem
  permissão são recusadas e nenhum nome de usuário, texto documental, análise
  integral ou credencial de teste aparece nos logs operacionais.
- **SC-009**: No perfil de aceite com cinco execuções simultâneas, três especialistas
  e dois documentos de 1.000 caracteres por execução, todos os resultados são
  correlacionados corretamente e cada execução atinge estado terminal no seu prazo,
  sem perda dos resultados confirmados como salvos.

## Assumptions

### Escopo e premissas funcionais

- Os atores são usuário solicitante, operação INFIS que habilita agentes, alunos
  responsáveis pelos especialistas e consumidor autorizado de consolidação.
- O escopo desta entrega é o lado orquestrador SOFIA e a verificação do contrato
  com agentes de referência. Cada aluno responde pela análise de sua especialidade
  e pela conformidade do seu servidor; não se exige implementar todos os projetos
  dos alunos dentro desta solução.
- A primeira seleção usa capacidades explicitamente solicitadas; não inclui
  classificação automática por modelo, descoberta aberta na internet ou cadastro
  autônomo de agentes. Todo agente compatível e autorizado para essas capacidades
  recebe uma chamada; capacidades sem cobertura ficam explícitas.
- A nova operação recebe documentos já convertidos em texto. O nome do usuário
  é fornecido explicitamente e não é inferido de token. Isso não adiciona campos
  obrigatórios aos contratos legados. A exposição da capacidade adicional será
  definida no planejamento sem modificar os endpoints existentes.
- A consolidação contemplada aqui é disponibilizar análises individuais
  consultáveis. Gerar síntese final, interface gráfica, streaming, notificações,
  conversas de esclarecimento com especialistas, reprocessamento automático após
  reinício e publicar a própria SOFIA como servidor A2A ficam fora desta entrega.
- O registro de execução é novo. MongoDB configurado não comprova histórico
  funcional. Não se inclui memória conversacional, novos formatos de extração,
  alteração de modelos OpenAI ou reestruturação da solução.

### Parâmetros iniciais de aceite

Valores abaixo são premissas explícitas para planejamento e testes, não medições
nem promessa de desempenho já implementado. Devem ser configuráveis e documentados
por ambiente; mudar contrato público ou comportamento legado exige aprovação.

| Parâmetro | Valor inicial e comportamento |
|---|---|
| Nome do usuário | 1–200 caracteres, após remover espaços externos |
| Pergunta no novo fluxo | 5–2.000 caracteres |
| Documentos por solicitação | 1–5; IDs distintos e nome não vazio |
| Texto por documento | 1–20.000 caracteres; rejeitar excedente ou truncamento conhecido |
| Envelope de solicitação e retorno | Até 1 MiB cada; rejeitar excedente antes de aceitá-lo como válido |
| Descoberta por agente | Até 5 segundos, incluídos no orçamento total |
| Espera por chamada de análise | Até 60 segundos, incluindo acompanhamento de tarefa |
| Orçamento total da execução | Até 120 segundos; até 5 segundos adicionais para encerrar e disponibilizar estado |
| Concorrência por execução | Até três chamadas de agentes em andamento; espera consome o mesmo orçamento total |
| Repetição automática de análise | Desabilitada inicialmente; habilitação exige deduplicação demonstrada |
| Retenção do novo histórico e métricas detalhadas | 30 dias após encerramento; eliminação em até 24 horas após expirar |

O limite de texto vale para a nova solicitação especializada e não altera o
truncamento sinalizado atualmente pela extração de anexos. Nome, pergunta e texto
são transitórios no novo fluxo; somente referências e resultados necessários são
retidos. O chamador autenticado, não o nome informado, define o acesso ao histórico.
O teste de retenção deve verificar expiração, eliminação e indisponibilidade de consulta.

### Dependências e restrições de interoperabilidade herdadas

As definições de protocolo e campos abaixo são contratos exigidos pelo projeto,
não escolhas de implementação de classes, bibliotecas ou armazenamento. A
arquitetura e o framework obrigatórios continuam regidos pela constituição.

- [Constituição SOFIA](../../.specify/memory/constitution.md), especialmente
  preservação, segurança, rastreabilidade e testes.
- [Skill de contrato](../../.agents/skills/sofia-a2a-contrato/SKILL.md) e
  [contrato SOFIA 1.0](../../.agents/skills/sofia-a2a-contrato/references/contrato-sofia-v1.md):
  nomes e obrigatoriedade de campos, estados de negócio e correlação são normativos.
- [Skill de implementação](../../.agents/skills/sofia-a2a-implementacao/SKILL.md) e
  [critérios de arquitetura](../../.agents/skills/sofia-a2a-implementacao/references/arquitetura-e-criterios.md):
  perfil fixado A2A v0.3.x, JSON-RPC 2.0, `message/send`, descoberta por
  `/.well-known/agent-card.json` e chamada ao `url` declarado no card.
- [Exemplos A2A](../../.agents/skills/sofia-a2a-contrato/references/exemplos-a2a-v03.md):
  solicitação SOFIA serializada como JSON UTF-8 no TextPart de Message; retorno
  SOFIA no TextPart de Artifact de Task `completed`. Status protocolar e status
  de negócio são distintos. Não substituir esse perfil por análise REST ad hoc.
- [Skill HTTP/CORS](../../.agents/skills/sofia-cors-rest-a2a/SKILL.md):
  conectividade e autenticação servidor-servidor são obrigatórias; CORS é
  condicional a navegador. HTTPS é exigido fora de desenvolvimento em rede isolada;
  novos acessos protegidos devem distinguir falta de autenticação e de permissão.
- Agentes reais dependem de rede, endereços permitidos e credenciais de serviço
  fornecidas pela INFIS/alunos. Cards podem ser públicos ou protegidos conforme
  a política do agente; a ausência de credencial necessária impede sua seleção.
- Aceite local deve poder usar agentes simulados com análise determinística,
  sem depender de OpenAI nem de documentos reais de clientes. A compatibilidade
  efetiva dos pacotes com o perfil fixado deve ser pesquisada no planejamento;
  dependência declarada não autoriza presumir suporte a outra versão do protocolo.
