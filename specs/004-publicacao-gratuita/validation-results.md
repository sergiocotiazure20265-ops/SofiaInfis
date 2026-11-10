# Preparação e provisionamento cloud

Data: 2026-11-10 (America/Sao_Paulo).

## Validação local do deploy

- Imagem `sofia-infis-cloud-check` construída com Dockerfile da Web, Release .NET 10.
- Host único em Production validado em container temporário na porta local 5285:
  `/health` RAG 200; acesso anônimo 401; interface autenticada 200; Swagger contém
  ingestão documental; POST inválido à API embutida 400.
- Container temporário encerrado após a verificação. A stack local original foi
  preservada.
- 10 testes direcionados aprovados: autenticação do deploy, falha fechada sem
  segredo obrigatório, gateway Web e renderização. Fonte:
  `CloudDeploymentTests`, `SofiaWebGatewayTests`, `SofiaWebRenderingTests`.
- O modo cloud exige `Sofia:EmbeddedApi=true`; o modo local separado permanece
  padrão. Nenhuma credencial é incluída no HTML, no Blueprint ou na imagem.

## Contas e escolhas confirmadas

- Usuário autorizou “Sergio's workspace”, `sergio.coti@gmail.com`, no Render.
- Usuário escolheu publicar primeiro sem MongoDB Atlas. O conector Atlas retornou
  que o acesso de clientes IA/MCP está desativado em todas as organizações.
- Neon: organização “Vercel: COTI”, plano free, projeto `neon-cyclamen-kite`.
  A organização é gerenciada pela Vercel e bloqueou criar projeto novo pelo MCP.
  Usuário autorizou um banco separado nesse projeto, preservando os bancos atuais.
- Banco `sofia_infis` criado no branch `main`, PgVector habilitado, tabela
  `conhecimentos` e índice HNSW criados. Usuário `sofia_app` com leitura/escrita
  restrita ao banco; não usa a conta administrativa no aplicativo.
- Render não tinha serviços criados na leitura inicial desta entrega.

## Transferência da base

Usuário autorizou expressamente transferir os 35 trechos locais, incluindo títulos,
textos, fontes, IDs e embeddings, para esse banco Neon. A revisão automática havia
bloqueado o envio até essa autorização específica.

A tentativa de transferência por conexão PostgreSQL direta retornou erro de
transporte Npgsql antes de confirmar qualquer escrita. A cópia passou a usar o
conector Neon autorizado, em lotes transacionais idempotentes. Cada lote compara
os IDs e o hash dos campos e vetores com a origem antes de confirmar a transação.
Não gera novos embeddings, não substitui conflitos divergentes nem remove dados
locais.

Cópia concluída: **35 registros importados e verificados**, com os mesmos IDs,
conteúdos e embeddings. A verificação final no Neon retornou 35 registros e vetores
com 1.536 dimensões. A origem local permaneceu com 35 registros. Nenhuma chamada
OpenAI foi necessária para essa migração.

Referências operacionais (sem credenciais): projeto Neon `autumn-river-56979290`,
branch `br-summer-cake-b82vrmlm`, banco `sofia_infis`, role `sofia_app`; workspace
Render confirmado `tea-dbphd5k9v7es73bthsqg`. As conexões de runtime podem ser
obtidas pelo conector Neon com o role limitado e devem ser fornecidas somente ao
ambiente do serviço, sem armazenamento em Git.

## Publicação pendente

O conector GitHub ainda retornou a conexão `sergiocotiazure20262-lang`, sem escrita.
Entretanto, o teste `git push --dry-run` com a autenticação Git local confirmou
acesso de publicação em `sergiocotiazure20265-ops/SofiaInfis`. O fluxo continuará
por esse acesso Git autorizado, sem trocar o repositório nem expor credenciais.
Nenhuma URL de aplicação pública é considerada pronta antes da confirmação do
deploy e da verificação HTTPS.
