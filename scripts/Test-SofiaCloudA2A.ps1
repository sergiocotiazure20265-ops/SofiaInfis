param([string]$BaseUrl = 'https://sofia-infis.onrender.com', [switch]$AllowPartial)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$target = [uri]$BaseUrl
if ($target.Scheme -ne 'https' -or $target.Host -ne 'sofia-infis.onrender.com' -or $target.UserInfo -or $target.Query -or $target.Fragment) {
    throw 'Destino HTTPS não autorizado para as credenciais da SOFIA.'
}
$access = Read-Host 'Deployment AccessKey (oculta)' -AsSecureString
$service = Read-Host 'Sofia ServiceToken (oculto, independente dos agentes)' -AsSecureString
$headers = @{
    'X-Sofia-Access-Key' = [Net.NetworkCredential]::new('', $access).Password
    Authorization = 'Bearer ' + [Net.NetworkCredential]::new('', $service).Password
}
function Request([string]$path, [string]$method = 'Get', $body = $null) {
    $argsForRequest = @{ Uri = $BaseUrl.TrimEnd('/') + $path; Method = $method; Headers = $headers; MaximumRedirection = 0; TimeoutSec = 45 }
    if ($null -ne $body) {
        $argsForRequest.ContentType = 'application/json'
        $argsForRequest.Body = [Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 10 -Compress))
    }
    Invoke-RestMethod @argsForRequest
}
try {
    $accessOnly = @{ 'X-Sofia-Access-Key' = $headers['X-Sofia-Access-Key'] }
    $missing = Invoke-WebRequest ($BaseUrl.TrimEnd('/') + '/api/v1/agentes') -Headers $accessOnly -MaximumRedirection 0 -TimeoutSec 30 -SkipHttpErrorCheck
    $accessOnly.Authorization = 'Bearer synthetic-invalid-token'
    $invalid = Invoke-WebRequest ($BaseUrl.TrimEnd('/') + '/api/v1/agentes') -Headers $accessOnly -MaximumRedirection 0 -TimeoutSec 30 -SkipHttpErrorCheck
    if ($missing.StatusCode -ne 401 -or $invalid.StatusCode -ne 401) { throw 'A API não rejeitou a credencial de entrada ausente/incorreta.' }
    $accessOnly.Clear()
    $catalog = Request '/api/v1/agentes'
    $catalogFile = Join-Path (Split-Path $PSScriptRoot -Parent) 'SofiaInfis.Api/a2a-student-agents.json'
    $expectedIds = (Get-Content -LiteralPath $catalogFile -Raw | ConvertFrom-Json).A2A.Agents.IdAgente
    if (@($expectedIds | Where-Object { $_ -notin $catalog.items.idAgente }).Count) { throw 'A publicação não contém todos os agentes cadastrados.' }
    if (@($catalog.items).Count -lt 2 -or @($catalog.items | Where-Object { $_.idAgente -in @('nfse-validador','agente-validacao-ecd') -and $_.elegivel }).Count -ne 2) {
        $catalog | ConvertTo-Json -Depth 5
        throw 'Os dois agentes esperados não estão elegíveis a partir do Render.'
    }
    $request = @{
        usuario = @{ nome = 'Teste sintetico SOFIA' }
        pergunta = 'Analise exclusivamente a estrutura destes documentos sinteticos de treinamento.'
        capacidades = @('analisar_documentos', 'monitorar_legislacao_tributaria')
        documentos = @(
            @{idDocumento='synthetic-ecd';nome='ecd-sintetico.txt';tipo='text/plain';textoCompleto=$true;texto="|0000|LECD|01012025|31122025|EMPRESA FICTICIA TESTE|00000000000000|SP|000000000|3550308||0|0|0|0|0|`n|9999|2|"},
            @{idDocumento='synthetic-nfse';nome='nfse-sintetica.xml';tipo='application/xml';textoCompleto=$true;texto='<CompNfse><Nfse versao="2.04"><InfNfse Id="synthetic-001"><Numero>1</Numero><Servico><Valores><ValorServicos>100.00</ValorServicos></Valores><Discriminacao>DOCUMENTO SINTETICO SEM VALIDADE FISCAL</Discriminacao></Servico></InfNfse></Nfse></CompNfse>'}
        )
    }
    $accepted = Request '/api/v1/orquestracoes' 'Post' $request
    $id = $accepted.idExecucao
    if (-not [guid]::TryParse($id, [ref]([guid]::Empty))) { throw 'ID de execução inválido.' }
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(135)
    do {
        Start-Sleep -Milliseconds 750
        $execution = Request ('/api/v1/orquestracoes/' + $id + '?pageSize=100')
    } while ($execution.estado -eq 'em_andamento' -and [DateTimeOffset]::UtcNow -lt $deadline)
    $results = @()
    $failures = @($catalog.items | Where-Object { -not $_.elegivel } | ForEach-Object { @{idAgente=$_.idAgente;estado='inelegivel';codigo=$_.motivo} })
    foreach ($call in $execution.chamadas.items) {
        if (-not $call.resultadoDisponivel) {
            $failures += @{idAgente=$call.idAgente;estado=$call.estado;codigo=$call.falhaTecnica.codigo}
            continue
        }
        $result = Request ('/api/v1/orquestracoes/' + $id + '/chamadas/' + $call.idSolicitacao + '/resultado')
        if ($result.versaoContrato -ne '1.0' -or $result.idExecucao -ne $id -or $result.idSolicitacao -ne $call.idSolicitacao -or $result.agente.idAgente -ne $call.idAgente -or -not $result.analise) { throw 'Resultado sem contrato/correlação válida.' }
        $results += @{ idAgente=$call.idAgente;idExecucao=$result.idExecucao;idSolicitacao=$result.idSolicitacao;status=$result.status;achados=@($result.achados).Count }
    }
    if ($results.Count -lt 2) { throw 'Faltam resultados dos agentes.' }
    # Cancel a separate synthetic execution; completed tasks may finish before cancellation.
    $cancelStart = Request '/api/v1/orquestracoes' 'Post' $request
    $cancel = Request ('/api/v1/orquestracoes/' + $cancelStart.idExecucao + '/cancelar') 'Post'
    if ($cancel.idExecucao -ne $cancelStart.idExecucao) { throw 'Cancelamento retornou correlação inválida.' }
    $report = @{timestamp=[DateTimeOffset]::UtcNow;catalog=$catalog.items;idExecucao=$id;estado=$execution.estado;results=$results;failures=$failures;cancelamento=$cancel}
    New-Item -ItemType Directory -Force artifacts | Out-Null
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath artifacts/student-agents-cloud.json
    $report | ConvertTo-Json -Depth 8
    if ($failures.Count -and -not $AllowPartial) { throw 'Há agentes sem resultado válido; consulte o relatório. Use -AllowPartial somente para diagnóstico de sucesso parcial.' }
} finally { $headers.Clear(); $access=$null; $service=$null }
