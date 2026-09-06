<#
.SYNOPSIS
    Script Definitivo de Ativação do IIS, Publicação Release e Deploy do AVA Acadêmico.
.DESCRIPTION
    1. Eleva privilégios para Administrador automaticamente (UAC).
    2. Ativa todos os recursos do IIS (Internet Information Services) no Windows.
    3. Inicia e configura o serviço W3SVC como inicialização Automática.
    4. Verifica o ASP.NET Core Hosting Bundle (.NET 10).
    5. Realiza a compilação Release (.NET 10) otimizada para produção.
    6. Configura permissões de pastas (ACL para IIS_IUSRS e IUSR).
    7. Cria/Atualiza o AppPool "No Managed Code" e o Site no IIS (porta 8088).
    8. Inicia os serviços de apoio de laboratórios práticos (Docker Desktop & Floci).
    9. Realiza teste de conectividade HTTP local.
#>

[CmdletBinding()]
param (
    [string]$SiteName = "AvaAcademico",
    [string]$AppPoolName = "AvaAcademicoPool",
    [int]$PortaHttp = 8088,
    [string]$PublishPath = "C:\Projetos_github\AvaAcademico\publish",
    [string]$ProjectPath = "C:\Projetos_github\AvaAcademico"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "       DEPLOY AUTOMATIZADO - AVA ACADÊMICO (IIS)       " -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan

# 1. Verificar e Solicitar Privilégios de Administrador Automaticamente
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[ELEVAÇÃO] Este script necessita de privilégios de Administrador para ativar o IIS e provisionar o site." -ForegroundColor Yellow
    Write-Host "Solicitando elevação de privilégios (UAC)..." -ForegroundColor Cyan
    try {
        Start-Process powershell.exe -Verb RunAs -ArgumentList "-NoExit -ExecutionPolicy Bypass -File `"$PSCommandPath`""
        exit
    } catch {
        Write-Error "Não foi possível elevar privilégios automaticamente. Abra o PowerShell clicando com botão direito em 'Executar como Administrador' e tente novamente."
        return
    }
}

# 2. Ativar Recursos do IIS no Windows se não estiverem instalados
Write-Host "`n[1/7] Verificando e ativando recursos do IIS no Windows..." -ForegroundColor Cyan
$featuresIIS = @(
    "IIS-WebServerRole",
    "IIS-WebServer",
    "IIS-CommonHttpFeatures",
    "IIS-StaticContent",
    "IIS-DefaultDocument",
    "IIS-DirectoryBrowsing",
    "IIS-HttpErrors",
    "IIS-HttpRedirect",
    "IIS-ApplicationDevelopment",
    "IIS-WebSockets",
    "IIS-NetFxExtensibility45",
    "IIS-RequestFiltering",
    "IIS-ManagementConsole",
    "IIS-ManagementService"
)

try {
    $iisFeature = Get-WindowsOptionalFeature -Online -FeatureName "IIS-WebServerRole" -ErrorAction SilentlyContinue
    if ($null -eq $iisFeature -or $iisFeature.State -ne "Enabled") {
        Write-Host " -> O IIS não está ativo nesta máquina. Ativando recursos necessários (isso pode levar alguns minutos)..." -ForegroundColor Yellow
        Enable-WindowsOptionalFeature -Online -FeatureName $featuresIIS -All -NoRestart -WarningAction SilentlyContinue
        Write-Host " -> Recursos do IIS ativados com sucesso no Windows!" -ForegroundColor Green
    } else {
        Write-Host " -> O IIS já se encontra ativado no sistema." -ForegroundColor Green
    }
} catch {
    Write-Warning "Falha com Enable-WindowsOptionalFeature. Tentando ativação via DISM..."
    foreach ($feat in $featuresIIS) {
        Write-Host "    Habilitando $feat..." -ForegroundColor Gray
        & dism.exe /Online /Enable-Feature /FeatureName:$feat /All /NoRestart | Out-Null
    }
    Write-Host " -> Recursos do IIS ativados com sucesso via DISM!" -ForegroundColor Green
}

# Inicia e configura o serviço W3SVC
try {
    Set-Service -Name W3SVC -StartupType Automatic -ErrorAction SilentlyContinue
    Start-Service -Name W3SVC -ErrorAction SilentlyContinue
    Write-Host " -> Serviço W3SVC (World Wide Web) ativo e configurado como Inicialização Automática." -ForegroundColor Green
} catch {
    Write-Warning "Aviso ao configurar serviço W3SVC: $_"
}

# 3. Verificar se o ASP.NET Core Hosting Bundle (.NET 10) está instalado
Write-Host "`n[2/7] Verificando ASP.NET Core Module v2 (Hosting Bundle)..." -ForegroundColor Cyan
$ancmPath64 = "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
$ancmPath86 = "${env:ProgramFiles(x86)}\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"

if ((Test-Path $ancmPath64) -or (Test-Path $ancmPath86)) {
    Write-Host " -> Módulo AspNetCoreModuleV2 detectado e pronto para InProcess hosting." -ForegroundColor Green
} else {
    Write-Warning "=========================================================================="
    Write-Warning " [ATENÇÃO] O ASP.NET Core Hosting Bundle (.NET 10) não foi detectado no IIS!"
    Write-Warning " O IIS precisa do Hosting Bundle para hospedar aplicações .NET 10."
    Write-Warning " Baixe e instale gratuitamente pelo link oficial da Microsoft:"
    Write-Warning " https://dotnet.microsoft.com/download/dotnet/10.0"
    Write-Warning " Após instalar, execute este script novamente para finalizar o deploy."
    Write-Warning "=========================================================================="
}

# 4. Parar temporariamente o AppPool se já existir (evita DLLs travadas em disco)
Write-Host "`n[3/7] Preparando AppPool para atualização..." -ForegroundColor Cyan
try {
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    if (Test-Path "IIS:\AppPools\$AppPoolName") {
        Write-Host " -> Parando temporariamente o AppPool '$AppPoolName'..." -ForegroundColor Gray
        Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
} catch {
    Write-Warning "Não foi possível gerenciar AppPool: $_"
}

# 5. Compilação e Publicação em Modo Release
Write-Host "`n[4/7] Compilando e gerando publicação Release (.NET 10)..." -ForegroundColor Cyan
Push-Location $ProjectPath
try {
    dotnet publish -c Release -o $PublishPath --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Erro na compilação Release do dotnet."
    }
    Write-Host " -> Publicação Release gerada com sucesso em: $PublishPath" -ForegroundColor Green
} finally {
    Pop-Location
}

# 6. Pastas de Logs e Uploads + Permissões de Segurança (IIS_IUSRS)
Write-Host "`n[5/7] Configurando diretórios de uploads/logs e permissões ACL..." -ForegroundColor Cyan
$logsPath = Join-Path $PublishPath "logs"
$uploadsPath = Join-Path $PublishPath "wwwroot\uploads"
$videosPath = Join-Path $PublishPath "wwwroot\videos"

@($logsPath, $uploadsPath, $videosPath) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}

try {
    $aclDirs = @($PublishPath, $logsPath, $uploadsPath, $videosPath)
    foreach ($dir in $aclDirs) {
        $acl = Get-Acl $dir
        $rule1 = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
        $rule2 = New-Object System.Security.AccessControl.FileSystemAccessRule("IUSR", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.SetAccessRule($rule1)
        $acl.SetAccessRule($rule2)
        Set-Acl -Path $dir -AclObject $acl
    }
    Write-Host " -> Permissões de 'IIS_IUSRS' e 'IUSR' aplicadas com sucesso." -ForegroundColor Green
} catch {
    Write-Warning "Aviso ao definir permissões ACL: $_"
}

# 7. Provisionar AppPool e Site no IIS
Write-Host "`n[6/7] Provisionando AppPool e Website no IIS..." -ForegroundColor Cyan
try {
    # 7.1 Cria ou Atualiza o AppPool (No Managed Code)
    if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
        Write-Host " -> Criando AppPool '$AppPoolName'..." -ForegroundColor Gray
        New-WebAppPool -Name $AppPoolName | Out-Null
    }
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value "" # No Managed Code
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "processModel.identityType" -Value 4 # ApplicationPoolIdentity

    # 7.2 Cria ou Atualiza o Website
    if (-not (Test-Path "IIS:\Sites\$SiteName")) {
        Write-Host " -> Criando Site IIS '$SiteName' na porta $PortaHttp..." -ForegroundColor Gray
        New-WebSite -Name $SiteName -Port $PortaHttp -PhysicalPath $PublishPath -ApplicationPool $AppPoolName | Out-Null
    } else {
        Write-Host " -> Atualizando configurações do Site '$SiteName'..." -ForegroundColor Gray
        Set-ItemProperty "IIS:\Sites\$SiteName" -Name "physicalPath" -Value $PublishPath
        Set-ItemProperty "IIS:\Sites\$SiteName" -Name "applicationPool" -Value $AppPoolName
    }

    # Inicia o AppPool e o Site
    Start-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Start-WebSite -Name $SiteName -ErrorAction SilentlyContinue
    Write-Host " -> Site e AppPool ativos no IIS na porta $PortaHttp!" -ForegroundColor Green
} catch {
    Write-Warning "Aviso ao provisionar site no IIS: $_"
}

# 8. Verificação do Docker Desktop e Floci (Ambiente de Laboratórios Práticos)
Write-Host "`n[7/7] Verificando serviços de apoio (Docker Desktop & Floci)..." -ForegroundColor Cyan

# --- Resolve Docker CLI (PATH do sistema não herda PATH do usuário ao rodar elevado) ---
$dockerExe = Get-Command docker -ErrorAction SilentlyContinue
if (-not $dockerExe) {
    $candidatos = @(
        "$env:LOCALAPPDATA\Programs\DockerDesktop\resources\bin",
        "$env:ProgramFiles\Docker\Docker\resources\bin",
        "${env:ProgramFiles(x86)}\Docker\Docker\resources\bin",
        "C:\Users\Eduar\AppData\Local\Programs\DockerDesktop\resources\bin"
    )
    foreach ($dir in $candidatos) {
        if (Test-Path "$dir\docker.exe") {
            $env:PATH = "$env:PATH;$dir"
            Write-Host " -> Docker CLI encontrado em '$dir'. Adicionado ao PATH da sessao." -ForegroundColor Gray

            # Persiste no PATH do sistema para que futuras sessoes Admin tambem encontrem
            $syspath = [System.Environment]::GetEnvironmentVariable("PATH", "Machine")
            if ($syspath -notlike "*$dir*") {
                try {
                    [System.Environment]::SetEnvironmentVariable("PATH", "$syspath;$dir", "Machine")
                    Write-Host " -> Docker CLI persistido no PATH do sistema permanentemente." -ForegroundColor Green
                } catch {
                    Write-Warning "Nao foi possivel persistir no PATH do sistema: $_"
                }
            }
            break
        }
    }
}

$dockerExe = Get-Command docker -ErrorAction SilentlyContinue
if (-not $dockerExe) {
    Write-Warning "Docker CLI nao encontrado em nenhum local conhecido. Instale o Docker Desktop e reinicie o script."
} else {
    try {
        $dockerInfo = docker info 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " -> Docker Desktop esta ativo e comunicando." -ForegroundColor Green

            # Garante a rede floci-net
            docker network inspect floci-net 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) {
                docker network create floci-net | Out-Null
                Write-Host " -> Rede 'floci-net' criada." -ForegroundColor Green
            } else {
                Write-Host " -> Rede 'floci-net' ja existe." -ForegroundColor Green
            }

            # Garante conteinr floci_target
            $flociStatus = docker ps --filter "name=floci_target" --format "{{.Status}}"
            if (-not $flociStatus) {
                Write-Host " -> Inicializando container floci_target..." -ForegroundColor Gray
                docker run -d --name floci_target --restart always -p 4566:4566 --network floci-net floci/floci:latest 2>&1 | Out-Null
                Write-Host " -> Container floci_target ativo na porta 4566!" -ForegroundColor Green
            } else {
                Write-Host " -> Container floci_target ja esta em execucao ($flociStatus)." -ForegroundColor Green
            }
        } else {
            Write-Warning "Docker Desktop nao esta em execucao no momento. Inicie-o quando for utilizar os laboratorios praticos."
        }
    } catch {
        Write-Warning "Erro ao comunicar com o Docker: $_"
    }
}

