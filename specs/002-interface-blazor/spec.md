# Feature Specification: 002 — Interface Blazor da SOFIA

**Feature Branch**: `main` — nenhuma branch criada por esta atualização documental.
**Created / Updated**: 2026-11-05.
**Status**: implementada e validada localmente; documentação consolidada após a entrega.
**Dependência**: [001 — Integração A2A](../001-integracao-a2a/spec.md).
**Input**: construir um frontend Blazor na solução, com identificação do usuário,
chat, seleção/arraste de arquivos, identidade visual das artes INFIS, apresentação
inicial, experiência moderna e responsiva, análises por agente, resultado final e
trace sem poluir a interface.

Esta especificação descreve a implementação entregue, não um backlog ainda por
implementar. Não altera a constituição, os contratos001 ou os endpoints existentes.
Evidências e ressalvas: [validação](../../SofiaInfis.Web/VALIDACAO.md).

## User Scenarios & Testing

### US1 — Conhecer a SOFIA e identificar-se (P1)

Como visitante, quero conhecer a proposta da SOFIA e informar como devo ser chamado.
Valor: orientar a entrada e contextualizar a conversa antes do envio de documentos.
Teste independente: abrir a apresentação e acessar o chat sem chamar agentes.

1. Dada a página inicial, a pessoa vê a personagem, a identidade INFIS, as etapas
   de uso e chamadas para iniciar uma conversa.
2. Ao iniciar, o modal solicita um nome não vazio de até 200 caracteres; após a
   confirmação, a saudação e o perfil exibem o nome informado.
3. Nome é contexto de negócio, não login ou prova de identidade. Não são criadas
   conta, senha ou sessão autenticada institucional nesta entrega.

### US2 — Perguntar ou compartilhar documentos (P1)

Como usuário identificado, quero escrever ou anexar documentos sem lidar com os
contratos técnicos da API. Teste independente: enviar pergunta sem arquivo ou
preparar um TXT para uma execução documental.

1. Sem anexos, a pergunta segue para a operação existente de perguntas com RAG/OpenAI.
2. A seleção ou arraste no compositor aceita até 5 arquivos PDF/TXT; nomes e
   tamanhos aparecem antes do envio e é possível remover um anexo.
3. Somente anexos, após clicar em Enviar, geram a pergunta visível:
   “Analise os documentos anexados e apresente os principais achados e limitações.”
4. Para análise especializada, a extração ocorre antes da delegação. Arquivo
   inválido, vazio, ilegível ou texto acima de 20.000 caracteres é rejeitado;
   nenhum conteúdo sabidamente truncado é delegado.
5. Enter envia; Shift+Enter insere linha. Durante leitura/envio, os controles
   impedem submissões concorrentes no mesmo chat.
6. A evolução [005 — Entrada de voz](../005-comando-voz/spec.md) adiciona ditado
   pt-BR com botão de iniciar/parar, texto revisável e envio manual. Navegadores
   sem suporte mantêm o compositor textual. Áudio não é recebido pela API.

### US3 — Consultar contribuições e síntese (P1)

Como usuário, quero distinguir cada contribuição e reunir uma resposta final sem
perder a origem das análises. Teste independente: execução com dois agentes e uma
síntese separada, usando a infraestrutura sintética dos testes.

1. Após aceite da API, a tela acompanha o ID original e apresenta estados por agente.
2. Contribuições mostram análise, achados, documentos referenciados, evidências,
   severidade quando informada, observações e limitações. Detalhes ficam recolhidos.
3. Uma falha não remove contribuições já recebidas; ausência de análise não é
   apresentada como análise sem achados.
4. Com resultado utilizável e execução encerrada, “Gerar síntese final” envia as
   análises/limitações ao endpoint de perguntas em TXT. A resposta final tem ID e
   consumo de chat próprios; o ID A2A e as análises originais permanecem intactos.
5. Síntese não é automática; depende de RAG/OpenAI disponíveis. Se a entrada
   serializada ultrapassa20.000 caracteres, manter consulta individual e informar
   a limitação, sem truncar para gerar uma síntese incompleta.

### US4 — Entender etapas e interromper espera (P1)

Como usuário, quero acompanhar a execução sem receber uma tela cheia de dados técnicos.
Teste independente: expandir rastreamento, conferir correlação e encerrar uma espera.

1. Rastreamento recolhido mostra, ao expandir, etapas observadas pelo cliente,
   identificador da execução, solicitações quando disponíveis, estado, duração,
   erro técnico e consumo informado; o ID pode ser copiado.
2. Não inventar tokens, taskId/contextId ou spans que a consulta da API não fornece.
3. Parar uma espera documental solicita cancelamento à API. Mostrar confirmação
   apenas quando recebida; caso contrário, informar que o trabalho remoto pode continuar.
4. Falhas de conexão/permissão/volume são legíveis e não exibem corpo HTTP arbitrário.

### US5 — Usar em celular e demonstrar com clareza (P2)

Como usuário, quero a mesma jornada em telas pequenas e distinguir uma demonstração.
Teste independente: abrir a 390×844, usar menu e ler uma análise; abrir o perfil demo.

1. Navegação lateral vira menu recolhível; compositor permanece acessível e não
   ocorre overflow horizontal no perfil móvel validado.
2. Identidade usa laranja, neutros, personagem e artes fornecidas. Os arquivos de
   imagem originais são preservados; a apresentação usa recorte CSS.
