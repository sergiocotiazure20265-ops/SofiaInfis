# Modelo de dados — 001 Integração A2A

Desenho proposto, sem classes, coleções ou índices criados nesta etapa.
Contrato canônico: [SOFIA 1.0](../../.agents/skills/sofia-a2a-contrato/references/contrato-sofia-v1.md).
Validação estrutural: [schema](contracts/sofia-v1.schema.json).

## 1. Domínio transportado

| Entidade | Campos e regras |
|---|---|
| SolicitacaoAnaliseSofia | versaoContrato=1.0; idExecucao/idSolicitacao UUID; usuario.nome não vazio; pergunta; documentos não vazios; contexto opcional |
| UsuarioContexto | nome transitório, 1–200 caracteres; não autentica |
| DocumentoAnalise | idDocumento único na execução, nome/texto não vazios; tipo MIME opcional; texto 1–20.000 caracteres; não é binário |
| ResultadoAnaliseSofia | versão/IDs preservados; agente.idAgente/nome; status; analise não vazia; achados obrigatório; observacoes e erro opcionais/nulos |
| AchadoAnalise | idAchado, descricao, idDocumentos e evidencias; referências válidas; severidade opcional baixa/media/alta/critica |
| ErroAnalise | codigo e mensagem não sensíveis; obrigatório quando status=erro |
| ConsumoInformado | entrada/saída/total/custo nullable no registro técnico; não é campo nativo do domínio/A2A |

JSON novo usa grafia exata do contrato; não depender de renomeação implícita C#.
Campos desconhecidos opcionais são tolerados no domínio v1, sem serem executados.
Caracteres do novo contrato são contados por escalares Unicode; validar com Rune,
alinhado ao schema, sem alterar StringLength/limites dos contratos antigos.

### Validação contextual adicional ao JSON Schema

- Correlacionar execução, solicitação e agente com a chamada registrada.
- Validar idDocumentos de cada achado contra o conjunto enviado e IDs únicos.
- Não aceitar JSON com propriedades duplicadas, campos obrigatórios nulos/brancos,
  retorno sem achados ou status desconhecido.
- Evidência vazia não prova ausência de divergência; registrar limitação declarada.
  Autenticidade factual da análise depende do especialista e de seus testes.
- Validar bytes do envelope externo já serializado, não somente do JSON interno.
- Não detectar confiabilidade por conteúdo textual: documentos sempre são dados
  não confiáveis. Truncamento conhecido é condição explícita da entrada; um marcador
  que o usuário digitou não autoriza tratar conteúdo como instrução.
- Para o novo corpo local, campo opcional textoCompleto por documento declara
  completude (default true para texto fornecido diretamente). false impede envio.
  Adaptadores de extração devem fornecer o estado real; não inferir completude
  após a extração atual ter sinalizado truncamento.

## 2. Catálogo configurado de agentes (não público integralmente)

| Campo | Função |
|---|---|
| idAgente | Chave canônica estável, única |
| expectedName | Nome esperado do card e resultado |
| cardUri | Endereço aprovado de descoberta |
| enabled | Habilitação explícita |
| allowedDestinations | Origens/portas/caminhos/redes permitidos para card e RPC |
| allowedCapabilities | Skills que a INFIS autoriza expor |
| credentialRef | Referência a segredo externo; nunca exposta em consulta |
| discoveryCredentialRef | Credencial separada se o card for protegido |

Card fornece skills/modos/versões/endpoints, não concede permissões.
Exigir protocolo 0.3.0, JSONRPC, suporte ao modo textual e skill identificada.
Publicar somente informações sanitizadas aos chamadores autorizados.
IDs/nome internos de configuração têm validação de tamanho e sintaxe,
sem adicionar restrições incidentais ao contrato de negócio dos alunos.

## 3. Entrada da operação local

CriarOrquestracaoRequest: usuario, pergunta, documentos, capacidades desejadas.
Não exige idExecucao nem idSolicitacao fornecidos pelo consumidor.
SOFIA gera ambos; se o fluxo estiver vinculado a execução interna já existente,
a aplicação preserva seu ID sem permitir referência arbitrária a execução alheia.

O primeiro HTTP POST sempre inicia uma execução nova; não há retry automático nem
garantia de idempotência entre dois POST independentes. Idempotência das chamadas
a agentes é por idSolicitacao/idAgente, não pelo texto da pergunta.

## 4. Persistência: a2a_execucoes

| Campo | Tipo lógico / uso |
|---|---|
| _id | UUID de execução em string canônica |
| ownerClientId | Identidade autenticada estável, não usuario.nome |
| ownerRunId | Identificador do processo que aceitou a execução |
| version | Inteiro incrementado em CAS |
| estado | em_andamento/concluida/parcial/falhou/cancelada/interrompida |
| capacidadesSolicitadas / semCobertura | IDs de capacidades |
| documentoIds | Somente IDs; não nomes/textos/pergunta |
| agentesSelecionados | IDs canônicos e posição estável |
| criadoEm / iniciadoEm / encerradoEm | UTC, encerramento nullable enquanto aberto |
| deadline | Prazo total contado da aceitação |
| expiresAt | Encerramento+30 dias; provisório conservador enquanto aberto |
| falhaRegistro | Código técnico sanitizado, se registro parcial recuperável |
| resumo | Contagens de chamadas/resultado, sem embutir análises |

