# Diagnóstico dos oito agentes A2A — 11/11/2026

Registro histórico da bateria anterior à entrega 011. A integração com Roberto foi
posteriormente adaptada ao schema oficial `lote.v1`; consulte a
[validação da entrega 011](../specs/011-adaptador-lote-roberto/validation-results.md).
Os resultados abaixo descrevem as amostras daquela bateria, sem incorporar os
retestes posteriores e sem certificar a correção fiscal dos agentes.

Testes reais pelo chat da SOFIA publicada: https://sofia-infis.onrender.com/. Foram usados apenas arquivos e identificação fictícios, sem documentos reais. Nenhum agente, segredo, regra de autorização ou código da aplicação foi alterado.

## Resultado geral

17 cenários; 15 chamadas efetivas de análise. Nas chamadas, oito resultados foram parciais, quatro foram erros de negócio/entrada e três foram rejeitados na validação do contrato. Nas outras duas tentativas não houve especialista selecionado: respondeu a SOFIA/RAG. A SOFIA apresentou resposta ao usuário nos 17 cenários.

**Nenhum agente retornou uma análise completa e validada nesta bateria.** A conclusão vale para as amostras executadas, reduzidas e com erros controlados; não equivale a homologação fiscal integral. O status da síntese da SOFIA não é o status do agente. HTTP 200 ou Task completed também não comprovam correção de conteúdo.

## Resultado por agente

| Agente / desenvolvedor | Resultado observado | Motivo e avaliação |
|---|---|---|
| NFS-e — Marcos | Parcial em quatro cenários | ABRASF com raízes CompNfse/Nfse não reconhecido pelo catálogo local. Nacional 1.00 rejeitado por versão; o agente pede 1.01. Nacional 1.01 reconhecido e submetido a XSD, com três apontamentos na amostra incompleta. Declara assinatura digital não verificada e regras tributárias completas pendentes. Explicita limitações, sem comprovar análise fiscal completa. |
| ECD — Carol | Parcial em três cenários | Detecta diferenças reais de saldo e conciliação, mas há falso positivo de data no I200: lê NUM_LCTO como DT_LCTO. Um achado sobre partidas normais também merece revisão da condição. Não considerar integralmente correta antes de corrigir o parser e repetir os controles. |
| ECF — Kaio Macedo | Parcial | Reconhece leiaute 12/ano 2025 e detecta contagem 999 para um arquivo de quatro linhas. Retorna 14 achados estruturais. Declara regras de conteúdo/análise ainda inexistentes, busca no manual desligada e etapas externas/LLM/validação de saída não implementadas. Funcionou a detecção estrutural conhecida; o escopo declarado segue incompleto. |
| Parecer Tributário — Roberto Rabelo | Erro de entrada em três cenários | Lotes candidatos rejeitados em $.versao, usando strings lote.v1/1.0 e número 1. Nenhum lote foi aceito. As fixtures também não têm obrigações previamente validadas para fundamentar parecer conclusivo. Precisa de schema e exemplo oficial aceito de lote.v1. A rejeição não prova erro de cálculo tributário. |
| Classificação Fiscal — Marcus Oliveira | Erro de contexto | Exige empresa, uf, regimeTributario, dataVigencia, papel, finalidade e tipoOperacao no contexto estruturado. O contrato enviado pela SOFIA não transporta esses campos nesse contexto; incluí-los no TXT não basta. O produto fictício também não tem composição/função para classificar com segurança. Integração e insumos bloqueiam a análise. |
| Auditoria NF-e — Andressa | INVALID_FINDING | O RPC respondeu; a SOFIA rejeitou ao menos um achado fora do contrato. Pela ordem do validador, campos principais e correlação passaram antes dessa etapa. O campo exato do achado rejeitado não é exposto na interface. Nenhuma análise do agente foi aceita para avaliar o conteúdo. |
| DIMP — Matheus do Valle | RESULT_CORRELATION_MISMATCH | Divergência em idExecucao, idSolicitacao, agente.idAgente ou agente.nome. Não foi possível isolar o campo nesta rodada, pois a saída inválida não é apresentada como resultado válido. Há histórico de nome divergente em 10/11, sem nova confirmação do campo exato. |
| Legislação/Jurisprudência — Lettícia Luize | INVALID_ANALYSIS_RESULT; duas outras tentativas sem seleção | Uma execução chamou o agente e rejeitou a saída por formato/conteúdo inválido. Nas outras duas, nenhum especialista foi selecionado: a resposta concluída veio da SOFIA/RAG. Não foram obtidas fontes oficiais válidas como resposta do agente. O teste não comprova funcionamento da coleta. |

