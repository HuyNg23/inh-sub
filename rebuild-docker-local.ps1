param(
    [string]$SqlSaPassword = "YourStrong!Passw0rd1",
    [string]$RootAdminUser = "iboxroot1",
    [string]$RootAdminPassword = "Admin@123",
    [switch]$ResetDatabase
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[STEP] $Message" -ForegroundColor Cyan
}

function Set-EnvValue {
    param(
        [string]$FilePath,
        [string]$Key,
        [string]$Value
    )

    if (-not (Test-Path $FilePath)) {
        New-Item -ItemType File -Path $FilePath -Force | Out-Null
    }

    $content = Get-Content -Path $FilePath -Raw
    $newLine = "$Key=$Value"
    if ($content -match "(?m)^$([regex]::Escape($Key))=") {
        $existingLine = [regex]::Match($content, "(?m)^$([regex]::Escape($Key))=.*$").Value
        if ($existingLine -eq $newLine) {
            return
        }
        $content = [regex]::Replace($content, "(?m)^$([regex]::Escape($Key))=.*$", $newLine)
    } else {
        if ($content.Length -gt 0 -and -not $content.EndsWith("`r`n")) {
            $content += "`r`n"
        }
        $content += "$newLine`r`n"
    }

    $maxRetry = 5
    for ($i = 1; $i -le $maxRetry; $i++) {
        try {
            Set-Content -Path $FilePath -Value $content -NoNewline
            return
        } catch [System.IO.IOException] {
            if ($i -eq $maxRetry) {
                throw
            }
            Start-Sleep -Milliseconds 500
        }
    }
}

function Encrypt-DbPassword {
    param([string]$PlainText)

    $key = "aqHQMH7wU1&T0XIZVB[eUi%[D!{aI4[,"
    $iv = "Ex$[.@2:d*Qq}G{c"

    $aes = [System.Security.Cryptography.Aes]::Create()
    $aes.Key = [System.Text.Encoding]::UTF8.GetBytes($key)
    $aes.IV = [System.Text.Encoding]::UTF8.GetBytes($iv)
    $aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
    $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($PlainText)
    $enc = $aes.CreateEncryptor().TransformFinalBlock($bytes, 0, $bytes.Length)
    [Convert]::ToBase64String($enc)
}

function Wait-SqlHealthy {
    param([int]$TimeoutSeconds = 180)

    $elapsed = 0
    while ($elapsed -lt $TimeoutSeconds) {
        $status = docker inspect --format "{{.State.Health.Status}}" backend-sqlserver-1 2>$null
        if ($LASTEXITCODE -eq 0 -and $status -eq "healthy") {
            return
        }
        Start-Sleep -Seconds 3
        $elapsed += 3
    }

    throw "SQL Server did not become healthy in $TimeoutSeconds seconds"
}

function Wait-RootApiReady {
    param([int]$TimeoutSeconds = 180)

    $elapsed = 0
    $payload = @{ user = "system"; body = @{} } | ConvertTo-Json -Depth 4
    while ($elapsed -lt $TimeoutSeconds) {
        try {
            $result = Invoke-RestMethod -Method Post -Uri "http://localhost:5004/api/Environment/CheckEnvironment" -ContentType "application/json" -Body $payload
            if ($null -ne $result) {
                return $result
            }
        } catch {
        }

        Start-Sleep -Seconds 3
        $elapsed += 3
    }

    throw "Root API did not become ready in $TimeoutSeconds seconds"
}

function Invoke-SqlQuery {
    param(
        [string]$Query,
        [string]$Database = "master"
    )

    docker exec backend-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SqlSaPassword" -C -d $Database -h -1 -W -Q $Query
}

function Get-RegisteredServiceNames {
    $result = Invoke-SqlQuery -Database "IBOX_MA01" -Query "SET NOCOUNT ON; SELECT Name FROM S_Services WHERE IsDelete = 0 AND IsOnline = 1 ORDER BY Name;"
    @($result | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() })
}

function Wait-ServiceRegistryReady {
    param(
        [string[]]$ExpectedServices,
        [int]$TimeoutSeconds = 180
    )

    $elapsed = 0
    while ($elapsed -lt $TimeoutSeconds) {
        $registeredServices = Get-RegisteredServiceNames
        $missingServices = @($ExpectedServices | Where-Object { $_ -notin $registeredServices })
        if ($missingServices.Count -eq 0) {
            return $registeredServices
        }

        Start-Sleep -Seconds 3
        $elapsed += 3
    }

    $registeredServices = Get-RegisteredServiceNames
    $missingServices = @($ExpectedServices | Where-Object { $_ -notin $registeredServices })
    throw "Service registry is incomplete. Missing services: $($missingServices -join ', '). Registered services: $($registeredServices -join ', ')"
}

function Get-RootToken {
    $signinPayload = @{
        user = "system"
        body = @{
            UserName = $RootAdminUser
            Password = $RootAdminPassword
        }
    } | ConvertTo-Json -Depth 4

    $signinResult = Invoke-RestMethod -Method Post -Uri "http://localhost:5004/api/XAccount/Signin" -ContentType "application/json" -Body $signinPayload
    if (-not $signinResult -or $signinResult.code -ne "0" -or [string]::IsNullOrWhiteSpace($signinResult.data)) {
        throw "Root sign-in failed: $($signinResult | ConvertTo-Json -Depth 10)"
    }

    return $signinResult.data
}