Não persistir nome do usuário, pergunta, texto bruto, token nem card bruto.
Análises podem conter dados sensíveis e são armazenadas somente nos registros
filhos protegidos. Não há transação global implícita.

Índices: _id único; ownerClientId/criadoEm/_id para consulta;
estado/ownerRunId para reconciliação; TTL single-field expiresAt (expireAfterSeconds=0).

## 5. Persistência: a2a_chamadas

| Campo | Tipo lógico / uso |
|---|---|
| _id | idSolicitacao UUID em string |
| idExecucao / idAgente / ordem | Vínculo e ordenação estável |
| ownerRunId / version | Fencing local e CAS |
| estado | aguardando/em_andamento/concluida/falhou/timeout/cancelada/interrompida |
| taskId / contextId / messageId / rpcIds | IDs técnicos, sem confundir com IDs de domínio |
| criadoEm / enviadoEm / encerradoEm | UTC |
| deadline | Menor prazo entre chamada e execução |
| resultado | ResultadoAnaliseSofia validado, nullable |
| falhaTecnica | Categoria/código/mensagem sanitizada, nullable |
| consumo | Valores informados ou null, com origem; nunca credenciais |
| duracaoMs | Duração medida da chamada |
| expiresAt | Mesma expiração final da execução |

Uma Task completed com status de negócio erro é chamada concluida contendo
resultado.status=erro, não falha de transporte. Não é contribuição utilizável.
Cada chamada <= envelope aceito + metadados controlados, bem abaixo de 16 MiB.
Limitar também bytes BSON antes de gravar; falha de armazenamento permanece explícita.

Índices: _id único; idExecucao/idAgente único; idExecucao/ordem/_id;
TTL single-field expiresAt. Repetição não cria segunda chamada.

## 6. Estados e transições

### Chamada

aguardando → em_andamento → concluida ou falhou ou timeout.
aguardando/em_andamento → cancelada ou interrompida.
Estado terminal não retorna a aberto e não é sobrescrito por resposta tardia.
Business parcial permanece resultado.status=parcial em chamada concluida.
Task submitted/working fica em_andamento; input-required/auth-required/rejected/
failed/canceled geram categoria própria sem inventar resultado válido.
Task completed sem artefato ou com JSON inválido gera falha de validação.

### Execução

em_andamento → concluida/parcial/falhou/cancelada/interrompida.

- concluida: >=1 chamada, todas com negócio concluido, nenhuma capacidade sem cobertura.
- parcial: >=1 resultado utilizável (negócio concluido/parcial), com limitação,
  falha, negócio parcial ou capacidade sem cobertura.
- falhou: nenhum resultado utilizável, inclusive nenhum agente elegível.
- cancelada: cancelamento explícito, preservando chamadas terminais anteriores.
- interrompida: reinício sem retomada de payload, preservando o que foi salvo.
- prazo total vencido: chamadas restantes timeout; estado global parcial/falhou
  conforme resultados utilizáveis, sem inventar um estado global novo.

## 7. Atomicidade e fechamento

Uma coordenação local por execução protege atualização de chamada e fechamento.
Cada write condiciona ownerRunId, version e estado esperado; resultado nunca usa
upsert. Antes de fechar, impedir novas gravações, aguardar as já iniciadas,
definir um encerradoEm comum, atualizar expiresAt das chamadas e depois da execução.
Só anunciar estado terminal salvo após acknowledgment de todas essas etapas.

Consulta lê estado/resumo da execução e chamadas persistidas, indicando andamento
enquanto fechamento não foi confirmado. Se summary divergir, reconciliar por
chamadas; não transformar write incerto em rollback presumido.
Em reinício, interromper abertos da instância anterior somente quando garantida
ausência de outro processo ativo. Multinstância não é suportada por este gate local.

## 8. Consulta, expiração e segurança

Owner é clientId autenticado. Consulta filtra proprietário e expiresAt > now;
outro proprietário, inexistente e expirado retornam 404.
Lista de chamadas é paginada por cursor opaco com ordenação estável, vinculada ao
owner/idExecucao. Não incluir análises integrais na lista.
Cada resultado original é recuperado separadamente por idSolicitacao,
evitando agregar muitos envelopes em uma resposta.

Retenção 30 dias após encerramento; limpeza periódica e TTL excluem registros
vencidos; o SLO de <=24 h exige operação monitorada e banco disponível.
Não estender retenção por consulta. A análise original não é substituída por síntese.
