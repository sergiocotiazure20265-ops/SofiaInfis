# Publicação gratuita da SOFIA

## Arquitetura preparada

Um serviço **Render Free** executa Blazor e API no mesmo processo .NET 10,
com `Sofia__EmbeddedApi=true`. O Dockerfile é o existente em
`SofiaInfis.Web/Dockerfile`. O modo local com dois containers continua disponível.
O frontend chama a API por loopback; não precisa de CORS nem proxy pela Vercel.

**Neon Free** armazena os conhecimentos e embeddings PgVector.
**MongoDB Atlas M0** pode ser provisionado para histórico especializado futuro,
mas não é requisito do chat atual; A2A permanece desativado.
SQL Server, pgAdmin e Mongo Express são ferramentas/infraestrutura locais e não
precisam de serviços públicos para a aplicação funcionar.

O arquivo `render.yaml` define um único serviço com plano `free`, sem banco pago,
disco persistente ou atualizações automáticas de deploy. `deploy/neon-schema.sql`
contém o esquema aditivo. No Neon, o esquema é preparado antes do deploy e a
aplicação usa um usuário limitado a leitura/escrita, com
`Rag__InitializeDatabase=false`. Na stack Docker local a inicialização automática
continua disponível.

## Configuração no provedor

- `ConnectionStrings__PostgreSQL`: formato Npgsql, por exemplo
  `Host=HOST_NEON;Port=5432;Database=neondb;Username=USUARIO;Password=SENHA;SSL Mode=VerifyFull;Timeout=30;Maximum Pool Size=10`.
  Use o endpoint direto na inicialização/migração. Não inclua o prefixo `psql`.
- `OpenAI__ApiKey`: segredo de runtime. Nunca incluir em Git, imagem ou frontend.
- `Deployment__AccessKey`: senha aleatória gerada pelo Render. Usuário HTTP Basic:
  `sofia`. A senha protege interface, uploads e operações que consomem OpenAI.
  A chamada interna da Web usa `X-Sofia-Access-Key`; o segredo não integra o HTML.
- `Deployment__RequireAccessKey=true`: falha fechada se a senha estiver ausente.
- `/health`: disponível ao health check do Render, sem acesso à base documental
  ou geração de tokens; verifica a conexão e tabela RAG.

Abra o endereço **HTTPS** do Render. Ao primeiro acesso, informe usuário `sofia`
e a senha gerada em `Deployment__AccessKey`, obtida no ambiente do serviço.

## Migração dos conhecimentos existentes

Provisionar Neon e preparar o esquema antes de transferir dados. Copiar somente
a tabela `conhecimentos`, preservando IDs e embeddings existentes, sem gerar
novos embeddings. Conferir contagem e busca após importação. O banco local é
preservado; não deve ser eliminado nem convertido na origem.

As credenciais devem ser fornecidas por conexão autorizada ou configuração local
ignorada. Exportações com conteúdo da base devem ficar em `artifacts/`, ignorado,
e nunca em um repositório público. A transferência externa só pode começar após
identificar o projeto Neon e o repositório/serviço de destino da conta autorizada.

## Limites e custo

A hospedagem foi preparada somente para planos gratuitos. Render compartilha a
franquia mensal entre os serviços gratuitos e pode hibernar por inatividade;
conversas em memória podem ser perdidas em reinícios. Não contornar essas cotas
com pings artificiais. Neon e Atlas também têm cotas; não habilitar plano pago
nem autoscaling pago. OpenAI permanece cobrada conforme o uso da chave existente.

Referências verificadas durante a preparação:

- [Render Free](https://render.com/docs/free)
- [Blueprint do Render](https://render.com/docs/blueprint-spec)
- [Neon Free](https://neon.com/pricing)
- [PgVector no Neon](https://neon.com/docs/extensions/pgvector)
- [Atlas Free](https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/)
- [Runtimes Vercel](https://vercel.com/docs/functions/runtimes)

## Estado desta preparação

As contas Render, Neon e MongoDB Atlas foram sugeridas para conexão. Nenhum recurso
cloud, transferência da base ou URL pública é declarado criado até existir
confirmação pelos provedores. O checkout não possuía remoto Git; o repositório
`sergiocotiazure20265-ops/SofiaInfis` localizado pelo conector concede apenas leitura
à conta conectada. Resolver acesso ao repositório antes de enviar fontes ou ativar
o Blueprint. Preservar esta seção como registro da preparação; registrar publicação
efetiva e validação em um relatório separado quando concluídas.
