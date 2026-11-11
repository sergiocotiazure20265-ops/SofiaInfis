# Adaptador do contrato existente de Roberto

Autorizado pelo usuário em 2026-11-11: implementar e publicar somente na SOFIA; sem dados suficientes, a própria SOFIA conclui e explica limitações. Substitui o pacote genérico da feature 010 na chamada final.

O adaptador aceita lotes/obrigações já normalizados e monta lote a partir de NF-e XML 4.00 e EFD ICMS/IPI 020 com relatório explícito de origem associado ao arquivo. Campos são extraídos deterministicamente; não usar LLM para preencher registros, CNPJ, recibo, período ou aprovação. Demais tipos são aceitos já normalizados no contrato oficial; extração bruta sem mapeamento/relatório deve explicar a limitação.

Validar schema oficial, CNPJ/DV, períodos, identidade, layouts, duplicidades, limites e validação de origem. Não omitir documento fiscal não adaptável para fabricar um lote aparentemente completo. Falta/ambiguidade cancela somente a chamada de Roberto; preservar análises e gerar conclusão SOFIA. Registrar etapa não executada por insuficiência, distinguindo erro remoto e cancelamento.

Manter A2A 0.3.0 / envelope SOFIA 1.0, ordem final única, cores, autorização e correlação. Sem mudança no servidor de Roberto. O parecer dele continua limitado às regras que implementa; não tratar como consolidação livre de todas as especialidades.

Constituição vigente atende ao escopo, sem emenda. Impacto: pipeline final, normalização, rótulos/conclusão, contratos e testes. Reversão pelo commit anterior. Sem alterar os endpoints existentes.
