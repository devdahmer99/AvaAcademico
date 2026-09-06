# Manual Definitivo de Implantação e Operação de Ambientes: AVA Acadêmico

> **Ambiente Alvo:** Windows Server / Windows 10 / Windows 11  
> **Servidor Web:** Internet Information Services (IIS) com ASP.NET Core Module v2 (InProcess)  
> **Modo de Execução:** Release (.NET 10 Otimizado)  
> **Banco de Dados:** Microsoft SQL Server (`baseERP`)  
> **Laboratórios Práticos & Cloud:** Docker Desktop + Floci Engine (AWS emulation)  

---

## 📋 Sumário
1. [Visão Geral da Arquitetura](#1-visão-geral-da-arquitetura)
2. [Pré-requisitos do Sistema](#2-pré-requisitos-do-sistema)
3. [Passo a Passo de Instalação do IIS e Módulos](#3-passo-a-passo-de-instalação-do-iis-e-módulos)
4. [Instalação do ASP.NET Core Hosting Bundle (.NET 10)](#4-instalação-do-aspnet-core-hosting-bundle-net-10)
5. [Configuração do Banco de Dados SQL Server](#5-configuração-do-banco-de-dados-sql-server)
6. [Deploy da Aplicação em Modo Release](#6-deploy-da-aplicação-em-modo-release)
7. [Configuração do Application Pool e Website no IIS](#7-configuração-do-application-pool-e-website-no-iis)
8. [Configuração das Permissões de Pastas (ACL)](#8-configuração-das-permissões-de-pastas-acl)
9. [Subida do Ambiente de Laboratórios Docker & Floci](#9-subida-do-ambiente-de-laboratórios-docker--floci)
10. [Deploy Automatizado em 1 Clique (`deploy-release.ps1`)](#10-deploy-automatizado-em-1-clique-deploy-releaseps1)
11. [Guia de Resolução de Problemas (Troubleshooting IIS)](#11-guia-de-resolução-de-problemas-troubleshooting-iis)

---

## 1. Visão Geral da Arquitetura

O **AVA Acadêmico** em produção opera sob a seguinte topologia de alta performance:

```
[ Usuário / Navegador ]
        │ HTTP/HTTPS (Porta 80 / 443 / 8088)
        ▼
┌─────────────────────────────────────────────────────────────┐
│ Windows IIS (Internet Information Services)                 │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐   │
│   │ AppPool: AvaAcademicoPool (No Managed Code)         │   │
│   │                                                     │   │
│   │  ┌───────────────────────────────────────────────┐  │   │
│   │  │ AspNetCoreModuleV2 (InProcess)                │  │   │
│   │  │  -> AvaAcademico.dll (.NET 10 Release)        │  │   │
│   │  │  -> Caching estático de CSS, JS, Imagens      │  │   │
│   │  │  -> Suporte a upload de arquivos de até 500MB │  │   │
│   │  └───────────────────────┬───────────────────────┘  │   │
│   └──────────────────────────┼──────────────────────────┘   │
└──────────────────────────────┼──────────────────────────────┘
                               │
            ┌──────────────────┴──────────────────┐
            ▼                                     ▼
┌──────────────────────┐             ┌─────────────────────────┐
│ SQL Server (baseERP) │             │ Docker Desktop Daemon   │
│ - Identity / Users   │             │ ┌─────────────────────┐ │
│ - Cursos & Aulas     │             │ │ floci_target (4566) │ │
│ - Anotações & Labs   │             │ │ Contêineres de labs │ │
│ - Gamificação & XP   │             │ └─────────────────────┘ │
└──────────────────────┘             └─────────────────────────┘
```

---

## 2. Pré-requisitos do Sistema

| Componente | Versão Recomendada | Função |
| :--- | :--- | :--- |
| **Sistema Operacional** | Windows 10/11 Pro/Enterprise ou Windows Server 2019/2022 | Execução de IIS e Virtualização |
| **IIS** | Versão 10.0+ | Servidor Web de Produção |
| **.NET Runtime & Hosting** | .NET 10.0 Hosting Bundle | Módulo `AspNetCoreModuleV2` para IIS |
| **Banco de Dados** | Microsoft SQL Server 2019/2022 ou SQL Express | Armazenamento de dados `baseERP` |
| **Docker Desktop** | Versão 4.25+ (com WSL2 Engine) | Execução de laboratórios práticos e Floci |

---

## 3. Ativação do IIS e Módulos no Windows

> [!TIP]
> **Modo Automático:** O script [`deploy-release.ps1`](file:///C:/Projetos_github/AvaAcademico/deploy-release.ps1) já detecta se o IIS está ausente e **ativa todos os recursos necessários automaticamente**! Você pode simplesmente executar o script. Se preferir fazer manualmente, utilize as instruções abaixo:

### Opção A: Via PowerShell (Como Administrador)
Abra o **PowerShell como Administrador** e execute:

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName `
    IIS-WebServerRole, `
    IIS-WebServer, `
    IIS-CommonHttpFeatures, `
    IIS-StaticContent, `
    IIS-DefaultDocument, `
    IIS-HttpErrors, `
    IIS-HttpRedirect, `
    IIS-ApplicationDevelopment, `
    IIS-WebSockets, `
    IIS-RequestFiltering, `
    IIS-ManagementConsole -All
```

### Opção B: Via Interface Gráfica do Windows
1. Pressione `Win + R`, digite `optionalfeatures` e tecle **Enter**.
2. Localize e marque **Serviços de Informações da Internet (IIS)**:
   - **Serviços da World Wide Web**:
     - *Recursos HTTP Comuns*: Conteúdo Estático, Documento Padrão, Erros HTTP.
     - *Segurança*: Filtragem de Solicitações.
     - *Recursos de Desenvolvimento de Aplicativos*: Protocolo WebSocket.
   - **Ferramentas de Gerenciamento da Web**: Console de Gerenciamento do IIS.
3. Clique em **OK** e aguarde o Windows aplicar as alterações.

---

## 4. Instalação do ASP.NET Core Hosting Bundle (.NET 10)

Para que o IIS compreenda e execute o AVA em modo **InProcess**, é indispensável instalar o pacote oficial da Microsoft:

1. Baixe o **.NET 10 Hosting Bundle** pelo site oficial:  
   🔗 [Download .NET 10 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Execute o instalador `dotnet-hosting-10.x.x-win.exe` como Administrador.
3. Após a conclusão, reinicie o serviço do IIS abrindo o prompt e digitando:
   ```cmd
   net stop was /y
   net start w3svc
   ```
4. **Validação:** Verifique se o módulo `AspNetCoreModuleV2` está registrado no IIS:
   Abra o **Gerenciador do IIS** (`inetmgr`) -> Clique no nome do servidor -> Dê dois cliques em **Módulos** (Modules) e certifique-se de que `AspNetCoreModuleV2` consta na lista.

---

## 5. Configuração do Banco de Dados SQL Server

A aplicação conecta-se à base `baseERP` no SQL Server.

1. **Nome da Instância:** `DESKTOP-C2N4P6Q` (ou `localhost` / `.\SQLEXPRESS`).
2. **String de Conexão no `appsettings.Production.json`:**
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=DESKTOP-C2N4P6Q;Initial Catalog=baseERP;Integrated Security=True;Pooling=True;Max Pool Size=100;Min Pool Size=5;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=True;Application Name=\"AvaAcademico-IIS\";Command Timeout=30"
   }
   ```
3. **Permissão de Acesso ao SQL Server para o IIS:**
   Como o IIS executa o AppPool como `IIS AppPool\AvaAcademicoPool`, configure o SQL Server Management Studio (SSMS):
   ```sql
   USE [master];
   IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'IIS APPPOOL\AvaAcademicoPool')
   BEGIN
       CREATE LOGIN [IIS APPPOOL\AvaAcademicoPool] FROM WINDOWS;
   END
   GO

   USE [baseERP];
   IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'IIS APPPOOL\AvaAcademicoPool')
   BEGIN
       CREATE USER [IIS APPPOOL\AvaAcademicoPool] FOR LOGIN [IIS APPPOOL\AvaAcademicoPool];
   END
   ALTER ROLE [db_datareader] ADD MEMBER [IIS APPPOOL\AvaAcademicoPool];
   ALTER ROLE [db_datawriter] ADD MEMBER [IIS APPPOOL\AvaAcademicoPool];
   ALTER ROLE [db_ddladmin] ADD MEMBER [IIS APPPOOL\AvaAcademicoPool];
   GO
   ```
   *(Nota: Se preferir utilizar autenticação com usuário e senha no SQL Server, basta alterar o `DefaultConnection` para `User ID=sa;Password=SuaSenhaForte;`)*.

---

## 6. Deploy da Aplicação em Modo Release

Para compilar e publicar os arquivos finais otimizados:

```powershell
# No terminal na raiz do projeto C:\Projetos_github\AvaAcademico
dotnet publish -c Release -o C:\Projetos_github\AvaAcademico\publish
```

A pasta `publish` conterá:
- `AvaAcademico.dll` e dependências compiladas sem símbolos de depuração.
- `web.config` com `AspNetCoreModuleV2` em `hostingModel="inprocess"`.
- `appsettings.Production.json` ativo.
- `wwwroot` com todos os assets minificados e compactados.

---

## 7. Configuração do Application Pool e Website no IIS

### 7.1. Criar o Application Pool (No Managed Code)
1. Abra o **Gerenciador do IIS** (`inetmgr`).
2. No menu esquerdo, clique em **Pools de Aplicativos** (Application Pools).
3. Clique com o botão direito -> **Adicionar Pool de Aplicativos...**
   - **Nome:** `AvaAcademicoPool`
   - **Versão do .NET CLR:** **Sem Código Gerenciado** (*No Managed Code*)  
     *(⚠️ CRÍTICO: Não selecione .NET v4.0 nem .NET v2.0)*.
   - **Modo de Pipeline Gerenciado:** **Integrado** (*Integrated*).
4. Clique em **OK**.
5. Clique com o botão direito no `AvaAcademicoPool` -> **Configurações Avançadas**:
   - **Identidade:** Deixe como `ApplicationPoolIdentity` (ou informe seu usuário local com permissões).
   - **Tempo Limite de Ociosidade (minutos):** `0` (evita que o site durma se ficar ocioso).
   - **Reciclar Intervalo de Tempo Fixo (minutos):** `0` (ou `1440` para uma vez ao dia).

### 7.2. Criar o Website
1. No menu esquerdo, clique com o botão direito em **Sites** -> **Adicionar Site...**
   - **Nome do Site:** `AvaAcademico`
   - **Pool de Aplicativos:** Selecione `AvaAcademicoPool`
   - **Caminho Físico:** `C:\Projetos_github\AvaAcademico\publish`
   - **Tipo:** `http`
   - **Endereço IP:** `Todos Não Atribuídos`
   - **Porta:** `8088` (ou `80` se a porta estiver livre)
2. Clique em **OK**.

---

## 8. Configuração das Permissões de Pastas (ACL)

O IIS precisa de permissão de leitura sobre a pasta `publish` e gravação sobre as pastas de logs e uploads.

Execute no **PowerShell como Administrador**:

```powershell
$publishDir = "C:\Projetos_github\AvaAcademico\publish"

# Cria pastas críticas caso ainda não existam
New-Item -ItemType Directory -Path "$publishDir\logs" -Force | Out-Null
New-Item -ItemType Directory -Path "$publishDir\wwwroot\uploads" -Force | Out-Null
New-Item -ItemType Directory -Path "$publishDir\wwwroot\videos" -Force | Out-Null

# Concede permissão de Leitura e Execução para IIS_IUSRS na raiz
icacls $publishDir /grant "IIS_IUSRS:(OI)(CI)RX" /t

# Concede permissão total de Gravação nas pastas de logs e uploads
icacls "$publishDir\logs" /grant "IIS_IUSRS:(OI)(CI)F" /t
icacls "$publishDir\wwwroot\uploads" /grant "IIS_IUSRS:(OI)(CI)F" /t
icacls "$publishDir\wwwroot\videos" /grant "IIS_IUSRS:(OI)(CI)F" /t
```

---

## 9. Subida do Ambiente de Laboratórios Docker & Floci

O AVA Acadêmico possui um motor de cibersegurança e cloud pentesting integrado com Docker Desktop e Floci.

### 9.1. Inicializar o Docker Desktop
1. Abra o **Docker Desktop** no Windows e aguarde até o status indicar **Engine Running** (verde).

### 9.2. Subir a Infraestrutura do Floci
Abra o terminal PowerShell e execute:

```powershell
# 1. Cria a rede isolada de pentest
docker network create floci-net 2>$null

# 2. Executa o contêiner emulado Floci (AWS Cloud Pentesting)
docker run -d `
  --name floci_target `
  --restart always `
  -p 4566:4566 `
  --network floci-net `
  floci/floci:latest
```

### 9.3. Validar Status dos Serviços de Apoio
Execute no terminal:
```powershell
docker ps
```
Você verá o contêiner `floci_target` saudável e ouvindo requisições na porta local `4566`. Ao iniciar um laboratório prático pelo portal, a aplicação automaticamente inicializará contêineres de exploração (`ttyd`, `kali-web`, `dvwa`, etc.) e gerenciará as portas dinamicamente.

---

## 10. Deploy Automatizado em 1 Clique (`deploy-release.ps1`)

Para facilitar sua vida operacional, foi criado o script [`deploy-release.ps1`](file:///C:/Projetos_github/AvaAcademico/deploy-release.ps1) na raiz do projeto.

Ele realiza **todas** as etapas anteriores automaticamente:
1. Para o AppPool no IIS para evitar travamento de DLLs.
2. Executa `dotnet publish -c Release`.
3. Garante e configura as permissões de pastas (`IIS_IUSRS`).
4. Cria ou atualiza o Website e o AppPool no IIS na porta `8088`.
5. Valida se o Docker e o Floci estão ativos.
6. Inicia o site e realiza um teste de integridade HTTP.

### Como Executar:

#### Opção 1: Via Arquivo em Lote (Mais Fácil - Ignora Restrições do PowerShell)
Basta dar **duplo clique** no arquivo ou executar no terminal:
```cmd
cd C:\Projetos_github\AvaAcademico
.\deploy.bat
```

#### Opção 2: Via PowerShell com Parâmetro Bypass
```powershell
cd C:\Projetos_github\AvaAcademico
powershell -ExecutionPolicy Bypass -File .\deploy-release.ps1
```
4. Ao final, acesse no seu navegador:  
   👉 **`http://localhost:8088`**

---

## 11. Guia de Resolução de Problemas (Troubleshooting IIS)

### Erro HTTP 500.19 - Erro de Configuração
* **Causa:** O IIS não reconhece as tags `<aspNetCore>` no `web.config`.
* **Solução:** O **.NET Hosting Bundle** não foi instalado ou o IIS não foi reiniciado após a instalação. Instale o Hosting Bundle e rode `iisreset`.

### Erro HTTP 500.30 - Falha ao Iniciar Processo no IIS (InProcess Failure)
* **Causa:** A aplicação disparou uma exceção durante o `Program.cs` ou na inicialização do banco de dados.
* **Solução:**
  1. Abra a pasta `C:\Projetos_github\AvaAcademico\publish\logs`.
  2. Abra o arquivo mais recente `stdout_xxxx.log`.
  3. A causa exata (ex: falha de autenticação no SQL Server) estará registrada no topo do log.

### Erro HTTP 502.5 - Falha ao Iniciar o Processo
* **Causa:** O executável `dotnet.exe` não está no PATH do sistema acessível pelo IIS ou a DLL `AvaAcademico.dll` está ausente.
* **Solução:** Certifique-se de que o caminho `C:\Program Files\dotnet` está nas variáveis de ambiente do Sistema.

### Erro de Conexão com o Banco de Dados (Login failed for user ...)
* **Causa:** O `ApplicationPoolIdentity` não possui permissão no SQL Server.
* **Solução:** Execute o script SQL da **Seção 5** para conceder permissão ao `IIS APPPOOL\AvaAcademicoPool`, ou configure uma conta `sa` no arquivo `appsettings.Production.json`.

---

## ✅ Resumo de Links e Comandos Rápidos

* **Acesso Web:** `http://localhost:8088`
* **Painel IIS:** `inetmgr`
* **Reiniciar IIS:** `iisreset`
* **Logs da Aplicação:** `C:\Projetos_github\AvaAcademico\publish\logs`
* **Caderno de Anotações:** `http://localhost:8088/Perfil/Anotacoes`

