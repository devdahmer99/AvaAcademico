# ==============================================================================
# Script de Configuração e Publicação Automática no IIS - EJLAcademy AVA
# Executar como Administrador no PowerShell
# ==============================================================================

# 1. Verifica privilégios de Administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[ERRO] Este script precisa ser executado como Administrador!" -ForegroundColor Red
    Write-Host "Abra o PowerShell clicando com o botão direito -> 'Executar como Administrador' e rode este script novamente." -ForegroundColor Yellow
    Exit 1
}

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host " Configurando EJLAcademy AVA no IIS (Hospedagem 24/7) " -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

# 2. Configurações de Diretório e IIS
$ProjectDir   = "C:\AvaAcademico"
$PublishDir   = "C:\inetpub\wwwroot\AvaAcademico"
$AppPoolName  = "AvaAcademicoAppPool"
$SiteName     = "AvaAcademico"
$Port         = 8080
$appcmd       = "$env:windir\system32\inetsrv\appcmd.exe"

# 3. Habilita serviços do IIS no Windows se necessário
Write-Host "`n[1/6] Verificando serviços do IIS..." -ForegroundColor Yellow
Start-Service W3SVC -ErrorAction SilentlyContinue

# 4. Verifica o ASP.NET Core Hosting Bundle
$hostingBundleV2 = (Test-Path "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll") -or (Test-Path "C:\Program Files (x86)\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll")
if (-not $hostingBundleV2) {
    Write-Host "[AVISO] O 'ASP.NET Core Hosting Bundle' não foi detectado no IIS." -ForegroundColor Yellow
    Write-Host "Para o IIS executar aplicações .NET Core / .NET 10, instale o pacote de hospedagem oficial:" -ForegroundColor Yellow
    Write-Host "-> Comando winget: winget install Microsoft.DotNet.HostingBundle.10 --silent" -ForegroundColor White
    Write-Host "-> Ou baixe diretamente em: https://dotnet.microsoft.com/download/dotnet" -ForegroundColor White
    Write-Host "O script continuará publicando e criando a estrutura do site.`n" -ForegroundColor DarkGray
}

# 5. Pausa temporária do IIS para liberar arquivos em uso (DLL Lock)
Write-Host "[2/6] Preparando publicação e liberando processos do IIS..." -ForegroundColor Yellow
if (Test-Path $PublishDir) {
    # Cria arquivo app_offline.htm para desligar o worker process graciosamente
    Set-Content -Path "$PublishDir\app_offline.htm" -Value "<h1>Atualizando aplicacao...</h1>" -Force -ErrorAction SilentlyContinue
}
& $appcmd stop apppool "$AppPoolName" 2>$null
& $appcmd stop site "$SiteName" 2>$null
Start-Sleep -Seconds 1

# 6. Compilação e Publicação do Projeto em Release
Write-Host "[3/6] Publicando a aplicação em modo Release..." -ForegroundColor Yellow
Set-Location $ProjectDir
dotnet publish -c Release -o $PublishDir --nologo

# Remove o app_offline.htm após a publicação
if (Test-Path "$PublishDir\app_offline.htm") {
    Remove-Item "$PublishDir\app_offline.htm" -Force -ErrorAction SilentlyContinue
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERRO] Falha na compilação e publicação do projeto." -ForegroundColor Red
    Exit 1
}

# 7. Criação de Pastas Críticas de Armazenamento
Write-Host "[4/6] Criando diretórios de uploads e logs..." -ForegroundColor Yellow
$foldersToCreate = @(
    "$PublishDir\logs",
    "$PublishDir\wwwroot\videos",
    "$PublishDir\wwwroot\uploads\avatares",
    "$PublishDir\wwwroot\uploads\livros"
)
foreach ($folder in $foldersToCreate) {
    if (-not (Test-Path $folder)) {
        New-Item -ItemType Directory -Path $folder -Force | Out-Null
    }
}

# 8. Configuração de Permissões NTFS (Leitura e Gravação para o IIS)
Write-Host "[5/6] Configurando permissões de escrita para uploads e vídeos..." -ForegroundColor Yellow
icacls "$PublishDir" /grant "IIS_IUSRS:(OI)(CI)F" /T /Q
icacls "$PublishDir" /grant "IUSR:(OI)(CI)F" /T /Q
$appPoolGrant = "IIS AppPool\" + $AppPoolName + ":(OI)(CI)F"
icacls "$PublishDir" /grant $appPoolGrant /T /Q 2>$null

# 9. Configuração e Inicialização do Application Pool e Site no IIS
Write-Host "[6/6] Configurando e iniciando Site e Application Pool no IIS..." -ForegroundColor Yellow

# Deleta pool existente se houver e recria
& $appcmd delete apppool "$AppPoolName" 2>$null
& $appcmd add apppool /name:"$AppPoolName" /managedRuntimeVersion:"" /managedPipelineMode:"Integrated"
& $appcmd set apppool "$AppPoolName" /startMode:"AlwaysRunning"
& $appcmd set apppool "$AppPoolName" /processModel.idleTimeout:"00:00:00"

# Deleta site existente se houver e recria
& $appcmd delete site "$SiteName" 2>$null
$bindingStr = "http/*:" + $Port + ":"
& $appcmd add site /name:"$SiteName" /bindings:$bindingStr /physicalPath:"$PublishDir"
$poolConfig = "/[path='/'].applicationPool:" + $AppPoolName
& $appcmd set site "$SiteName" $poolConfig

# Inicia o AppPool e o Site
& $appcmd start apppool "$AppPoolName" 2>$null
& $appcmd start site "$SiteName" 2>$null
& $appcmd recycle apppool "$AppPoolName" 2>$null

Write-Host "`n============================================================" -ForegroundColor Green
Write-Host " 🎉 EJLAcademy AVA configurado e atualizado com sucesso! " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Site publicado em:  $PublishDir" -ForegroundColor White
Write-Host "Endereço de Acesso: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "Application Pool:   $AppPoolName (AlwaysRunning, No Managed Code)" -ForegroundColor White
Write-Host "Status do Serviço:  Em execução contínua no Windows (24/7)" -ForegroundColor White
Write-Host "============================================================`n" -ForegroundColor Green
