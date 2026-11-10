# Correção de rejeição do chat online com anexos

Data: 2026-11-10. Evidência do usuário: frontend em sofia-infis.onrender.com lista os agentes, mas o envio de XML sintético retorna “A API não aceitou os dados”. Autorização: corrigir a incompatibilidade do fluxo existente preservando contrato, credenciais e validações.

## Causa reproduzida

SofiaOptions.Capabilities é inicializado com analisar_documentos. O binding de configuração acrescenta Sofia:Capabilities:0=analisar_documentos ao array existente. O gateway enviava ambas as ocorrências e a API rejeitava a requisição com INVALID_ANALYSIS_REQUEST, pois capacidades duplicadas são inválidas. A configuração real do Render tem também monitorar_legislacao_tributaria.

O teste de integração anterior construía Options.Create(new SofiaOptions), sem exercitar o binding que ocorre no programa real. Novo caso com configuração vinculada e XML sintético reproduziu a mesma mensagem do usuário: um caso aprovado sem configuração de capabilities e um reprovado com ela, antes da correção.

## Comportamento e aceite

- Gateway envia cada capacidade configurada uma única vez, comparação ordinal, preservando ordem e todas as skills autorizadas. Não descartar skills, documentos ou valores inválidos silenciosamente.
- API continua rejeitando requisições malformadas/duplicadas de outros clientes; nenhum relaxamento de autorização, destinos, correlação, tokens, protocolo ou contrato.
- XML sintético atravessa preparação, gateway, API, Mongo isolado e dois agentes simulados; execução e síntese separada continuam funcionando com ou sem Sofia:Capabilities explícito.
- Teste adicional confirma que analisar_documentos e monitorar_legislacao_tributaria chegam uma vez cada e textoCompleto permanece true.
- Teste externo opt-in usa ISofiaChat resolvido pelo DI do programa Web real, configuração equivalente à produção e API publicada, somente XML fictício. Não chama síntese OpenAI.
- Publicar correção no GitHub/Render e distinguir contrato aceito pela SOFIA das pendências remotas já registradas para NF-e/DIMP e insumos dos especialistas.

A constituição e contrato 1.0 permanecem válidos. Não alterar dados RAG, Mongo ou segredos. Após deploy, o usuário deve recarregar o chat para abrir circuito da nova versão.
