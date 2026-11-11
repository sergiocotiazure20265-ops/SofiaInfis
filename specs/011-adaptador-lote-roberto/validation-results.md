# Validação do adaptador de Roberto

2026-11-11. Build da solução aprovado. Suíte focada de regressão: 124 aprovados,
0 falhas, 0 ignorados. Inclui 21 cenários do adaptador, pipeline HTTP com dois
especialistas e revisor final, condições de dados insuficientes sem envio remoto,
validação de schema e domínio, falhas, cancelamento, prazo, autoria, cores e
conclusão local com limitações quando a síntese falha.

Comando: dotnet test SofiaInfis.Api.Tests --no-restore com filtro das classes
FinalReviewPipelineTests, FinalReviewGatewayTests, AutomaticAnswerTests,
SelectiveRoutingTests, AgentTraceRenderingTests, Unit, SofiaWebGatewayTests,
CloudDeploymentTests, SofiaWebRenderingTests, A2ACompatibilityMatrixTests,
SofiaDomainContractTests, StudentAgentCatalogTests e A2A03WireContractTests.

Os testes novos de integração usam transporte HTTP real local e repositório em
memória. A suíte completa dependente de Mongo/Docker não foi reexecutada neste
ambiente. Avisos preexistentes NU1902/NU1903 de Pkcs 9.0.18 permanecem.

A simulação Postman existente modela o especialista fiscal genérico. A aceitação
de lote pelo Roberto não é inferida dela: schema/código oficial foram consultados,
o adaptador foi testado, e a verificação remota será registrada abaixo.

## Publicação e verificação real

Versão inicial publicada no Render em 11/11/2026: revisão
`85b3d142027637fa0646108baa82c35ea0fab4c7`, deploy
`dep-dbqc32uc6qcc73esn4v0`, estado `live` às 19:28:04 UTC.

Evidências locais da verificação pelo chat publicado, somente com dados sintéticos:

- `538bb579-178a-4f86-9bd5-21ed66ff22d1`: obrigações normalizadas NFSE e ECD;
  os dois especialistas retornaram parciais e Roberto foi chamado na última
  etapa. Lote aceito; parecer parcial, sem cruzamentos para esses tipos. A tela
  identificou a autoria de Roberto e as limitações, sem certificar conformidade.
- `e73e6000-1516-47d4-956e-01383d81a639`: arquivos brutos sem relatórios de
  origem. Roberto não foi chamado; a SOFIA concluiu com os motivos da
  insuficiência. Esse teste expôs mensagem ambígua sobre contribuintes, corrigida
  para distinguir ausência de identificação de raízes efetivamente diferentes.

Arquivos ignorados: `artifacts/lote-adapter/sufficient-result.json`,
`insufficient-result.json` e `sufficient-desktop.png`. Essas evidências não
homologam regras fiscais nem os demais agentes.

Reteste da entrega em 11/11/2026: 124 testes aprovados, zero falhas ou ignorados,
incluindo simulação Postman. Os 21 cenários do adaptador passaram novamente após
acrescentar verificações para as mensagens de contribuinte ausente e raízes
distintas. O teste de renderização agora usa proteção de dados efêmera e logging
isolado, sem depender de gravação no perfil/Event Log do Windows. A primeira
execução restrita encontrou duas falhas de permissões locais (Event Log e runtime
Postman); a regressão com acesso ao runtime local passou. Artefatos:
`lote-release.trx` e `lote-messages.trx` em `artifacts/lote-adapter`.

Republicação concluída: revisão `b96bb3952130caed2b9de81c70a5ce2def14d2f6`,
deploy `dep-dbqdpr67bikc739t6n8g`, estado `live` em
2026-11-11T21:24:53Z (18:24:53 de Brasília). Envio confirmado em `origin/main`.

Verificação HTTPS após o deploy: `/health`, `/`, `/conhecimentos`, `/chat.js` e
`/voice-input.mjs` responderam HTTP 200; Swagger sem credencial respondeu 401,
conforme esperado. Nenhum log de nível error/fatal retornado na janela consultada
de 21:24:53 a 21:25:21 UTC. Evidência: `release-http.json` no diretório local de
artefatos. O navegador integrado não conseguiu abrir a sessão nesta retomada;
a checagem desta revisão foi por HTTP, e os cenários de chat acima são da versão
inicial da entrega 011. A distinção das novas mensagens está coberta pelos testes
locais; não foi repetida no chat remoto nesta retomada.
