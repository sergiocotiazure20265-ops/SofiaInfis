# Roteamento por especialidade e rastreamento visual

Data: 2026-11-11. Autorização: pedido do usuário neste chat para substituir o envio a todos por seleção baseada na pergunta/arquivos e melhorar o trace com nomes em caixa alta, desenvolvedor e avatar. Publicação no serviço SOFIA existente, conforme fluxo autorizado neste chat.

## Requisitos
- Filtrar agentes por autorização/capacidade e depois por perfil de especialidade revisado. A capacidade genérica analisar_documentos não basta.
- Comparar pergunta, nome e texto completo extraído com termos/assinaturas específicos, normalizando acentos e separadores e respeitando limites de palavras. Nunca usar somente PDF/TXT/XML como especialidade.
- Encaminhar a cada agente somente documentos compatíveis. Pergunta específica sem documento compatível é representada explicitamente como contexto textual da pergunta, mantendo o contrato 1.0 não vazio; não se apresenta como anexo do usuário.
- Perguntas sem anexo podem consultar especialistas quando há correspondência; sem correspondência, preservar RAG. Nenhum agente sem perfil é escolhido automaticamente. Sem cobertura, informar a ausência e responder pela SOFIA, sem broadcast.
- Catálogo com nomes em caixa alta, autoria conforme specs 006/007, avatar de iniciais, disponibilidade. Trace responsivo com agentes realmente chamados, motivo, quantidade de documentos, estados legíveis e duração. Preservar análises originais, voz e acesso público.
- Documento é dado não confiável. O roteamento é local/determinístico; texto não executa instruções nem altera destinos, permissões ou perfis. Não persistir trechos usados para seleção.

## Compatibilidade e aceite
Campos opcionais aditivos no catálogo e resumo de chamada. Sem alteração do wire A2A 0.3.0/JSON-RPC ou ResultadoAnaliseSofia 1.0, credenciais, destinos, limites e retenção. Constituição 1.0.0 já exige agentes necessários, rastreabilidade e testes; nenhuma emenda necessária. Reversão por commit anterior. Testar NFS-e isolada, ECD isolada, mistura com separação de documentos, pergunta sem anexo, ausência de correspondência, acentos, limites de palavras, autorização e indisponibilidade; validar dois agentes simulados e regressão web.
