# Plano

1. Inspecionar módulo existente e skills de implementação, contrato e conectividade.
2. Consultar os seis cards com Bearer, sem token e com token inválido. Registrar respostas DNS locais e Google Public DNS com contexto das redes Render aprovadas. Nunca presumir IPs por faixa.
3. Acrescentar entradas ao catálogo público distribuído com API/Web; preservar os dois anteriores. Restringir capacidades a skills confirmadas.
4. Paralelizar somente a descoberta, com semáforo MaxConcurrentCalls e Task.WhenAll para manter ordem. Extrair validação por card sem alterar suas regras. Testar oito servidores com barreira determinística, concorrência máxima três e ordem estável.
5. Tornar relatórios externos proporcionais a sucesso parcial: expor falha individual e preservar análise válida; não ocultar indisponibilidade de RPC. Capacidade de legislação autorizada no servidor, sem alterar credenciais de entrada/saída.
6. Compilar, executar regressão e teste sintético externo, publicar GitHub e Render, verificar catálogo/execução na instância pública. Documentar pendências por agente e DNS observado no ambiente real.

Sem mudança de esquema Mongo, RAG, CORS ou APIs públicas do domínio. SDKs existentes e transporte JSON-RPC 0.3.0 preservados. Reversão da expansão: desabilitar individualmente os novos IDs por configuração explícita; preservar registros e agentes anteriores.
