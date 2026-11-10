# Testes locais e isolados

Requer SDK .NET 10, Docker e Postman CLI (validado com 1.86.0), no Windows.
Execute na raiz do repositório. Portas 27029, 54329 e 5190 devem estar livres.
Não aponte estas fixtures a bases de produção.

```powershell
docker run --name sofia-a2a-test-mongo --label sofia.role=test-only -p 127.0.0.1:27029:27017 -d mongo:8.0
docker run --name sofia-a2a-test-postgres --label sofia.role=test-only -e POSTGRES_PASSWORD=sofia-local-test-only -p 127.0.0.1:54329:5432 -d pgvector/pgvector:pg16
dotnet restore SofiaInfis.slnx
dotnet build SofiaInfis.slnx --no-restore
dotnet test SofiaInfis.slnx --no-restore
```

A senha acima é exclusiva do container descartável. MongoFixture usa bancos com
prefixo `sofia_a2a_tests_`, e PostgresFixture usa schemas `sofia_test_`, removidos
ao terminar. Os agentes Kestrel usam portas efêmeras. Nenhum token OpenAI real é
usado: os clientes SDK recebem respostas sintéticas por HttpMessageHandler.

PostmanSimulationTests inicia sua própria simulação na porta 5190, verifica a SOFIA
via HTTP e Mongo e encerra apenas a árvore de processo que criou. Não deixe outra
simulação nessa porta durante a suíte. Nenhuma publicação ou login é necessário.

Se os containers já existirem, use `docker start` com os dois nomes acima.
Após os testes, `docker stop sofia-a2a-test-mongo sofia-a2a-test-postgres` encerra
somente a infraestrutura descartável criada para esta suíte.

Os testes demonstram o comportamento do cliente e dos contratos sintéticos.
Disponibilidade, certificados, permissões e interoperabilidade de agentes reais
dos alunos precisam de validação no ambiente da turma. O relógio controlado testa
expiração na aplicação; não acelera o monitor TTL do servidor Mongo.

## Testes do frontend Blazor

O projeto de testes também referencia `SofiaInfis.Web`. A entrega 002 acrescentou
9 casos:6 de gateway HTTP, 2 de host/renderização e1 de integração atravessando o
cliente Blazor, a API de teste, dois agentes Kestrel, Mongo/PgVector e OpenAI sintética.

```powershell
dotnet test SofiaInfis.Api.Tests --filter 'FullyQualifiedName~SofiaWeb'
```

Esse grupo inclui a integração e requer os dois bancos de teste iniciados. Para
gateway/renderização apenas, filtre `SofiaWebGatewayTests|SofiaWebRenderingTests`.
A porta 5275 só é necessária para a prévia manual, não para as WebApplicationFactory.
A suíte completa continua exigindo Postman CLI e porta 5190 livre.

Especificação: [002](../../specs/002-interface-blazor/spec.md).
Execução e limites: [guia Web](../../SofiaInfis.Web/README.md).
Resultados registrados: [validação](../../SofiaInfis.Web/VALIDACAO.md).

A entrega 003 adiciona XML seguro, ingestão documental, chat direto sem agentes e
bootstrap/reinício RAG. `xunit.runner.json` limita a execução entre classes a uma
thread para evitar exaustão do Mongo local; testes de concorrência continuam
criando suas próprias chamadas simultâneas. O teste de autorização espera a primeira
chamada autorizada terminar antes de medir que uma chamada proibida não invoca agente.
