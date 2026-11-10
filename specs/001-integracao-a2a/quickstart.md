# Guia de validação — 001 Integração A2A

Este guia corresponde à implementação da feature 001. As evidências executadas e
limitações estão em [validation-results.md](validation-results.md).
Contratos: [README](contracts/README.md).
Dados/estados: [data-model.md](data-model.md).

Para executar o consumidor visual, siga o
[quickstart 002](../002-interface-blazor/quickstart.md). A API 001 permanece com os
mesmos contratos; a UI usa seu servidor Blazor como cliente autenticado. A opção
de síntese da UI é uma chamada separada à API de perguntas, não uma etapa automática
do worker A2A.

## 1. Pré-requisitos

SDK .NET 10; Docker/Compose; MongoDB 8.0 de teste separado, sem dados reais;
PostgreSQL/PgVector isolado e Postman CLI; três agentes HTTP determinísticos provisionados pela suíte (fiscal,
contábil, indisponível/lento). Dois agentes bastam para o mínimo de interoperabilidade;
três demonstram sucesso parcial. Não depende de OpenAI ou dos agentes reais dos alunos.

Usar instância única da API, sem sobreposição durante testes de reinício.
Não reutilizar segredo nem banco de produção.
As referências de extração aceitam PDF/TXT no fluxo legado; o novo início recebe
documentos previamente convertidos em texto, sem criar novos parsers.

## 2. Preparação

1. Configurar A2A:Enabled=true; catálogo dos agentes de teste com cardUri,
   expectedName, destinos permitidos e referências às credenciais.
2. Configurar A2A:InboundClients com clientId estável, hash de token de teste forte,
   permissões a2a:execute/read/cancel/agents:read e capacidades autorizadas.
3. Fornecer segredos somente por ambiente; credentialRef de saída difere da chave
   de entrada. Configurar Mongo de teste via configuração isolada.
4. Confirmar timeouts 5/60/120 s, fechamento <=5 s, concorrência 3 por execução,
   retenção 30 dias, limite de envelope 1 MiB.
5. Se a API roda no host, usar portas publicadas dos agentes; se em Docker,
   usar DNS de serviço e porta interna. Não usar localhost para outro container.
6. Fora da rede isolada, usar HTTPS com certificado validado.
   CORS não é requisito para esta comunicação servidor-servidor.

As opções estão em `SofiaInfis.Api/appsettings.a2a.example.json`. Esse exemplo não
é carregado automaticamente. Use configuração local/ambiente, sem mudar o exemplo
para conter segredos. Os testes injetam opções e credenciais efêmeras.

Siga [Fixtures/README.md](../../SofiaInfis.Api.Tests/Fixtures/README.md) para iniciar
Mongo/PgVector descartáveis. Comandos a partir da raiz da solução:

```powershell
dotnet restore SofiaInfis.slnx
dotnet build SofiaInfis.slnx --no-restore
dotnet test SofiaInfis.Api.Tests/SofiaInfis.Api.Tests.csproj --no-build
```

O projeto de testes já pertence à solução. A suíte inicia os agentes locais e uma
simulação Postman na porta 5190; Mongo/PgVector devem estar em execução nas portas
27029/54329. Não execute outra simulação na porta 5190 durante a suíte.

## 3. Fluxo manual

Com API e agentes de teste em execução:

```powershell
$baseUri = 'http://localhost:5151'
$authHeaders = @{ Authorization = ('Bearer ' + $env:SOFIA_TEST_TOKEN) }
$agents = Invoke-RestMethod -Uri ($baseUri + '/api/v1/agentes') -Headers $authHeaders
$body = Get-Content 'specs/001-integracao-a2a/contracts/examples/criar-orquestracao.json' -Raw
$accepted = Invoke-RestMethod -Method Post -Uri ($baseUri + '/api/v1/orquestracoes') -Headers $authHeaders -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($body))
$execution = Invoke-RestMethod -Uri ($baseUri + '/api/v1/orquestracoes/' + $accepted.idExecucao) -Headers $authHeaders
$execution
```

Esperado: POST 202 com ID/Location; GET mostra em_andamento ou estado terminal,
resumos por agente e links dos resultados. Consultar novamente até terminal,
sem reenviar POST. Para cada resultado disponível, usar linkResultado.
A nova API é local de orquestração, não o endpoint A2A dos alunos.

## 4. Cenários de validação

| Cenário | Ação / resultado esperado | Rastreabilidade |
|---|---|---|
| Descoberta | Dois cards válidos e um inválido; inválido não recebe documento | US1, SC-001 |
| Entrada integral | Dois textos e IDs chegam iguais ao especialista | US2, SC-002 |
| Envelope | Validar domínio e Task/Message/Artifact contra schemas | FR-005–013 |
| Sem achados | concluido com achados=[] é sucesso | US2.3 |
| Tarefa longa | working → tasks/get → completed; uma só análise enviada | US2.4 |
| Correlação | Outra execução/agente/documento, duplicata e resposta tardia rejeitados | US3, SC-003 |
| Parcial | Dois agentes concluem, terceiro falha; preservar as duas análises <=125 s | US3, SC-004 |
| Cancelamento | POST cancelar; estado salvo e resultados prévios preservados | US3.4 |
| Persistência | Reiniciar após conclusão; mesmos resultados sem nova análise | US4, SC-006 |
| Interrupção | Reiniciar em andamento sem processo anterior; interrompida, sem retry | US4.6 |
| Falha ao gravar | Antes do início: nenhuma análise; depois: não declarar salvo | US4.5 |
| Acesso | Sem token401, sem permissão403, outro proprietário404 | US4.4/US5 |
| Privacidade | Logs sem nome, texto, análise integral e token; consumo ausente null | US5, SC-008 |
| Retenção | Relógio de teste expira registro; leitura404 e limpeza em até24 h | FR-022 |
| Concorrência | Cinco execuções, três agentes, dois textos de1.000 caracteres | SC-009 |
| Regressão | Três operações atuais e PDF/TXT sem nome obrigatório, com A2A off/indisponível | US6, SC-007 |

Para cenários rápidos, TimeProvider permite avançar o tempo em unitários.
Pelo menos uma integração mede prazos reais. TTL real precisa verificação própria:
não confundir relógio da aplicação com relógio do servidor Mongo.

## 5. Rede, segurança e CORS condicional

Testar GET do card e POST JSON-RPC no url declarado a partir da rede usada pela API,
com credencial válida e sem Origin. Conferir métodos message/send, tasks/get e
tasks/cancel nos registros sanitizados das fixtures.

Se houver navegador: OPTIONS de origem permitida recebe permissão correspondente;
origem desconhecida não recebe Access-Control-Allow-Origin. Testar Vary: Origin
quando aplicável. Não usar o endpoint backend da SOFIA como origem do frontend
por engano. Ausência de CORS não pode impedir backend autenticado.

## 6. Evidências e critérios de encerramento

Guardar relatórios de teste, payloads sintéticos, estados HTTP/negócio, tempos e
IDs de correlação; nenhuma credencial/documento real. Build e testes devem passar;
listar qualquer verificação não executada. Resultado de planejamento não prova
integração em execução.

A2A desabilitada deve permitir startup e fluxos antigos sem Mongo/cards/credenciais
do novo módulo. Nenhuma alteração de contrato antigo deve ser necessária para
concluir este roteiro.