## Contraprova de correção no ECD

O controle reconciliado usa período mensal, CNPJ sintético com dígitos verificadores e dois lançamentos. Conta 1: saldo 100 devedor + 50 de débito − 20 de crédito = 130 devedor. Conta 2: 100 credor + 50 de crédito − 20 de débito = 130 credor. Saldos assinados somam zero; débitos/créditos totalizam 70 de cada lado.

Um dos lançamentos enviados:

```text
|I200|1|15012025|50,00|N|
|I250|1||50,00|D|||Lancamento sintetico||
|I250|2||50,00|C|||Lancamento sintetico||
```

O agente disse que a data era “1”. O manual oficial da RFB, página 142, define campo 02 como NUM_LCTO e campo 03 como DT_LCTO. A data da linha acima é 15012025. Portanto, esse achado é um falso positivo de leitura de campo.

Fonte: [Manual ECD, RFB, janeiro de 2026](https://sped.rfb.gov.br/estatico/66/D1D73A379CB13BC5CDE7B7948B1F6C48967CCE/Manual_de_Orienta%C3%A7%C3%A3o_da_ECD_Leiaute_9_janeiro_2026.pdf).

Na contraprova, alterei somente o saldo final da conta 1 de 130 para 999, sem revelar a resposta esperada na pergunta. O agente manteve os dois achados anteriores e acrescentou dois coerentes com a alteração: saldo final líquido 869 e divergência na fórmula do saldo da conta. Isso comprova detecção nessa regra aritmética, mas não elimina o falso positivo de data.

O agente também trata menos de duas partidas de cada lado em lançamento normal como inconsistência. Recomenda-se revisar a condição de REGRA_LCTO_4_FORMULA contra a versão de manual adotada: a documentação oficial descreve a verificação e o aviso quando a condição não é cumprida. Essa regra não foi homologada com o PGE oficial nesta bateria.

Referência adicional: [Manual da RFB, regra I200](https://sped.rfb.gov.br/estatico/37/F02026250FA8FC1E5CA134A4A7E65CD60D6234/Manual_de_Orienta%C3%A7%C3%A3o_da_ECD_2017_Abril_30_04_2017.pdf).

## Correções indicadas

1. **Carol / ECD:** corrigir o índice da data de I200 e revisar a regra de partidas; repetir controles reconciliado e alterado.
2. **Andressa / NF-e:** inspecionar achados antes de responder. Exigências: idAchado/descricao não vazios; idDocumentos não vazio, sem duplicatas e restrito aos IDs recebidos; evidencias como array sem strings vazias; severidade baixa/media/alta/critica. O teste não identifica qual dessas condições falhou. Ver [validador SOFIA](../SofiaInfis.Api/Services/A2A/Domain/SofiaAnalysisValidator.cs).
3. **Matheus / DIMP:** comparar os dois IDs e a identidade do resultado com a solicitação/Agent Card. Preservar a validação estrita da SOFIA.
4. **Lettícia / Legislação:** inspecionar resultado e metadados atuais do card. Exigir contrato 1.0, IDs, agente, status permitido, analise não vazia e achados em array, além de observações no formato aceito. Conferir elegibilidade/capacidades nas tentativas sem chamada; o teste não permite atribuir a falha especificamente ao coletor.
5. **SOFIA + Marcus / Classificação:** pactuar extensão compatível para transportar o contexto e validar seu preenchimento. O [OpenAPI público](https://agente-classificacao-fiscal.vercel.app/openapi/v1.json), esquema PedidoAnalise, confirma os campos obrigatórios. Não preencher dados da empresa por suposição.
6. **Roberto / Parecer:** disponibilizar schema versionado e exemplo aceito de lote.v1 com obrigações validadas. Validar fixture antes da análise. Não adivinhar versão nem certificar lote vazio.
7. **Marcos / NFS-e:** documentar layouts suportados; nacional 1.01 foi reconhecido. Expandir catálogo quando necessário e completar/explicitar assinatura e regras tributárias pendentes.
8. **Kaio / ECF:** documentar escopo estrutural atual e implementar/ativar as etapas declaradas pendentes antes de prometer validação de conteúdo completa.

## Rastreabilidade

Os estados abaixo são dos agentes. “Sem agente” indica resposta SOFIA/RAG, não conclusão de especialista.

| Cenário | Status do agente | Execução SOFIA |
|---|---|---|
| nfse | Parcial | `51059094-ae55-4354-9ec9-10955f2bb11f` |
| ecd | Parcial | `43671c54-1a42-42a0-9939-63523e3b8756` |
| ecf | Parcial | `e34d4714-54da-43d1-a38f-2cb5ecafc595` |
| parecer | Com limitação | `e427e35a-deaf-452f-9de1-6f5411ff9dbf` |
| classificacao | Com limitação | `c1398adb-1cd0-4025-b6cf-9b17e2620f3b` |
| nfe | Indisponível | `0ad056d9-33be-4301-b9ed-99d118ce2e4b` |
| dimp | Indisponível | `06088dc1-6ac7-4b06-bc19-c81ead60f1f3` |
| legislacao | Indisponível | `991d274c-6575-4b00-b71f-bc5ba704fae0` |
| ecd-controle-coerente | Parcial | `4a0d4370-86b2-4488-8060-01bf6db103f7` |
| ecd-controle-divergente | Parcial | `7feca93e-bc79-4de0-abe9-09cc28aa73cc` |
| nfse-nacional | Parcial | `e01b35af-065b-41c8-9bcb-cad197efd7cb` |
| parecer-versao-1 | Com limitação | `b6bd67eb-3dc2-4445-86a1-31de367a95c7` |
| nfse-abrasf-raiz | Parcial | `0b01221a-b859-4595-b52b-716e494f682d` |
| legislacao-coleta | Sem agente | `dff8f72d-301f-41ce-be4c-effaeae57821` |
| nfse-nacional-101 | Parcial | `53b13dcb-737e-47bc-9e94-500bc441e9e3` |
| parecer-versao-numerica | Com limitação | `7d3e0475-ddc6-4fee-9d96-e52d0f7dbad5` |
| legislacao-reteste | Sem agente | `2662c3ff-f7e3-453e-82ba-e46474dee8ff` |

## Evidências locais

Arquivos ignorados pelo Git, contendo somente material sintético:

- `artifacts/agent-diagnostics/cases.json`, `followup-cases.json`, `final-cases.json`: perguntas, documentos e critérios.
- `artifacts/agent-diagnostics/all-results.json`: respostas capturadas, achados, observações, erros e respostas SOFIA.
- `artifacts/agent-diagnostics/run-cases.py`: rotina pelo chat; sem credenciais.
- `artifacts/agent-diagnostics/classificacao-openapi.json`: contrato público consultado.

Os payloads rejeitados pela validação não ficam disponíveis na interface como análises válidas. Para atribuir o campo exato das falhas de contrato, é necessário inspecionar a resposta/logs no projeto de cada agente. Não contornei autenticação nem reduzi validações.
