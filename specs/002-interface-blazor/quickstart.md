# Quickstart: 002 — Interface Blazor

Execute na raiz da solução. Requer SDK .NET 10. Veja também o
[guia detalhado](../../SofiaInfis.Web/README.md).

## Prévia sem serviços externos

```powershell
dotnet restore SofiaInfis.slnx
dotnet run --project SofiaInfis.Web --launch-profile demo
```

Abra [SOFIA local](http://localhost:5275/). O perfil seleciona dados simulados apenas
em Development e mostra DEMO. API, bancos e OpenAI não são necessários nesse modo.

1. Inicie a conversa e informe um nome.
2. Envie uma pergunta ou anexe PDF/TXT; é possível enviar somente arquivos.
3. Com anexos, acompanhe as contribuições demonstrativas dos especialistas.
4. Expanda uma análise, solicite a síntese e abra o rastreamento.
5. Confira o menu móvel e o compositor em uma tela estreita.

Resultados de demonstração não representam avaliação do documento anexado.
As [capturas de referência](../../docs/INTERFACE_SOFIA.md) usam esse modo.

## Conexão real

Prepare a API conforme o [quickstart001](../001-integracao-a2a/quickstart.md) e o
[README da solução](../../README.md): RAG/PgVector e OpenAI para perguntas/síntese;
Mongo e catálogo A2A para os documentos. Os perfis abaixo são executados em terminais distintos.

```powershell
dotnet run --project SofiaInfis.Api --launch-profile http
```

```powershell
$env:Sofia__ApiBaseUrl = 'http://localhost:5151/'
$env:Sofia__ServiceToken = '<TOKEN-DE-SERVICO-CONFIGURADO-NA-API>'
dotnet run --project SofiaInfis.Web --launch-profile http
```

O placeholder deve ser substituído por credencial externa; não o versione.
User Secrets é uma alternativa para `Sofia:ApiBaseUrl`/`Sofia:ServiceToken`.
As permissões são `a2a:agents:read`, `a2a:execute`, `a2a:read`, `a2a:cancel` e as
capacidades configuradas em `Sofia:Capabilities` (default `analisar_documentos`).
Fora de loopback, use HTTPS. Não é necessário autorizar a origem do navegador em
CORS para o cliente Blazor no servidor. Nenhuma credencial é solicitada no chat.

## Testes e critérios observáveis

Prepare os bancos descartáveis das [fixtures](../../SofiaInfis.Api.Tests/Fixtures/README.md).

```powershell
dotnet build SofiaInfis.slnx --no-restore
dotnet test SofiaInfis.Api.Tests --no-build --filter 'FullyQualifiedName~SofiaWeb'
dotnet test SofiaInfis.slnx --no-build
```

Para9 testes do frontend, a integração utiliza Mongo/PgVector; os outros casos
utilizam transporte sintético/renderização. A suíte completa também exige Postman
CLI e porta 5190 livre. Não use o `--no-build` antes de um build atualizado.

Esperado: pergunta sem anexo não exige nome no contrato legado; documentos chegam
como texto com nome contextual; análises preservam IDs; síntese tem ID distinto;
falhas não viram sucesso; consumo ausente não é estimado; origem de demonstração
continua explícita. Confira [VALIDACAO.md](../../SofiaInfis.Web/VALIDACAO.md) para
resultados já registrados e verificações ainda não executadas.
