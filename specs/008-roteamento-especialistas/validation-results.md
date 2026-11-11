# Evidências da entrega 008

2026-11-11. Build .NET 10 aprovado. 70 testes aprovados, zero falhas: seletor por especialidade, gateway, acesso público, renderização, segurança/autorização, contratos e pipeline A2A com servidores HTTP sintéticos. Postman CLI 1.86.0: simulação existente chamada pelo cliente real, resposta correlacionada e resultado preservado em repositório de teste em memória.

Comando usado: `dotnet test SofiaInfis.Api.Tests --no-restore --filter 'FullyQualifiedName~SelectiveRoutingTests|FullyQualifiedName~AgentTraceRenderingTests|FullyQualifiedName~.Unit.|FullyQualifiedName~SofiaWebGatewayTests|FullyQualifiedName~CloudDeploymentTests|FullyQualifiedName~SofiaWebRenderingTests|FullyQualifiedName~A2ACompatibilityMatrixTests|FullyQualifiedName~SofiaDomainContractTests|FullyQualifiedName~StudentAgentCatalogTests|FullyQualifiedName~A2A03WireContractTests' --verbosity quiet`.

- NFS-e/XML e ECD/TXT identificados por conteúdo, não pelo MIME. Envio misto separado por especialista. Agente de legislação não chamado nesses cenários.
- Pergunta específica sem anexo: especialista recebe contexto-pergunta.txt explicitamente identificado, nunca documento alheio. Sem correspondência: zero chamadas A2A e resposta pelo RAG.
- A solicitação de análise genérica do frontend não inclui monitoramento legislativo apenas por essa permissão estar configurada. Isso evita cobertura incompleta falsa.
- Agente indisponível preserva os resultados dos demais. Permissões e inelegibilidade prevalecem sobre correspondência textual. Agente sem perfil não recebe broadcast.
- Renderização verificada no Edge headless: desktop 1440 px e viewport real 390 px; largura do conteúdo igual à viewport móvel, sem overflow horizontal. Capturas locais ignoradas pelo Git em artifacts/routing-preview. Identidade em caixa alta, autoria, avatar, motivo, documentos, estado e duração presentes.

Limites: regras locais explícitas por especialidade, sem classificador LLM; termos ambíguos ou não cadastrados podem resultar em RAG. Novos agentes exigem revisão do perfil. Não há garantia de entendimento semântico de paráfrases ou negações. A qualidade das análises e a disponibilidade de cada agente remoto continuam independentes do roteamento. Docker Desktop não iniciou; a suíte dependente de Mongo/PgVector não foi reexecutada. Não foram enviados documentos reais nem segredos a agentes de teste. Avisos preexistentes NU1902/NU1903 para System.Security.Cryptography.Pkcs 9.0.18 permanecem fora deste escopo.

Publicação e validação do ambiente público: pendentes no momento desta revisão.
