# Publicação em planos gratuitos

Solicitação: publicar a aplicação integrada em serviços gratuitos. O usuário
informou que vai conectar Render, Neon e MongoDB Atlas. Isso não comprova que as
conexões já tenham sido concluídas.

Preservar .NET 10/Blazor; hospedar frontend e API no mesmo processo, por opção
`Sofia:EmbeddedApi`, em um único serviço Render Free. Manter Docker local separado
como padrão. Neon Free recebe PgVector; Atlas M0 fica reservado ao módulo A2A
opcional, que continua desativado. Nenhum plano pago é autorizado.

Na publicação, proteger interface e API com senha de acesso de runtime, incluindo
ingestão e consumo da OpenAI. Sem senha obrigatória configurada, recusar acesso.
Não embutir segredos no HTML, Git ou imagem. Health check permanece disponível.

Migrar os conhecimentos preservando IDs e embeddings, sem modificar a origem.
Confirmar titularidade/projeto/repositório antes das operações externas. Limites
de cotas, hibernação e cobrança OpenAI devem ser explícitos. Publicação só estará
concluída após provedores confirmarem criação e os fluxos funcionarem por HTTPS.
