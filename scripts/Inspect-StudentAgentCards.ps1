param()
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$hostsToInspect = @(
    'agente-parecer-tributario.vercel.app', 'agente-ecf-agente.vercel.app',
    'agente-classificacao-fiscal.vercel.app', 'agente-auditoria-nfe.vercel.app',
    'agente-dimp-demo.vercel.app', 'agent-monitoramento-legislacao.vercel.app'
)
$protected = Read-Host 'Token A2A compartilhado (oculto)' -AsSecureString
$token = [Net.NetworkCredential]::new('', $protected).Password
$report = @()
try {
    foreach ($agentHost in $hostsToInspect) {
        $answers = @()
        $answers += [Net.Dns]::GetHostAddresses($agentHost).IPAddressToString
        foreach ($subnet in @('0.0.0.0/0', '74.220.49.0/24', '74.220.57.0/24')) {
            $dns = Invoke-RestMethod ('https://dns.google/resolve?name=' + $agentHost + '&type=A&edns_client_subnet=' + [uri]::EscapeDataString($subnet)) -TimeoutSec 20
            $answers += @($dns.Answer | Where-Object type -eq 1 | Select-Object -ExpandProperty data)
        }
        $statuses = @{}
        $card = $null
        foreach ($mode in @('none','invalid','valid')) {
            $headers = @{}
            if ($mode -ne 'none') { $headers.Authorization = 'Bearer ' + $(if($mode -eq 'valid') {$token} else {'synthetic-invalid-token'}) }
            $response = Invoke-WebRequest ('https://' + $agentHost + '/.well-known/agent-card.json') -Headers $headers -MaximumRedirection 0 -SkipHttpErrorCheck -TimeoutSec 25
            $statuses[$mode] = [int]$response.StatusCode
            if ($mode -eq 'valid' -and $response.StatusCode -eq 200) { $card = $response.Content | ConvertFrom-Json }
        }
        $entry = @{host=$agentHost;addresses=@($answers | Sort-Object -Unique);cardHttp=$statuses;card=$card}
        $report += $entry
        $entry | ConvertTo-Json -Depth 12 -Compress
    }
    New-Item -ItemType Directory -Force artifacts | Out-Null
    $report | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath artifacts/new-student-agent-cards.json
} finally { $token=$null; $protected=$null }
