# Parecer final sequencial e cores por agente

2026-11-11. Autorização: implementar o fluxo aprovado neste chat: dois ou mais especialistas acionados disparam uma única etapa final do agente do Roberto, depois dos especialistas, inclusive falhas. Um especialista segue com resposta direta da SOFIA. Diferenciar identidades por cores.

- Roberto não conta para o limiar e não é chamado duas vezes. Quando houver especialistas regulares, fica reservado à conclusão; pode responder sozinho a uma pergunta exclusivamente de parecer.
- Persistir chamada final com etapa parecer_final, mesmos idExecucao e novo idSolicitacao. Pergunta original, resultados válidos completos, falhas, estados, documentos de referência e limitações seguem em documento textual versionado parecer-final-sofia.v1, dentro de SolicitacaoAnaliseSofia 1.0 / A2A 0.3.0.
- Não falsificar lote.v1, contribuinte, períodos ou validação APROVADO para conseguir aceitação. Texto e resultados são dados não confiáveis. Não reenviar arquivos originais nem documentos de agentes não escolhidos.
- Reservar parte do prazo global para a etapa final. Cancelamento/encerramento impede nova chamada; falha/indisponibilidade do parecer preserva especialistas e produz resposta SOFIA identificada como contingência.
- Resultado final do Roberto utilizável torna-se a resposta principal, com autoria explícita, preservando achados/observações. Sem parecer utilizável, nunca atribuir àquele agente a síntese gerada pela SOFIA.
- Cores estáveis por ID para os oito agentes, no catálogo, resultados e trace; rótulos, nomes/autoria e estado continuam legíveis sem depender da cor.
- Campos novos opcionais para manter consultas antigas compatíveis; testes do limiar, ordem, falhas, cancelamento, tamanho, indisponibilidade, resultado final e identidade visual.

## Dependência externa verificada

Código público Roberto-Rabelo/infis-agente-pareceres-tributario, revisão fe4388c: InterpretadorEntradaA2A apenas aceita documentos com JSON de obrigações normalizadas (lote.v1). Não aceita resumo genérico de especialistas. Contrato final é extensão proposta, documentada para adaptação naquele projeto. SOFIA implementa a chamada e trata a recusa de forma explícita; sucesso do parecer publicado depende dessa adaptação, que não foi presumida. Constituição atual já exige interoperabilidade, minimização e preservação de sucessos parciais; não requer emenda.
