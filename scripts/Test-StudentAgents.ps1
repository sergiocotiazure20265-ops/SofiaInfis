param()
$ErrorActionPreference = 'Stop'
$protected = Read-Host 'Token compartilhado dos agentes (nao sera exibido)' -AsSecureString
$previousToken = $env:SOFIA_A2A_TOKEN
$previousLive = $env:SOFIA_TEST_LIVE_STUDENTS
$previousReport = $env:SOFIA_A2A_REPORT
try {
    $env:SOFIA_A2A_TOKEN = [System.Net.NetworkCredential]::new('', $protected).Password
    $env:SOFIA_TEST_LIVE_STUDENTS = '1'
    New-Item -ItemType Directory -Force artifacts | Out-Null
    $env:SOFIA_A2A_REPORT = Join-Path (Get-Location) 'artifacts/student-agents-live.json'
    if (Test-Path $env:SOFIA_A2A_REPORT) { Remove-Item -LiteralPath $env:SOFIA_A2A_REPORT }
    dotnet test SofiaInfis.Api.Tests/SofiaInfis.Api.Tests.csproj --no-build --nologo --tl:off --filter FullyQualifiedName~StudentAgentsLiveTests --logger 'console;verbosity=minimal' *> artifacts/student-agents-test.log
    $testExit = $LASTEXITCODE
    Get-Content artifacts/student-agents-test.log -Tail 5
    if (Test-Path $env:SOFIA_A2A_REPORT) { Get-Content $env:SOFIA_A2A_REPORT }
    exit $testExit
} finally {
    $env:SOFIA_A2A_TOKEN = $previousToken
    $env:SOFIA_TEST_LIVE_STUDENTS = $previousLive
    $env:SOFIA_A2A_REPORT = $previousReport
}
