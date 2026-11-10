# Contratos — 001 Integração A2A

Artefatos de desenho; nenhuma rota nova implementada nesta etapa.
O domínio SOFIA 1.0 é normativo e independente do transporte 0.3.
Referência: [contrato compartilhado](../../../.agents/skills/sofia-a2a-contrato/references/contrato-sofia-v1.md).

## Arquivos

- [sofia-v1.schema.json](sofia-v1.schema.json): validação estrutural de solicitação
  e resultado; usar #/$defs/SolicitacaoAnaliseSofia ou #/$defs/ResultadoAnaliseSofia.
- [orquestracao.openapi.json](orquestracao.openapi.json): API HTTP aditiva da SOFIA.
- [examples](examples/): entradas/saídas sintéticas e envelopes A2A completos.
- [schema A2A oficial v0.3](https://raw.githubusercontent.com/a2aproject/A2A/v0.3.0/specification/json/a2a.json):
  referência externa dos envelopes, não substituída pelo schema de negócio.

O JSON Schema não verifica correlação com estado externo, igualdade contextual de
IDs, unicidade por propriedade nem fatos/evidências. Essas verificações pertencem
ao validador de aplicação e aos testes. Permitir campos adicionais opcionais v1
não permite mudar nomes, tipos ou obrigatoriedade dos campos existentes.

## API local adicional (proposta)

| Operação | Permissão | Sucesso |
|---|---|---|
| GET /api/v1/agentes | a2a:agents:read | 200 lista sanitizada, com elegibilidade/capacidades |
| POST /api/v1/orquestracoes | a2a:execute | 202 idExecucao/estado/Location após registro e fila |
| GET /api/v1/orquestracoes/{idExecucao} | a2a:read | 200 execução e página de resumos de chamadas |
| POST /api/v1/orquestracoes/{idExecucao}/cancelar | a2a:cancel | 200 estado atual, idempotente após encerramento |
| GET /api/v1/orquestracoes/{idExecucao}/chamadas/{idSolicitacao}/resultado | a2a:read | 200 resultado original de domínio, sem síntese |

GET de execução aceita cursor opaco e pageSize (padrão 20, máximo 100).
Início/consulta retornam links relativos; não aceitar URL externa do consumidor.
Resultado ainda não disponível: 409; falha técnica não é resultado de negócio,
portanto não converter em ResultadoAnaliseSofia fictício.
Cancelamento local é confirmado como 200 apenas depois de salvar o estado;
falha de persistência retorna 503 e não afirma encerramento salvo.

Erros: 400 validação; 401 credencial; 403 permissão; 404 ID inexistente/alheio/
expirado; 413 volume; 429 capacidade local esgotada; 503 módulo desativado,
fila indisponível ou armazenamento; 409 resultado ainda indisponível.
ProblemDetails com codigo, idExecucao quando disponível e traceId; sem payload
documental, nome, token, URL com segredo ou exceção interna.

Chamador usa Bearer opaco e identidade clientId validada, sem confusão com JWT.
Permissões não substituem ownership da execução. APIs antigas não são alteradas.
As novas rotas são contrato local proposto, não rotas padronizadas de A2A.

## Transporte remoto

No perfil fixado, enviar domínio serializado como string JSON UTF-8 no TextPart
de Message. Receber domínio no TextPart de Artifact de Task completed.
rpc.id, messageId, task.id e contextId são campos de transporte;
idExecucao/idSolicitacao/idDocumento/agente pertencem ao domínio SOFIA.

Agent Card exige protocolo 0.3.0, preferredTransport JSONRPC, identidade e skills.
A fixture usa http://localhost apenas para desenvolvimento isolado.
Em produção exigir HTTPS e destinos aprovados. Não colocar segredo no card.
Nome esperado do card e agente.idAgente retornado são vinculados pelo registry.

Conferir número/tipo de rpc.id correspondente, exatamente um result ou error,
kind/message role e IDs de Task, sem assumir que status.message é saída final.
Aceitar somente um Artifact relevante chamado analise-sofia-v1 com um TextPart
de domínio. Outros artefatos não ampliam permissões; duplicidade ambígua é erro.
Esse nome é convenção de interoperabilidade da turma, documentada nas fixtures.

## Limites e compatibilidade

1 MiB por envelope A2A externo já serializado, incluindo escape do JSON interno;
mesmo teto no corpo de criação local. Ler stream com limite, não confiar no header.
Payload que cresce além do teto ao envelopar é rejeitado antes de delegar.
Nome do usuário/pergunta/documentos seguem limites da spec.
Custo/tokens não têm campo padrão A2A: consumo ausente fica null no registro.
Extensão futura de consumo exige acordo e testes; não alterar contrato v1 por suposição.

Fixtures corrigem omissões dos exemplos conceituais de envelope sem alterar as
skills nem o contrato de negócio. Campos extras opcionais são compatíveis;
breaking change de negócio/protocolo exige aprovação e versão correspondente.

## Critérios de contrato na implementação

- Validar examples de domínio e wire contra seus schemas e round-trip.
- Rejeitar ausências, null indevido, versão/status desconhecido e JSON duplicado.
- Conferir IDs/agente/documentos por chamada, sem aceitar referência externa.
- Demonstrar Task ainda ativa, polling, completed/erro de negócio e falha técnica.
- Tratar texto como dado; verificar logs sem conteúdo sensível.