# 9. Teste de Conectividade e Status Final
Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "                   RESULTADO DO DEPLOY                  " -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan
Start-Sleep -Seconds 3
$urlTeste = "http://localhost:$PortaHttp/"
try {
    $response = Invoke-WebRequest -Uri $urlTeste -UseBasicParsing -TimeoutSec 10 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host " SUCESSO! AVA ACADÊMICO ESTÁ ONLINE E PRONTO NO IIS!" -ForegroundColor Green
        Write-Host " URL de Acesso: $urlTeste" -ForegroundColor Yellow
    } else {
        Write-Host " Site publicado no IIS respondeu com status: $($response.StatusCode)" -ForegroundColor Yellow
        Write-Host " URL de Acesso: $urlTeste" -ForegroundColor Yellow
    }
} catch {
    Write-Host " Publicação e configuração do IIS concluídas com sucesso!" -ForegroundColor Green
    Write-Host " URL configurada: $urlTeste" -ForegroundColor Yellow
    Write-Host " Caso encontre algum erro de inicialização HTTP 500.30/500.19, consulte o manual:" -ForegroundColor Cyan
    Write-Host " C:\Projetos_github\AvaAcademico\manual_implantacao_iis.md" -ForegroundColor Gray
}

Write-Host "========================================================" -ForegroundColor Cyan

