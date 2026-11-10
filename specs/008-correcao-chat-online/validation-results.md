# Validação da correção do chat online

Data: 2026-11-10. Somente dados sintéticos; nenhuma síntese OpenAI acionada.

## Reprodução e correção

Logs do Render mostraram INVALID_ANALYSIS_REQUEST nas tentativas do chat, inclusive em 16:17 UTC. A captura do usuário confirma o endereço público e a listagem de agentes já carregada. O teste de integração passou a usar IConfiguration.Get<SofiaOptions> em vez de somente Options.Create manual, com Sofia:Capabilities:0 configurado como na publicação.

Antes da correção: dois casos executados, um aprovado sem capacidades explícitas e um reprovado com capacidades configuradas. A falha reproduziu exatamente a mensagem “A API não aceitou os dados. Confira os documentos e o tamanho da pergunta.”. O binding acrescentou analisar_documentos ao valor padrão já inicializado; o gateway enviou duplicata e o validador da API a rejeitou.

Correção localizada em LiveSofiaChat.SubmitAsync: Distinct(StringComparer.Ordinal) das capacidades antes de criar CriarOrquestracaoRequest. Todos os valores distintos e sua ordem são preservados. Validação/autorização da API não foi alterada.

## Testes

- Build .NET aprovado, zero erros; avisos preexistentes de Pkcs permanecem.
- Gateway e integração com API/Mongo isolados/dois agentes simulados: 9 testes aprovados, zero falhas, 6 s. Inclui XML sintético com/sem capacidades configuradas, duas skills preservadas sem repetição e síntese separada simulada.
- Regressão completa sem opt-ins externos: 96 aprovados, zero falhas, 33 s.
- Novo teste opt-in SofiaOnlineGatewayTests resolve ISofiaChat pelo próprio DI do programa Web, configura as duas skills e credenciais como na produção e envia somente o XML fictício do exemplo à API HTTPS publicada. Um teste aprovado, 22 s. Execução c5b57732-dbcf-48b3-837f-0785c11c2abd, estado parcial, com resultados válidos de NFS-e/ECD/legislação e respostas de negócio dos demais agentes que atenderam. DIMP continuou rejeitado pela divergência de nome já documentada na feature 007. ID da execução conferido nos resultados.

O teste externo acima usa gateway/configuração reais e a API online, mas o processo do gateway é o host Web de testes local. Não foi uma inspeção visual automatizada do navegador público. Esse limite é registrado para não repetir a lacuna dos testes anteriores, que não exerciam o binding.

## Publicação

Correção publicada no GitHub, revisão runtime `6d897cf7a5ed8ee7315dd52ebdf624e19321ec2e`. Deploy `dep-dbpkarek1f9s739e7dh0` confirmado live em 2026-11-10 16:26:27 UTC (13:26:27 America/Sao_Paulo). Nenhuma variável, token, dado, esquema ou proteção foi alterada.

Após o deploy, repetido SofiaOnlineGatewayTests contra a publicação: 1 aprovado, 23 s. Execução `b29652ab-e486-4b67-b0b2-05254a672ed3`, estado parcial. Oito chamadas acompanhadas pelo gateway, seis resultados de domínio aceitos e duas falhas remotas conhecidas: AGENT_HTTP_FAILURE em NF-e e RESULT_CORRELATION_MISMATCH em DIMP. A API aceitou o envio do XML; não ocorreu a rejeição de capacidades duplicadas.

A inicialização do Mongo registrou indisponibilidade transitória imediatamente após o deploy, recuperada antes do teste final. A execução e suas consultas persistentes foram concluídas com sucesso; não confundir esse evento inicial com a rejeição HTTP 400 corrigida.

Instrução ao usuário: Ctrl+F5, iniciar nova conversa e reenviar o XML sintético. Isso abre circuito Blazor da revisão corrigida. A inspeção visual interativa do navegador público não foi automatizada nesta entrega; as evidências cobrem reprodução local da falha, gateway/DI real e API online após publicação.