function Test-BackendConfigurationAggregation {
    param(
        [string]$Token,
        [int]$ExpectedServiceCount = 6
    )

    $headers = @{ Authorization = $Token }
    $result = Invoke-RestMethod -Method Get -Uri "http://localhost:5004/api/GetAllConfiguration" -Headers $headers
    if (-not $result -or $result.code -ne "0") {
        throw "GetAllConfiguration failed: $($result | ConvertTo-Json -Depth 10)"
    }

    $serviceConfigs = @($result.data)
    if ($serviceConfigs.Count -lt $ExpectedServiceCount) {
        $sites = @($serviceConfigs | ForEach-Object { $_.site })
        throw "GetAllConfiguration returned $($serviceConfigs.Count) service configs, expected at least $ExpectedServiceCount. Returned sites: $($sites -join ', ')"
    }

    return $serviceConfigs
}

Write-Step "Checking Docker"
$null = docker --version
$null = docker info

Set-Location -Path $PSScriptRoot

Write-Step "Ensuring docker network ibox-network"
$networkExists = docker network ls --format "{{.Name}}" | Select-String -SimpleMatch "ibox-network"
if (-not $networkExists) {
    docker network create ibox-network | Out-Null
}

Write-Step "Preparing backend .env"
$envPath = Join-Path $PSScriptRoot ".env"
$envExamplePath = Join-Path $PSScriptRoot ".env.example"
if (-not (Test-Path $envPath) -and (Test-Path $envExamplePath)) {
    Copy-Item $envExamplePath $envPath
}

$encryptedDbPassword = Encrypt-DbPassword -PlainText $SqlSaPassword
Set-EnvValue -FilePath $envPath -Key "DB_SERVER" -Value "sqlserver,1433"
Set-EnvValue -FilePath $envPath -Key "DB_NAME" -Value "IBOX_MA01"
Set-EnvValue -FilePath $envPath -Key "DB_USER" -Value "sa"
Set-EnvValue -FilePath $envPath -Key "DB_PASSWORD" -Value $encryptedDbPassword
Set-EnvValue -FilePath $envPath -Key "SQL_SA_PASSWORD" -Value $SqlSaPassword
Set-EnvValue -FilePath $envPath -Key "ASPNETCORE_ENVIRONMENT" -Value "Development"

Write-Step "Starting backend stack (includes SQL Server)"
docker compose up -d

Write-Step "Waiting for SQL Server health"
Wait-SqlHealthy

if ($ResetDatabase) {
    Write-Step "Resetting IBOX_MA01 database"
    docker exec backend-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SqlSaPassword" -C -Q "IF DB_ID('IBOX_MA01') IS NOT NULL BEGIN ALTER DATABASE [IBOX_MA01] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [IBOX_MA01]; END;"
    docker exec backend-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SqlSaPassword" -C -Q "CREATE DATABASE IBOX_MA01;"
}

Write-Step "Ensuring IBOX_MA01 exists"
docker exec backend-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SqlSaPassword" -C -Q "IF DB_ID('IBOX_MA01') IS NULL CREATE DATABASE IBOX_MA01;"

Write-Step "Waiting for Root API readiness"
$checkResult = Wait-RootApiReady

Write-Step "Bootstrapping root environment (creates schema/admin if needed)"
$adminReady = $false
if ($checkResult -and $checkResult.code -eq "0" -and $checkResult.data -and $checkResult.data.database) {
    $adminState = $checkResult.data.database | Where-Object { $_.name -eq "IBox user admin config" }
    if ($adminState -and $adminState.state -eq $true) {
        $adminReady = $true
    }
}

if (-not $adminReady) {
    $buildPayload = @{
        user = "system"
        body = @{
            IBOXAdmin = $RootAdminUser
            IBOXAdminPassword = $RootAdminPassword
        }
    } | ConvertTo-Json -Depth 6

    $buildResult = Invoke-RestMethod -Method Post -Uri "http://localhost:5004/api/Environment/BuildEnvironment" -ContentType "application/json" -Body $buildPayload
    if (-not $buildResult -or $buildResult.code -ne "0") {
        throw "BuildEnvironment failed: $($buildResult | ConvertTo-Json -Depth 10)"
    }
}

$expectedServices = @(
    "RootBEService",
    "ClientBEService",
    "LogService",
    "RestService",
    "ScheduleService",
    "StoreService"
)

Write-Step "Waiting for backend services to register in S_Services"
$registeredServices = Wait-ServiceRegistryReady -ExpectedServices $expectedServices
Write-Host "Registered services: $($registeredServices -join ', ')" -ForegroundColor DarkGray

Write-Step "Verifying GetAllConfiguration with root token"
$rootToken = Get-RootToken
$serviceConfigs = Test-BackendConfigurationAggregation -Token $rootToken -ExpectedServiceCount $expectedServices.Count
Write-Host "Aggregated configuration count: $($serviceConfigs.Count)" -ForegroundColor DarkGray

Write-Step "Starting frontends"
Set-Location -Path (Join-Path $PSScriptRoot "..\frontend")
docker compose up -d
Set-Location -Path (Join-Path $PSScriptRoot "..\master-frontend")
docker compose up -d

Write-Step "Deployment status"
Set-Location -Path (Join-Path $PSScriptRoot "..\backend")
docker compose ps

Write-Host ""
Write-Host "Done. URLs:" -ForegroundColor Green
Write-Host "- Frontend:       http://localhost:8080"
Write-Host "- Root Frontend:  http://localhost:8081"
Write-Host "- Root API:       http://localhost:5004"
Write-Host "- SQL Server:     localhost,1433"
Write-Host "- Root admin:     $RootAdminUser"
Write-Host "- Verified APIs:  /api/Environment/CheckEnvironment, /api/GetAllConfiguration" -ForegroundColor Green
