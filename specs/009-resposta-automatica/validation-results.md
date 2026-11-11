# Evidências da entrega 009

2026-11-11. Causa encontrada: SubmitAsync recebia análises e encerrava a execução sem chamar ConsolidateAsync; a síntese era manual e o trace aberto ocupava a área visível.

Build .NET 10 aprovado. 84 testes aprovados, zero falhas, incluindo 11 cenários novos de resposta automática: sucesso/parcial, timeout, serviço indisponível, resposta vazia, volume acima do limite sem truncamento, falha de todos os agentes com RAG complementar, dupla falha, cancelamento sem nova chamada e nova tentativa de síntese sem repetir agentes. Estados e IDs originais preservados. Simulação Postman, servidores A2A locais, roteamento seletivo, contratos e regressão de segurança/frontend também passaram.

Comando: `dotnet test SofiaInfis.Api.Tests --no-restore --filter 'FullyQualifiedName~AutomaticAnswerTests|FullyQualifiedName~SelectiveRoutingTests|FullyQualifiedName~AgentTraceRenderingTests|FullyQualifiedName~.Unit.|FullyQualifiedName~SofiaWebGatewayTests|FullyQualifiedName~CloudDeploymentTests|FullyQualifiedName~SofiaWebRenderingTests|FullyQualifiedName~A2ACompatibilityMatrixTests|FullyQualifiedName~SofiaDomainContractTests|FullyQualifiedName~StudentAgentCatalogTests|FullyQualifiedName~A2A03WireContractTests' --verbosity quiet`.

Não se garante conclusão factual quando faltam dados/evidências ou entrega após fechamento/desconexão total do navegador. A resposta de contingência explica a limitação, sem inventar validação. Docker local permanece indisponível: regressão específica Mongo/PgVector não reexecutada; integração sintética utiliza armazenamento de teste em memória. Avisos preexistentes NU1902/NU1903 permanecem fora deste escopo.

## Publicação e verificação online

Commit `42a6b35`, deploy `dep-dbqaktg8v5cc739jusf0`, live em 2026-11-11 17:49:29 UTC no serviço existente sofia-infis, sem alterar ambiente, credenciais ou seleção.

Teste no chat HTTPS público, identificação fictícia e XML NFS-e mínimo/sintético: somente AGENTE VALIDADOR NFS-E chamado. Agente retornou status Parcial; a SOFIA apresentou automaticamente texto não vazio com convergências, achados, limitações e próximos passos, sem ação manual de síntese. Trace recolhido, nenhum botão Gerar síntese final e nenhum aviso de erro. Logs Render de nível error vazios no período da validação.

Capturas locais ignoradas pelo Git: artifacts/automatic-answer/live-desktop-verified.png e live-mobile-verified.png. Conferidas em viewport desktop 1440x1100 e móvel 390x1000; conteúdo móvel com largura 390, sem overflow horizontal. Nenhum documento real foi utilizado. Falhas de síntese foram injetadas somente nos testes locais, sem prejudicar o serviço publicado. A assertiva adicional de trace recolhido também passou nos quatro testes de renderização.

