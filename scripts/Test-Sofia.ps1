param([string]$ApiUrl = 'http://localhost:5151')
$ErrorActionPreference = 'Stop'
$fixtureDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/smoke'
New-Item -ItemType Directory -Path $fixtureDir -Force | Out-Null
$textFile = Join-Path $fixtureDir 'controle.txt'
$xmlFile = Join-Path $fixtureDir 'nota.xml'
Set-Content -LiteralPath $textFile -Encoding utf8 -Value 'Documento fiscal sintético para teste de integração. O código de controle é SOFIA-VALIDACAO-4827. Não representa operação real.'
Set-Content -LiteralPath $xmlFile -Encoding utf8 -Value '<NFe xmlns="urn:sofia:teste"><infNFe Id="TESTE4827"><total><ICMSTot><vNF>150.50</vNF></ICMSTot></total><observacao>Documento sintético sem validade fiscal.</observacao></infNFe></NFe>'
$health = Invoke-WebRequest "$ApiUrl/health" -TimeoutSec 10
if ($health.StatusCode -ne 200) { throw 'A base RAG não está disponível.' }
$search = Invoke-RestMethod "$ApiUrl/api/v1/conhecimentos/buscar" -Method Post -ContentType 'application/json' -Body (@{pergunta='serviços fiscais da INFIS';topK=3} | ConvertTo-Json) -TimeoutSec 90
$response = Invoke-RestMethod "$ApiUrl/api/v1/sofia/perguntar" -Method Post -Form @{
    pergunta = 'Nos documentos sintéticos anexados, qual é o valor de vNF no XML e qual é o código de controle no TXT? Apenas extraia esses dois dados.'
    arquivos = @((Get-Item -LiteralPath $textFile), (Get-Item -LiteralPath $xmlFile))
} -TimeoutSec 120
if ($response.arquivosProcessados.Count -ne 2 -or $response.resposta -notmatch '4827' -or $response.resposta -notmatch '150') {
    throw 'A resposta real não confirmou os dois documentos de controle.'
}
[pscustomobject]@{
    ApiHealth = $health.StatusCode
    SearchMatches = @($search).Count
    DocumentsProcessed = $response.arquivosProcessados.Count
    RagReferences = $response.conhecimentosUtilizados.Count
    ExecutionId = $response.idExecucao
    ChatTokens = $response.tokensTotal
    DocumentValuesVerified = $true
} | ConvertTo-Json
