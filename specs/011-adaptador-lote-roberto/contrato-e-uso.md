# Adaptador SOFIA → contrato atual de Roberto

Somente SOFIA foi alterada. O transporte permanece A2A 0.3.0/JSON-RPC message/send,
com envelope SolicitacaoAnaliseSofia 1.0 e lote em documentos[].texto.

Fonte: [schema oficial](https://github.com/Roberto-Rabelo/infis-agente-pareceres-tributario/blob/fe4388c4d4e27696574dfa0b37479ae7c47b90dc/contracts/entrada/lote.v1.schema.json),
[documentação do orquestrador](https://github.com/Roberto-Rabelo/infis-agente-pareceres-tributario/blob/fe4388c4d4e27696574dfa0b37479ae7c47b90dc/docs/contrato-entrada-orquestrador.md),
LeitorLote, MapeadorObrigacao e Cnpj desse repositório, consultados em 2026-11-11.
Schema incorporado sem mudanças em SofiaInfis.Api/Services/A2A/Contracts/roberto-lote.v1.schema.json.
Exemplo público sintético incorporado em SofiaInfis.Api.Tests/Fixtures/Contracts/examples/roberto-lote.json.
Dados e aprovações do exemplo são apenas demonstrativos, não certificação de um documento real.

## Entradas aceitas

- JSON de lote.v1 completo ou uma obrigação normalizada por arquivo TXT. Mantém registros e relatórios, monta um lote de uma única raiz CNPJ e período abrangendo as obrigações. Valida schema, DV alfanumérico, datas, recibo/chave, layout, unicidade e aprovação explícita sem ocorrência ERRO.
- NF-e XML modelo 55, layout 4.00: extrai chave, data, grupos/campos XML e CNPJ no papel informado no relatório. Um documento por XML. Recusa DTD, duplicidades ambíguas e versões não suportadas.
- EFD ICMS/IPI 020: extrai 0000, 0150, C100, C190 e 1601 por posições revisadas; converte datas DDMMYYYY em ISO e decimais de vírgula para ponto. Recusa registros de dados não mapeados, sem descartá-los silenciosamente. Registros de abertura/fechamento/contagem não são dados de cruzamento.
- NFSE 1.01, EFD_CONTRIBUICOES 006, ECD 9.00, ECF 012 e DIMP 09/10 são aceitos já normalizados. Outros XML/SPED sem leitor revisado seguem para conclusão SOFIA com explicação.

Para XML/SPED bruto, anexar um relatório JSON em TXT com nome exato e único do
arquivo correspondente. Exemplo de estrutura (preencher com relatório real):

```json
{
  "documento": "nota.xml",
  "contribuinte": { "cnpj": "CNPJ_REAL_DO_CONTRIBUINTE_ANALISADO" },
  "validacaoOrigem": {
    "agente": "IDENTIDADE_DO_VALIDADOR",
    "versaoValidador": "VERSAO_REAL",
    "dataValidacao": "DATA_ISO_REAL",
    "status": "APROVADO",
    "ocorrencias": []
  }
}
```

Este trecho é um modelo de campos, não um lote válido nem aprovação automática.
Para EFD, adicionar identificacao com recibo e/ou hash reais; reciboAnterior se
substituta. Não calcular um hash novo para fingir recibo/validação da origem.
O relatório de origem é uma declaração fornecida, não autenticada criptograficamente
pela SOFIA. Preservar procedência e realizar revisão humana quando necessária.

## Dados insuficientes

O adaptador não preenche dados com LLM, não inventa CNPJ, período, recibo, chave ou
aprovação. Não omite arquivos fiscais incompatíveis para conseguir um lote parcial
aparentemente completo. Na ausência de dados, registra nao_executada e
FINAL_REVIEW_INSUFFICIENT_DATA com motivo legível. Nenhum request é enviado ao
Roberto. A resposta principal é da SOFIA, com limitações, inclusive se a síntese
remota falhar. Cancelamentos continuam impedindo nova chamada.

Lote e contexto respeitam 20.000 caracteres por documento, sem truncamento.
Falhas reais da chamada remota continuam distintas da ausência de dados.

## Alcance do parecer

Só NF-e × EFD ICMS/IPI possui regras de cruzamento executáveis no motor verificado.
Aceitação de outro tipo não significa que sua validação fiscal foi implementada.
O parecer preserva limitações de cobertura e dos especialistas. O pacote de análises
segue como contexto adicional, sem presumir que Roberto o processa. Um resultado
válido dele mantém autoria; falha remota gera conclusão SOFIA identificada.
