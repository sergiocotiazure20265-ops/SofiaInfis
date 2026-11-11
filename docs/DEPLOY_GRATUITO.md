# Publicação gratuita da SOFIA

## Arquitetura preparada

Um serviço **Render Free** executa Blazor e API no mesmo processo .NET 10,
com `Sofia__EmbeddedApi=true`. O Dockerfile é o existente em
`SofiaInfis.Web/Dockerfile`. O modo local com dois containers continua disponível.
O frontend chama a API por loopback; não precisa de CORS nem proxy pela Vercel.

**Neon Free** armazena os conhecimentos e embeddings PgVector.
**MongoDB Atlas Free** armazena execuções e análises especializadas no banco
`sofia-a2a`. O módulo A2A e o cliente servidor Blazor foram habilitados no Render
na entrega 006. Perguntas sem anexo continuam usando o RAG.
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
  `Host=HOST_NEON;Port=5432;Database=sofia_infis;Username=sofia_app;Password=SENHA;SSL Mode=VerifyFull;GSS Encryption Mode=Disable;Timeout=30;Maximum Pool Size=10`.
  Use o endpoint direto na inicialização/migração. Não inclua o prefixo `psql`.
- `OpenAI__ApiKey`: segredo de runtime. Nunca incluir em Git, imagem ou frontend.
- `Deployment__AccessKey`: chave aleatória gerada pelo Render para proteger a API,
  Swagger e OpenAPI. A chamada interna da Web usa `X-Sofia-Access-Key`; o segredo
  não integra o HTML. A interface, uploads pela interface e chat são públicos.
- `Deployment__RequireAccessKey=true`: a API falha fechada se a chave estiver ausente;
  páginas, arquivos estáticos, conexão Blazor e health check continuam públicos.
- `/health`: disponível ao health check do Render, sem acesso à base documental
  ou geração de tokens; verifica a conexão e tabela RAG.

Abra o endereço **HTTPS** do Render. A interface não pede usuário ou senha.
O servidor Blazor mantém as credenciais necessárias para chamar a API.

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

## Publicação concluída

Publicada em 2026-11-10:

- Frontend: https://sofia-infis.onrender.com/
- Swagger: https://sofia-infis.onrender.com/swagger
- Conhecimentos: https://sofia-infis.onrender.com/conhecimentos
- Painel: https://dashboard.render.com/web/srv-dbphv12jnfac739g79o0
- Código: https://github.com/sergiocotiazure20265-ops/SofiaInfis

O serviço foi criado por MCP no workspace confirmado pelo usuário, em plano
`free`, com Dockerfile da Web e variáveis protegidas autorizadas expressamente.
O arquivo `render.yaml` fica como referência reproduzível; não há Blueprint
ativo gerenciando esse serviço. Alterações operacionais atuais são feitas no
serviço existente. Deploy automático permanece desativado.

O conector GitHub mantinha uma conta sem escrita, mas a autenticação Git local
permitiu publicar no repositório autorizado, confirmada por dry-run e push.
Neon recebeu 35 registros com os mesmos IDs e embeddings, verificados após a
transferência; origem local preservada. Atlas, inicialmente adiado, foi provisionado
na entrega 006 em projeto separado e plano gratuito. Consulte o
[guia A2A dos alunos](../specs/006-agentes-alunos/quickstart.md) e as
[evidências por agente](../specs/006-agentes-alunos/validation-results.md).

O `render.yaml` conserva os defaults iniciais com agentes desligados para novas
instalações. A fonte de configuração do serviço existente é seu Environment;
uma futura migração para Blueprint deve reconciliar essas flags antes do sync.

Para acessar a interface, basta abrir o link, sem login. Para testar a API,
use `Deployment__AccessKey` da seção Environment do painel Render no cabeçalho
`X-Sofia-Access-Key`. Não compartilhe essa chave em arquivos públicos. Para repetir o
teste público (incluindo chamadas OpenAI reais), execute
`./scripts/Test-SofiaCloud.ps1 -TestOpenAI` e forneça a senha no prompt oculto.
As evidências e limites estão no relatório de validação da especificação 004.
