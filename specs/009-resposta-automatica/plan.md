# Plano
1. Compositor local de resposta de contingência, sem LLM, sem descartar análises recebidas.
2. Síntese automática com timeout próprio, validação de texto não vazio e tratamento das falhas esperadas; fallback do RAG se não houver análise utilizável.
3. Proteção de saída no serviço e na interface para erros/validação/cancelamento; preservação de estado e IDs.
4. Interface com resposta principal, indicador de síntese, retry de síntese opcional e trace recolhido.
5. Testes de resultados parciais, falhas, resposta vazia, cancelamento e volume; regressão, deploy e validação sintética online.
