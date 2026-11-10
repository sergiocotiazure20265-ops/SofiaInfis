# Descoberta e execução dos agentes dos alunos

Data: 2026-11-10. Pedido e autorização: cadastrar os agentes Marcos/NFS-e e Carol/ECD no módulo existente, configurar credenciais no servidor e validar antes da publicação. Expande a feature 001 sem alterar os contratos REST/A2A, a base RAG ou o ditado da 005. A constituição 1.0.0 já contempla interoperabilidade, privacidade, correlação e destinos; não requer emenda.

## Requisitos e aceite

- `A2A:IncludeStudentAgents=true` acrescenta o catálogo revisado em `a2a-student-agents.json` às opções `A2A:Agents`. Entradas explícitas com o mesmo IdAgente prevalecem, inclusive desabilitação. Demais agentes permanecem intactos; habilitação global continua explícita.
- Nomes exatamente iguais aos cards, URLs HTTPS completas do card/RPC, IPs públicos observados individualmente e capacidade `analisar_documentos`. Nenhum wildcard, CIDR ou desvio da validação DNS/URL/TLS/redirect.
- Descoberta e RPC usam `CredentialRef`/`DiscoveryCredentialRef=SOFIA_A2A_TOKEN`. O valor existe somente no ambiente protegido. O token de entrada `Sofia:ServiceToken` é independente e forte; a API armazena seu SHA-256 em `A2A:InboundClients`.
- Cliente Blazor recebe somente `a2a:agents:read`, `a2a:execute`, `a2a:read`, `a2a:cancel` e capacidade `analisar_documentos`. Credenciais nunca chegam ao navegador. O portão `Deployment:AccessKey` permanece em vigor.
- `A2A:Enabled=true` depende de MongoDB persistente disponível e inicializado; `Sofia:UseSpecialistAgents=true` habilita a interface. Sem Mongo cloud validado, não anunciar integração publicada nem habilitar um fluxo quebrado.
- Aceitar somente protocolo 0.3.0, transporte JSONRPC, identidade esperada e segurança HTTP Bearer declarada. Card ainda com X-Api-Key permanece inelegível; não converter o cliente para contornar essa falha.
- Documento sintético atravessa cliente Blazor, API, Mongo, descoberta, message/send, Task completed, Artifact único `analise-sofia-v1`, resultado 1.0 validado e consulta. Preservar idExecucao/idSolicitacao/idDocumento e identidade.
- UI mostra os agentes e disponibilidade; análises continuam em contribuições individuais e síntese opcional. Consulta textual sem anexo continua no RAG.
- Credenciais ausentes/incorretas são rejeitadas. Sem documentos reais nos testes. Evidência gravada contém apenas identidade pública, status, contagens e IDs sintéticos.

## Publicação e reversão

Validar build, contratos, segurança, dois agentes simulados, gateway e agentes publicados antes do deploy. Conclusão operacional exige também teste a partir do Render e interface publicada; execução local contra Vercel não comprova a rede de saída do Render. Registrar bloqueios e pendências por agente. Reversão: `Sofia__UseSpecialistAgents=false` e `A2A__Enabled=false`, preservando catálogo, segredos e registros; não remover dados.