3. Demonstração só é selecionada com opção explícita em Development, sinalizada
   na interface; nunca é fallback para indisponibilidade real da API.
4. Texto de análise é codificado; Markdown básico não executa HTML/scripts recebidos.

## Functional Requirements

| ID | Requisito | Estado / referência |
|---|---|---|
| FR-001 | Acrescentar frontend Blazor net10.0, preservando API e contratos | Implementado; SofiaInfis.Web |
| FR-002 | Apresentação com artes INFIS e entrada por nome | Implementado; Home |
| FR-003 | Chat com pergunta, seleção/arraste e remoção de anexos | Implementado; ChatPanel/chat.js |
| FR-004 | Usar pergunta padrão visível para envio somente de arquivos | Implementado; ChatPanel |
| FR-005 | Preservar pergunta sem anexos pelo fluxo legado | Implementado; LiveSofiaChat |
| FR-006 | Extrair texto integral e rejeitar truncamento antes de A2A | Implementado; DocumentPreparation |
| FR-007 | Delegar apenas pela API e capacidades configuradas | Implementado; LiveSofiaChat |
| FR-008 | Acompanhar ID, paginação e resultados correlacionados | Implementado; LiveSofiaChat |
| FR-009 | Mostrar análise/achados/limitações por agente sem sobrescrever | Implementado; ChatPanel |
| FR-010 | Oferecer síntese separada, sob demanda e com limites | Implementado; ConsolidateAsync |
| FR-011 | Expor rastreamento expansível e consumo disponível | Implementado; RunTrace |
| FR-012 | Distinguir cancelamento confirmado de espera encerrada | Implementado; LiveSofiaChat |
| FR-013 | Manter credencial no servidor e nome como contexto | Implementado; Interactive Server |
| FR-014 | Codificar conteúdo recebido e sanitizar erros HTTP | Implementado; MarkdownText/gateway |
| FR-015 | Ter versão responsiva e controles acessíveis por teclado | Implementado; CSS/JS/componentes |
| FR-016 | Demonstração explícita e isolada da conexão real | Implementado; DemoSofiaChat/Program |
| FR-017 | Limitar retenção transitória da conversa | Implementado;20 mensagens/circuito |
| FR-018 | Cobrir cliente, renderização e integração com testes |9 testes novos; evidências históricas |

## Limites e casos de borda

| Item | Comportamento atual |
|---|---|
| Nome | Não vazio, até 200; navegador aplica maxlength200, gateway conta escalares Unicode |
| Pergunta |5–2.000; navegador aplica maxlength2000, gateway conta escalares Unicode |
| Arquivos | Até5;5 MiB por arquivo;20 MiB no total; PDF textual e TXT |
| Texto por documento A2A | Até20.000 caracteres do extrator; recusar resultado truncado |
| Envelope A2A | Limite da API 001 de 1 MiB continua aplicável, inclusive escapes |
| Envio / síntese | Orçamentos de 150 s /120 s no cliente |
| Comunicação HTTP | Timeout130 s, conexão5 s; catálogo até10 s; polling750 ms |
| Cancelamento | Tentativa de confirmação até 6 s; sem garantia de interrupção remota |
| Paginação |100 chamadas por página; proteção de volume acima de1.000 chamadas |
| Resposta da API | Leitura limitada a2 MiB por resposta no cliente |
| Síntese | Até20.000 caracteres no JSON convertido em TXT, antes do envio legado |
| Sem especialista / sem credencial | Exibir limitação real; não substituir por demo |
| Recarregar / Nova conversa | Limpa estado local; histórico A2A durável continua na API |

## Key Entities

`ChatInput` contém nome, pergunta e arquivos; `ChatFile` mantém bytes transitórios;
`ChatRun` associa resposta, ID A2A, ID da síntese, agentes e etapas; `AgentView`
mantém contribuição original/erro/duração/consumo; `TraceEntry` registra etapa observada.
Nenhuma entidade desta feature adiciona tabela ou coleção.

## Success Criteria e evidências

| ID | Critério | Evidência / limite |
|---|---|---|
| SC-001 | Navegar da apresentação ao chat identificado | Conferência visual desktop e móvel |
| SC-002 | Perguntar sem anexo usando contrato legado | SofiaWebGatewayTests |
| SC-003 | Enviar texto integral e recusar correlação divergente | Gateway + integração A2A |
| SC-004 | Sintetizar sem substituir ID/análises A2A | Gateway + integração de dois agentes |
| SC-005 | Exibir conteúdo não confiável sem executar HTML | SofiaWebRenderingTests |
| SC-006 | Ler resultado e rastreamento sem overflow a 390 px | Conferência visual390×844 |
| SC-007 | Preservar regressão da solução | Última suíte:79 aprovados,9 novos |

As evidências foram obtidas durante a implementação e as capturas posteriores,
não executadas novamente nesta atualização documental. Arraste é implementado;
o gesto completo não foi automatizado. Agentes reais, SSO e produção não validados.

## Fora do escopo e funcionalidades ausentes

SSO/contas institucionais, memória de LLM, histórico persistente de conversas da UI,
OCR, novos formatos documentais, ingestão de conhecimentos pela interface,
administração do catálogo, painel completo de spans e implantação pública.
O frontend consome a 001; não implementa outro servidor/protocolo A2A.
