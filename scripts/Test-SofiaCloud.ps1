param(
    [string]$BaseUrl = 'https://sofia-infis.onrender.com',
    [switch]$TestOpenAI,
    [switch]$TestVoiceAssets
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$target = [uri]$BaseUrl
if ($target.Scheme -ne 'https' -or $target.Host -ne 'sofia-infis.onrender.com') {
    throw 'Este teste só envia credenciais ao serviço HTTPS SOFIA autorizado.'
}
Write-Host 'Ready for cloud access key (hidden).'
$secureKey = Read-Host 'Access key' -AsSecureString
$credential = [pscredential]::new('sofia', $secureKey)
$health = Invoke-WebRequest "$BaseUrl/health" -TimeoutSec 90
$anonymous = Invoke-WebRequest "$BaseUrl/" -SkipHttpErrorCheck -TimeoutSec 30
$landing = Invoke-WebRequest "$BaseUrl/" -Authentication Basic -Credential $credential -TimeoutSec 30
$knowledge = Invoke-WebRequest "$BaseUrl/conhecimentos" -Authentication Basic -Credential $credential -TimeoutSec 30
$swagger = Invoke-RestMethod "$BaseUrl/swagger/v1/swagger.json" -Authentication Basic -Credential $credential -TimeoutSec 30
if ($health.StatusCode -ne 200 -or $anonymous.StatusCode -ne 401 -or $landing.StatusCode -ne 200 -or $knowledge.StatusCode -ne 200 -or -not $swagger.paths.'/api/v1/conhecimentos/ingest/arquivo') {
    throw 'A verificação dos endpoints públicos falhou.'
}
$result = [ordered]@{
    health = $health.StatusCode
    anonymous = $anonymous.StatusCode
    frontend = $landing.StatusCode
    knowledge = $knowledge.StatusCode
    swagger = $true
    documentUploadDocumented = $true
}
if ($TestVoiceAssets) {
    $voiceModule = Invoke-WebRequest "$BaseUrl/voice-input.mjs" -Authentication Basic -Credential $credential -TimeoutSec 30
    $composerModule = Invoke-WebRequest "$BaseUrl/chat.js" -Authentication Basic -Credential $credential -TimeoutSec 30
    if ($voiceModule.StatusCode -ne 200 -or $voiceModule.Headers.'Content-Type' -notmatch 'javascript' -or $composerModule.Content -notmatch 'voice-input\.mjs') {
        throw 'O módulo de voz publicado não está disponível ou integrado ao compositor.'
    }
    $result.voiceModulePublished = $true
    $result.voiceComposerBinding = $true
}
if ($TestOpenAI) {
    $search = Invoke-RestMethod "$BaseUrl/api/v1/conhecimentos/buscar" -Method Post -Authentication Basic -Credential $credential -ContentType 'application/json' -Body (@{pergunta='Serviços fiscais da INFIS';topK=3} | ConvertTo-Json) -TimeoutSec 120
    $fixtureDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/cloud-smoke'
    New-Item -ItemType Directory -Path $fixtureDir -Force | Out-Null
    $txtPath = Join-Path $fixtureDir 'controle.txt'
    $xmlPath = Join-Path $fixtureDir 'nota.xml'
    Set-Content -LiteralPath $txtPath -Encoding utf8 -Value 'Documento fiscal sintético. Código de controle: SOFIA-VALIDACAO-4827. Não representa operação real.'
    Set-Content -LiteralPath $xmlPath -Encoding utf8 -Value '<NFe xmlns="urn:sofia:teste"><infNFe Id="TESTE4827"><total><vNF>150.50</vNF></total><observacao>Documento sintético sem validade fiscal.</observacao></infNFe></NFe>'
    $answer = Invoke-RestMethod "$BaseUrl/api/v1/sofia/perguntar" -Method Post -Authentication Basic -Credential $credential -Form @{
        pergunta = 'Nos documentos sintéticos anexados, qual o valor de vNF no XML e qual o código de controle no TXT? Apenas extraia os dados.'
        arquivos = @((Get-Item -LiteralPath $txtPath), (Get-Item -LiteralPath $xmlPath))
    } -TimeoutSec 150
    if ($answer.arquivosProcessados.Count -ne 2 -or $answer.resposta -notmatch '4827' -or $answer.resposta -notmatch '150' -or @($search).Count -ne 3) {
        throw 'A resposta pública não confirmou os documentos e a busca RAG.'
    }
    $result.searchMatches = @($search).Count
    $result.processedFiles = $answer.arquivosProcessados.Count
    $result.ragReferences = $answer.conhecimentosUtilizados.Count
    $result.documentValuesVerified = $true
    $result.chatTokens = $answer.tokensTotal
    $result.executionId = $answer.idExecucao
}
$result | ConvertTo-Json
