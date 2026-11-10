# Evidências da implementação — Integração A2A

## Setup e fundação (2026-11-05)

- Build original: aprovado antes da integração. Avisos preexistentes NU1902/NU1903
  em System.Security.Cryptography.Pkcs 9.0.18; nenhuma atualização produtiva feita.
- Restore: cache local do usuário, pacotes fixados conforme plano. A execução
  elevada precisa usar --packages C:/Users/sergi/.nuget/packages para não apontar
  para o perfil isolado do sandbox. Os arquivos de dependências produtivas não mudam.
- Smoke de fixtures: 2 testes aprovados, HTTP local e OpenAI sintética sem rede externa.
- Vermelho da fundação: 7 falhas de comportamento e2 aprovações demonstradas;
  credencial válida não autenticava, destinos indevidos eram aceitos e gravação era ausente.
- Verde da fundação: 9 testes aprovados após implementação.
- Checkpoint ampliado: 13 testes aprovados,0 falhas, incluindo startup/validação
  dos endpoints antigos com módulo desligado e Mongo novo indisponível.
- Mongo de teste: container sofia-a2a-test-mongo, loopback27029, sem volumes
  existentes; bancos efêmeros sofia_a2a_tests_* são removidos pela fixture.
- CLI Postman oficial:1.86.0 instalada e comando simulation disponível.
  Sessão sem login/workspace: busca de mocks de proprietários não executada.

O checkpoint acima registra apenas a fundação. As fases posteriores e seus
resultados estão documentados abaixo.
As fixtures possuem dados sintéticos. Nenhum token/documento real foi enviado.

## US1 — descoberta
Cinco testes aprovados: identidade estável/duplicada, card válido/incompatível, endpoint protegido e sem segredos, execução sem agente sem análise fictícia. HTTP real local e Mongo exclusivo de testes; sem tokens OpenAI.

## US2 — análise individual
Três cenários POST inicialmente falharam por404; após implementação,13 testes de contratos e análise individual passaram. Envio único, textos integrais, Task working/polling/completed, ausência de achados e erro de negócio registrados. Sem chamada OpenAI externa.

## US3 — agregação e cancelamento
Nove cenários vermelhos demonstraram ausência de agregação/cancelamento. Após generalização,12 testes selecionados passaram (incluindo análise individual): dois sucessos + falha preservados, ID errado rejeitado, cancelamento salvo e estado terminal preservado. Concorrência de chamadas limitada e observabilidade apenas do módulo.

## US4 — consulta e recuperação
Três falhas comportamentais iniciais demonstraram GET ausente e execução abandonada sem reconciliação. Após implementação,4 testes selecionados passaram: consulta com link ao resultado original, reinício sem reanálise, interrupção recuperada e expiração/limpeza com índice TTL. Três cenários adicionais de falha de escrita passaram: falha antes do início não delega; falha antes de salvar resultado não o anuncia disponível; perda de confirmação após salvar permite recuperar o resultado confirmado por leitura, sem reanálise.

## US5 — segurança e privacidade

Testes HTTP verificam 401 sem token e com token inválido, 403 sem permissões nas
cinco operações, 404 para outro proprietário, capacidade não autorizada sem envio
e rotação do hash preservando ClientId/acesso ao histórico. Tokens distintos de
entrada e saída são usados pelas fixtures. Opções são aplicadas no startup; a
mutação em teste simula aplicação de uma configuração rotacionada.

Logs e tags de spans foram inspecionados com sentinelas de pessoa, documento e
credenciais. Conteúdo adversarial permanece texto, sem ampliar delegação; consumo
ausente permanece null. As métricas usam somente agente/estado/categoria técnica,
sem texto, pergunta, pessoa ou credencial. CORS permite apenas origens explícitas,
sem cookies; backend sem Origin funciona e CORS não substitui autenticação.
Políticas de saída rejeitam HTTP fora do ambiente isolado, URL não aprovada,
query/credenciais na URI. Um card que exige Bearer e outro esquema simultâneo é
inelegível. TLS usa validação padrão da plataforma; não se desabilita certificado.

## US6 — regressão

PostgreSQL/PgVector real de teste: container `sofia-a2a-test-postgres`, loopback
54329, sem volume de dados existente. Schemas exclusivos `sofia_test_*`.
OpenAI substituída no transporte SDK por respostas sintéticas: nenhum consumo real.

Os três endpoints antigos funcionam sem novo nome/token, com A2A off, Mongo novo
indisponível e opções inválidas. Swagger mantém as três operações; resposta de
perguntar mantém os nove campos, ID UUID, tokens e duração. Fluxos sem anexo e com
TXT, ingestão, chunks 1200/overlap 200, vetores1536, busca e rollback transacional
foram exercitados. Extração PDF textual/TXT, cinco arquivos,5 MiB por arquivo e
truncamento sinalizado20.000 preservados. Kestrel real rejeita multipart acima de
20 MiB com **400**, comportamento MVC existente, sem impor novo status ao legado.
Uma corrida na criação da extensão do banco de teste foi corrigida na fixture.

## Escala, compatibilidade e Postman

