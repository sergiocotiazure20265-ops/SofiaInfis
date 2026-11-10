# Expansão do catálogo A2A dos alunos

Data: 2026-11-10. Pedido explícito: interromper o diagnóstico anterior e cadastrar mais seis agentes. Preservar os dois agentes da feature 006, credenciais e proteções. A constituição 1.0.0 e o contrato SOFIA 1.0 permanecem vigentes; não há mudança do protocolo.

| Aluno | IdAgente | Nome exato | Host / RPC |
|---|---|---|---|
| Roberto Rabelo | agente-parecer-tributario | Agente Parecer Tributário | agente-parecer-tributario.vercel.app /a2a |
| Kaio Macedo | agente-ecf | Agente ECF | agente-ecf-agente.vercel.app /a2a |
| Marcus Oliveira | agente-fiscal | Agente Fiscal INFIS | agente-classificacao-fiscal.vercel.app /a2a |
| Andressa | agente-auditoria-nfe | AgenteAuditoriaNFe | agente-auditoria-nfe.vercel.app /a2a/auditoria-nfe |
| Matheus do Valle | agente-dimp | Agente Fiscal DIMP INFIS | agente-dimp-demo.vercel.app /a2a |
| Lettícia Luize | agente-monitoramento-legislacao | Agente de Monitoramento da Legislação e Jurisprudência | agent-monitoramento-legislacao.vercel.app /a2a |

## Requisitos

- Oito agentes no catálogo aditivo; agentes/configurações explícitas existentes prevalecem. Nenhum ID duplicado.
- Todos usam analisar_documentos; legislação também autoriza monitorar_legislacao_tributaria, confirmada no card. Acrescentar essa capacidade ao cliente servidor Blazor e ao hash/cliente já existente no Render, sem trocar tokens.
- Cards em HTTPS `/.well-known/agent-card.json`, RPC exatamente como tabela, AllowedDestinations explícitas, IPs realmente observados por domínio, CredentialRef e DiscoveryCredentialRef SOFIA_A2A_TOKEN. Não versionar valor do segredo.
- Confirmar 0.3.0/JSONRPC, identidade, Bearer, skills, DNS e HTTPS. Não transformar HTTP 200 do card em prova de execução; registrar RPC indisponível, Task/Artifact inválido e falha de correlação por agente, mantendo os demais resultados.
- Com oito agentes e timeout de descoberta de cinco segundos, descoberta sequencial poderia consumir quarenta segundos e ultrapassar os trinta do frontend. Aprova-se, neste escopo, descoberta paralela limitada pelo MaxConcurrentCalls existente (três), mantendo ordem do registry, timeout individual, cancelamento e todas as validações. Não aumentar concorrência ilimitadamente.
- Testar com dois ou mais agentes simulados, limite de concorrência, catálogo preservado e documentos sintéticos contra agentes publicados. Validar no Render após publicação; só anunciar execução dos agentes que efetivamente retornarem resultado válido.
- A descrição duplicada de DIMP no bloco de legislação não altera sua identidade: usar skills e nome do Agent Card efetivamente observado, mantendo DIMP como agente separado.

## Limites

Roteamento continua por capacidade: analisar_documentos pode enviar a mesma fixture a todos os especialistas. Não implementar seleção por formato nesta expansão. Não retomar o diagnóstico do erro anterior do chat, expressamente interrompido pelo usuário. Nenhum documento real nos testes.
