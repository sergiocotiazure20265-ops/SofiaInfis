# Baseline anterior à integração A2A

Data: 2026-11-05. Projeto original: SofiaInfis.Api, net10.0.

- POST /api/v1/sofia/perguntar: multipart, pergunta de 5–2.000 caracteres,
  arquivos opcionais; até cinco PDF/TXT, 5 MiB/arquivo, 20 MiB/requisição;
  extração truncada com marcador após 20.000 caracteres.
- POST /api/v1/conhecimentos/ingest: titulo/texto/fonte; chunking 1.200/200,
  embeddings de 1.536 dimensões e INSERT transacional parametrizado.
- POST /api/v1/conhecimentos/buscar: pergunta, topK padrão3, intervalo1–20;
  PostgreSQL/PgVector via Npgsql/Dapper, conexões construídas no serviço.
- Resposta da SOFIA: idExecucao, pergunta, resposta, arquivosProcessados,
  conhecimentosUtilizados, tokensEntrada/Saida/Total e duracaoMs.
- OpenAI direto, sem memória conversacional persistida. MongoDB está configurado,
  mas não era acessado pela aplicação. Nenhuma autenticação efetiva dos legados.
- Microsoft.Agents.AI.A2A 1.27.0-preview.261030.1 restaura A2A1.0.0-preview3;
  o perfil da turma exige0.3.0. Dependências de produção devem permanecer intactas.

Não foram copiados segredos, documentos ou valores de configuração local.
Os achados de speckit-analyze permanecem registrados no chat; a implementação
não altera spec/plan/contratos aprovados para corrigi-los implicitamente.
