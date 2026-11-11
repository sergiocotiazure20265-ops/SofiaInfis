# Resposta automática mesmo com análise parcial

2026-11-11. Autorização: usuário relatou análise parcialmente concluída sem resposta e pediu correção/melhoria para sempre retornar uma resposta. O problema observado no código é a síntese depender de ação manual após o despacho A2A. Publicar no serviço existente conforme o fluxo autorizado neste chat.

- Gerar resposta da SOFIA automaticamente após resultados utilizáveis, inclusive parciais, preservando estados e análises originais.
- Se todos os agentes falharem, tentar responder pela SOFIA com os documentos originais; indicar explicitamente a falha dos especialistas.
- Falha/timeout/resposta vazia ou limite de síntese não podem deixar Answer vazio: compor resposta local com análises, achados, evidências e observações recebidos, agentes indisponíveis e próximos passos. Sem resultados, explicar que não foi possível validar e solicitar complementação/reenvio, nunca inventar achados.
- Cancelamento do usuário não dispara nova chamada ao modelo. A resposta informa a interrupção e preserva contribuições já recebidas.
- Síntese tem orçamento próprio limitado, independente do prazo de acompanhamento A2A. Garantia é de conteúdo explicativo enquanto a sessão está ativa; desconexão total/fechamento do navegador impede garantia de entrega.
- Resposta principal visível antes das contribuições. Trace começa recolhido. Manter indicação de parcial/falha; resposta de contingência não implica análise bem-sucedida. Permitir tentar a síntese novamente quando a contingência foi usada.
- Não alterar wire A2A, contratos SOFIA 1.0, seleção de especialistas, autenticação ou persistência. Constituição atual já exige sucesso parcial, rastreabilidade e preservação: nenhuma emenda necessária.