SC-009: cinco execuções simultâneas, três agentes, dois textos de1.000 caracteres,
15 contribuições correlacionadas e persistidas. O teste limita o tempo a120 s e
registra latência total/mínima/máxima no TRX. Saturação retorna429; deadline inclui
fila e fechamento fica limitado ao deadline+5 s. O teste de prazo usa tempo real,
medido desde a aceitação, excluindo startup da fixture.

Matriz: schemas oficiais0.3.0, exemplos de domínio1.0, obrigatoriedade dos campos,
status concluido/parcial/erro, extensões opcionais no round-trip, versão incorreta,
IDs/agente/documento incorretos, duplicatas JSON, terminal imutável, polling e limite
de bytes escapados/stream. Cursor protegido não pode ser reutilizado em outra execução.

`PostmanSimulationTests` passou com CLI1.86.0 e simulação local, sem publicação,
histórico cloud, login ou payload real. O cliente real da SOFIA descobre o card,
envia message/send, valida o artefato e salva o resultado no Mongo. O handler usa
req.body no runtime Postman. A evidência e as hipóteses estão no `.context.yaml`.

## Cobertura e limites de aceitação

| Requisitos | Evidência principal |
|---|---|
| FR-001–003 / SC-001 | Registry, discovery, seleção, card incompatível, execução sem elegíveis |
| FR-004 / SC-008 | SecurityBoundary, políticas e ownership HTTP |
| FR-005–010 / SC-002,005 | DomainContract, CompatibilityMatrix, análise individual e reducer |
| FR-011–013 / SC-003 | WireContract, polling, correlação e CAS terminal |
| FR-014–017 / SC-004,005 | Três agentes, parcialidade, cancelamento, fila/deadline |
| FR-018–020 / SC-006 | MongoWrite, PersistenceFailure, Query, Restart, Race |
| FR-021–024 / SC-008 | Observabilidade, privacidade, retenção, política de destino e CORS |
| FR-025 / SC-007 | LegacyApiContract, DocumentExtraction, RagFlow, ModuleIsolation |
| FR-026–027 / SC-009 | Schemas/round-trip, concorrência, suíte e infraestrutura sintética |

Revisão dos12 princípios: arquitetura/net10 preservados; serviços A2A separados e
injetados; domínio/wire fixados; dados textuais e correlação obrigatórios;
autenticação/erros/prazos e rastreabilidade implementados; regressão do RAG/documentos;
testes incluídos e skills usadas. Nenhum serviço/controller/DTO legado, pacote
produtivo, appsettings local ou Compose foi alterado. Program recebeu apenas wiring
adicional. Não havia memória conversacional funcional para preservar ou anunciar.

Limitações: operação em instância única, sem lease distribuído; fila/documentos
transitórios são perdidos no reinício e não são reenviados. TTL depende do relógio e
monitor Mongo; limpeza explícita e leitura expirada foram testadas, sem aguardar30
dias reais. Consumo externo não tem extensão pactuada nesta versão e fica null.
Não há teste de agente real dos alunos, certificado/deploy externo ou correção
factual da análise fiscal. Validação estrutural não comprova veracidade de evidências.
Os avisos NU1902/NU1903 preexistentes foram registrados sem alterar dependências.
Nenhum segredo real foi acrescentado; tokens fixos existentes nos doubles são
exclusivamente sintéticos. Nenhum commit/push foi executado por este workflow.

## Verificação final — 2026-11-05

- Restore da solução: aprovado, versões fixadas, cache local e `NuGetAudit=false`.
  Não representa nova auditoria de vulnerabilidades; os alertas já conhecidos
  continuam registrados acima.
- `dotnet build SofiaInfis.slnx --no-restore -v minimal`: aprovado,0 erros e0 avisos
  nessa execução,10,11 s.
- `dotnet test SofiaInfis.slnx --no-build --no-restore --logger trx --results-directory TestResults -v minimal`:
  **70 aprovados,0 falhas,0 ignorados**, duração reportada13 s.
- TRX local: `TestResults/sergi_SERGIO_2026-11-05_14_24_55_net10.0.trx`
  (ignorado pelo Git; contém evidências dos testes sintéticos).
- SC-009:5 execuções,3 agentes,15 chamadas,2 documentos de1.000 caracteres;
  **397 ms** desde submissões até conclusão observada; duração individual
  **130–166 ms** nesta máquina, com atraso sintético100 ms por agente.
  Esses números não são benchmark de agentes reais nem de produção.
- `git diff --check`: aprovado. Arquivos produtivos rastreados alterados: apenas
  Program.cs com composição adicional; solução inclui o novo projeto de testes.
  Demais implementações são arquivos novos. Exemplo A2A está liberado no ignore.
- Busca de padrões de chaves reais/privadas nos arquivos novos: nenhuma ocorrência.
- `.specify/extensions.yml` ausente: nenhum hook pós-implementação a executar.
- Infraestrutura descartável criada nesta execução encerrada após a validação;
  as instruções das fixtures permitem reiniciá-la para reproduzir os testes.

T001–T080 concluídas. Checklist de requisitos foi tratado como gate somente de
leitura e não foi alterado pela implementação.
