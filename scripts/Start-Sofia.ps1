param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    if (-not (Test-Path 'docker-compose.yml')) {
        Copy-Item 'docker-compose.example.yml' 'docker-compose.yml'
    }
    # Load an existing local credential without printing or copying it into an image.
    $hasDotEnvKey = (Test-Path '.env') -and ((Get-Content '.env' -Raw) -match '(?m)^SOFIA_OPENAI_API_KEY=.+$')
    if (-not $env:SOFIA_OPENAI_API_KEY -and -not $hasDotEnvKey -and (Test-Path 'SofiaInfis.Api/appsettings.json')) {
        $localConfig = Get-Content 'SofiaInfis.Api/appsettings.json' -Raw | ConvertFrom-Json
        $env:SOFIA_OPENAI_API_KEY = $localConfig.OpenAI.ApiKey
    }
    if (-not $env:SOFIA_OPENAI_API_KEY -and -not $hasDotEnvKey) {
        throw 'Configure SOFIA_OPENAI_API_KEY no .env, no ambiente ou OpenAI:ApiKey no appsettings local.'
    }
    $composeArgs = @('compose', 'up', '-d')
    if (-not $NoBuild) { $composeArgs += '--build' }
    & docker @composeArgs
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível iniciar a stack Docker.' }
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try {
            $health = Invoke-WebRequest 'http://localhost:5151/health' -TimeoutSec 3
            $web = Invoke-WebRequest 'http://localhost:5275/' -TimeoutSec 3
            if ($health.StatusCode -eq 200 -and $web.StatusCode -eq 200) { $ready = $true; break }
        } catch { }
        Start-Sleep -Seconds 2
    }
    if (-not $ready) { throw 'Os serviços não ficaram prontos. Consulte docker compose logs api web.' }
    Write-Host 'SOFIA: http://localhost:5275'
    Write-Host 'Conhecimentos: http://localhost:5275/conhecimentos'
    Write-Host 'API: http://localhost:5151/swagger'
} finally { Pop-Location }
