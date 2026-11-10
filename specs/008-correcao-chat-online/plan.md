# Plano

1. Consultar logs sanitizados do Render, inspecionar DTO/validador/gateway e reproduzir o envio com opções vinculadas por IConfiguration.
2. Demonstrar falha antes da alteração e deduplicar capacidades somente na montagem da requisição do cliente Web.
3. Cobrir integração com dois agentes, XML e síntese sintética, e preservação de ambas as skills autorizadas.
4. Criar teste externo opt-in que resolve o gateway pelo DI Web real, usa a API HTTPS autorizada e credenciais por ambiente protegido; relatório contém apenas IDs/status/códigos.
5. Compilar, executar regressões, publicar GitHub/Render e validar o gateway configurado contra o ambiente público. Registrar evidências e limites, preservando pendências externas da feature 007.

Nenhum pacote, endpoint, esquema de banco ou regra da API muda. Reversão: revisão anterior do gateway, sem migração de dados; voltar também à configuração sem arrays explícitos só como intervenção operacional revisada, sem reutilizar token de saída como token de entrada.
