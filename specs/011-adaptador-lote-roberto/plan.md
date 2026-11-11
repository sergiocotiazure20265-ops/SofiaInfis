# Plano

1. Incorporar schema oficial e exemplo público com fonte registrada; validar em runtime com JsonSchema.Net 7.3.4 já usado nos testes.
2. Adaptador puro e leitores determinísticos; relatórios explícitos associados por nome exato único do arquivo. Lotear uma raiz de CNPJ, preservar registros e relatórios, rejeitar dados insuficientes/ambíguos sem truncar.
3. Substituir pacote genérico pelo lote.v1 na última chamada. Manter pacote de análises como contexto adicional, sem supor que o motor remoto o interpreta.
4. Estado não executada e motivo para conclusão SOFIA. Autoria real e limitações determinísticas mesmo se a síntese falhar.
5. Testes de normalização, schema, pipeline com HTTP real local, falhas, UI, regressão e verificação publicada. Não modificar código de Roberto.
