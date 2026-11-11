# Evidências da entrega 009

2026-11-11. Causa encontrada: SubmitAsync recebia análises e encerrava a execução sem chamar ConsolidateAsync; a síntese era manual e o trace aberto ocupava a área visível.

Build .NET 10 aprovado. 84 testes aprovados, zero falhas, incluindo 11 cenários novos de resposta automática: sucesso/parcial, timeout, serviço indisponível, resposta vazia, volume acima do limite sem truncamento, falha de todos os agentes com RAG complementar, dupla falha, cancelamento sem nova chamada e nova tentativa de síntese sem repetir agentes. Estados e IDs originais preservados. Simulação Postman, servidores A2A locais, roteamento seletivo, contratos e regressão de segurança/frontend também passaram.

Comando: `dotnet test SofiaInfis.Api.Tests --no-restore --filter 'FullyQualifiedName~AutomaticAnswerTests|FullyQualifiedName~SelectiveRoutingTests|FullyQualifiedName~AgentTraceRenderingTests|FullyQualifiedName~.Unit.|FullyQualifiedName~SofiaWebGatewayTests|FullyQualifiedName~CloudDeploymentTests|FullyQualifiedName~SofiaWebRenderingTests|FullyQualifiedName~A2ACompatibilityMatrixTests|FullyQualifiedName~SofiaDomainContractTests|FullyQualifiedName~StudentAgentCatalogTests|FullyQualifiedName~A2A03WireContractTests' --verbosity quiet`.

Não se garante conclusão factual quando faltam dados/evidências ou entrega após fechamento/desconexão total do navegador. A resposta de contingência explica a limitação, sem inventar validação. Docker local permanece indisponível: regressão específica Mongo/PgVector não reexecutada; integração sintética utiliza armazenamento de teste em memória. Avisos preexistentes NU1902/NU1903 permanecem fora deste escopo.

Publicação e teste sintético online: pendentes.
