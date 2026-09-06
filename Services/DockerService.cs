using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace AvaAcademico.Services;

public class DockerService : IDockerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DockerService> _logger;

    public DockerService(IServiceProvider serviceProvider, ILogger<DockerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<bool> IsDockerDisponivelAsync()
    {
        try
        {
            var (exitCode, stdout, _) = await ExecutarComandoAsync("docker", "info --format \"{{.ServerVersion}}\"", 5000);
            return exitCode == 0 && !string.IsNullOrWhiteSpace(stdout);
        }
        catch
        {
            return false;
        }
    }

    public async Task<ResultadoDocker> IniciarLaboratorioAsync(Laboratorio lab, string usuarioId)
    {
        var disponivel = await IsDockerDisponivelAsync();
        if (!disponivel)
        {
            TentarIniciarDockerDesktop();

            return new ResultadoDocker
            {
                Sucesso = false,
                Mensagem = "⚠️ O Docker Desktop não estava em execução! O aplicativo foi chamado para abrir no seu notebook. Por favor, aguarde o ícone da baleia do Docker terminar de carregar na barra de tarefas e clique em 'Iniciar Laboratório' novamente."
            };
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AvaContext>();

        // 1. Verifica se já existe uma instância em execução para este aluno e lab
        var instanciaExistente = await context.InstanciasLaboratorios
            .FirstOrDefaultAsync(i => i.LaboratorioId == lab.Id && i.UsuarioId == usuarioId && i.Status == "Executando" && i.ExpiraEm > DateTime.UtcNow);

        if (instanciaExistente != null)
        {
            // Confirma se o container realmente continua rodando no Docker
            if (await IsContainerExecutandoAsync(instanciaExistente.ContainerId))
            {
                return new ResultadoDocker
                {
                    Sucesso = true,
                    ContainerId = instanciaExistente.ContainerId,
                    PortaHost = instanciaExistente.PortaHost,
                    Mensagem = "Laboratório já está em execução!"
                };
            }
            else
            {
                instanciaExistente.Status = "Finalizado";
                await context.SaveChangesAsync();
            }
        }

        // 2. Aloca porta livre no host
        int portaHost = ObterPortaLivre();

        // 3. Monta comando Docker
        string sanitizedUser = Regex.Replace(usuarioId, "[^a-zA-Z0-9]", "").ToLower();
        if (sanitizedUser.Length > 8) sanitizedUser = sanitizedUser[..8];
        string nomeContainer = $"ava-lab-{lab.Id}-{sanitizedUser}-{portaHost}";

        // Remove container com mesmo nome caso tenha sobrado
        await ExecutarComandoAsync("docker", $"rm -f {nomeContainer}", 5000);

        string envArgs = "";
        if (!string.IsNullOrWhiteSpace(lab.ParametrosAmbiente))
        {
            var lines = lab.ParametrosAmbiente.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    envArgs += $" -e \"{trimmed}\"";
                }
            }
        }

        // Se houver Flag configurada, injeta como variável de ambiente padrão
        if (!string.IsNullOrWhiteSpace(lab.Flag))
        {
            envArgs += $" -e \"CHALLENGE_FLAG={lab.Flag}\"";
        }

        string imagem = lab.ImagemDocker;
        int portaContainer = lab.PortaPadraoContainer;
        string customCmd = "";

        if (!string.IsNullOrWhiteSpace(lab.ComandoCustomizado))
        {
            customCmd = $" {lab.ComandoCustomizado}";
            if (imagem.Contains("ttyd", StringComparison.OrdinalIgnoreCase))
            {
                portaContainer = 7681;
            }
        }
        else if (imagem.Equals("alpine", StringComparison.OrdinalIgnoreCase) ||
                 imagem.Equals("alpine:latest", StringComparison.OrdinalIgnoreCase) ||
                 imagem.Contains("ttyd", StringComparison.OrdinalIgnoreCase))
        {
            imagem = "tsl0922/ttyd:alpine";
            portaContainer = 7681;
            var flagStr = lab.Flag ?? "DESEC{terminal_linux_master}";
            
            // Cenário ofensivo realista com usuário sem privilégios e binário SUID vulnerável
            customCmd = $" sh -c \"adduser -D -s /bin/bash pentester && echo '{flagStr}' > /root/flag.txt && chmod 400 /root/flag.txt && cp /bin/busybox /usr/local/bin/backup-tool && chmod 4755 /usr/local/bin/backup-tool && echo -e '=== DESAFIO OFENSIVO (PRIVILEGE ESCALATION) ===\\nVoce acessou como pentester (baixo privilegio).\\nA flag esta protegida em /root/flag.txt.\\nEncontre o binario com bit SUID ativado no sistema para obter root e ler a flag!' > /home/pentester/README.txt && su - pentester -c 'ttyd -W -p 7681 bash'\"";
        }

        string args = $"run -d --name {nomeContainer} -p {portaHost}:{portaContainer}{envArgs} {imagem}{customCmd}";
        _logger.LogInformation("Executando Docker: docker {Args}", args);

        var (exitCode, stdOut, stdErr) = await ExecutarComandoAsync("docker", args, 60000); // 60s para baixar imagem se necessário

        if (exitCode != 0)
        {
            _logger.LogError("Erro ao iniciar container: {StdErr}", stdErr);
            return new ResultadoDocker
            {
                Sucesso = false,
                Mensagem = $"Falha ao subir imagem '{imagem}': {stdErr}"
            };
        }

        string containerId = stdOut.Trim();
        if (containerId.Length > 12) containerId = containerId[..12];

        // Aguarda 1 segundo e valida se o container continuou em execução
        await Task.Delay(1000);
        if (!await IsContainerExecutandoAsync(containerId))
        {
            var (_, logs, _) = await ExecutarComandoAsync("docker", $"logs {containerId}", 5000);
            return new ResultadoDocker
            {
                Sucesso = false,
                Mensagem = $"O container foi iniciado mas encerrou logo em seguida. Detalhes: {logs}"
            };
        }

        // Se for laboratório de Cloud Pentest / Floci, provisiona o cenário emulado na AWS local
        if (lab.ImagemDocker.Contains("floci", StringComparison.OrdinalIgnoreCase))
        {
            await ProvisionarCenarioCloudAsync(portaHost, lab);
        }

        // 4. Salva nova instância no banco
        var novaInstancia = new InstanciaLaboratorio
        {
            LaboratorioId = lab.Id,
            UsuarioId = usuarioId,
            ContainerId = containerId,
            PortaHost = portaHost,
            Status = "Executando",
            IniciadoEm = DateTime.UtcNow,
            ExpiraEm = DateTime.UtcNow.AddMinutes(lab.TempoLimiteMinutos > 0 ? lab.TempoLimiteMinutos : 60),
            Resolvido = false
        };

        context.InstanciasLaboratorios.Add(novaInstancia);
        await context.SaveChangesAsync();

        return new ResultadoDocker
        {
            Sucesso = true,
            ContainerId = containerId,
            PortaHost = portaHost,
            Mensagem = "Laboratório iniciado com sucesso!"
        };
    }

    public async Task<bool> PararLaboratorioAsync(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId)) return false;

        try
        {
            var (stopExit, _, _) = await ExecutarComandoAsync("docker", $"stop -t 3 {containerId}", 8000);
            var (rmExit, _, _) = await ExecutarComandoAsync("docker", $"rm -f {containerId}", 8000);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AvaContext>();

            var instancias = await context.InstanciasLaboratorios
                .Where(i => i.ContainerId == containerId && i.Status == "Executando")
                .ToListAsync();

            foreach (var inst in instancias)
            {
                inst.Status = "Finalizado";
            }
            await context.SaveChangesAsync();

            return stopExit == 0 || rmExit == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parar container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<bool> IsContainerExecutandoAsync(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId)) return false;

        try
        {
            var (exitCode, stdout, _) = await ExecutarComandoAsync("docker", $"inspect -f \"{{{{.State.Running}}}}\" {containerId}", 4000);
            return exitCode == 0 && stdout.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public async Task LimparInstanciasExpiradasAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AvaContext>();

            var agora = DateTime.UtcNow;
            var expiradas = await context.InstanciasLaboratorios
                .Where(i => i.Status == "Executando" && i.ExpiraEm < agora)
                .ToListAsync();

            foreach (var inst in expiradas)
            {
                _logger.LogInformation("Limpando container expirado {ContainerId} do lab {LabId}", inst.ContainerId, inst.LaboratorioId);
                await ExecutarComandoAsync("docker", $"rm -f {inst.ContainerId}", 6000);
                inst.Status = "Expirado";
            }

            if (expiradas.Any())
            {
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na limpeza periódica de containers Docker");
        }
    }

    private static int ObterPortaLivre(int portaInicial = 9000, int portaFinal = 9999)
    {
        try
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
            var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();

            var portasOcupadas = tcpListeners.Select(l => l.Port)
                .Concat(tcpConnections.Select(c => c.LocalEndPoint.Port))
                .ToHashSet();

            for (int p = portaInicial; p <= portaFinal; p++)
            {
                if (!portasOcupadas.Contains(p))
                {
                    return p;
                }
            }
        }
        catch
        {
            // Fallback caso permissões de rede do Windows restrinjam a leitura
        }

        return new Random().Next(10000, 20000);
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> ExecutarComandoAsync(string comando, string argumentos, int timeoutMs = 30000)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = comando,
            Arguments = argumentos,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            process.Start();
            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            using var cts = new CancellationTokenSource(timeoutMs);
            await process.WaitForExitAsync(cts.Token);

            var stdOut = await stdOutTask;
            var stdErr = await stdErrTask;
            return (process.ExitCode, stdOut.Trim(), stdErr.Trim());
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); } catch { }
            return (-1, string.Empty, "Tempo limite esgotado para o comando Docker.");
        }
        catch (Exception ex)
        {
            return (-1, string.Empty, ex.Message);
        }
    }

    private static void TentarIniciarDockerDesktop()
    {
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var possiveisCaminhos = new[]
            {
                Path.Combine(localAppData, "Programs", "DockerDesktop", "Docker Desktop.exe"),
                Path.Combine(userProfile, "AppData", "Local", "Programs", "DockerDesktop", "Docker Desktop.exe"),
                @"C:\Program Files\Docker\Docker\Docker Desktop.exe"
            };

            foreach (var caminho in possiveisCaminhos)
            {
                if (File.Exists(caminho))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = caminho,
                        UseShellExecute = true
                    });
                    break;
                }
            }
        }
        catch
        {
            // Silencioso se não conseguir disparar o processo
        }
    }

    private async Task ProvisionarCenarioCloudAsync(int portaHost, Laboratorio lab)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            string endpoint = $"http://localhost:{portaHost}";

            // Aguarda o Floci responder ao health check
            for (int i = 0; i < 12; i++)
            {
                try
                {
                    var health = await httpClient.GetAsync($"{endpoint}/_floci/health");
                    if (health.IsSuccessStatusCode) break;
                }
                catch { }
                await Task.Delay(500);
            }

            // 1. Cria o bucket S3 corporativo simulado
            string bucketName = "nexus-corp-backup";
            await httpClient.PutAsync($"{endpoint}/{bucketName}", null);

            // 2. Envia o arquivo de credenciais contendo a Flag de intrusão
            string flag = lab.Flag ?? "DESEC{s3_bucket_public_leak_captured}";
            var payloadJson = $"{{\"database_host\":\"10.0.0.5\",\"admin_user\":\"nexus_admin\",\"admin_pass\":\"Sup3rS3cr3t#2026\",\"challenge_flag\":\"{flag}\",\"nota\":\"Backup confidencial de chaves da NexusCorp. RESTRITO AO TIME DE SEGURANCA.\"}}";
            var content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");
            await httpClient.PutAsync($"{endpoint}/{bucketName}/credentials_dump.json", content);

            // 3. Envia arquivo de metadados para enumeração
            var readmeContent = new StringContent("=== NEXUS CORP BACKUP SYSTEM ===\nBackup gerado automaticamente em 06/09/2026.\nArquivos de configuracao e chaves de acesso confidenciais.", System.Text.Encoding.UTF8, "text/plain");
            await httpClient.PutAsync($"{endpoint}/{bucketName}/README.txt", readmeContent);

            _logger.LogInformation("Cenário de Cloud Pentest (S3 Bucket) provisionado com sucesso no Floci na porta {Porta}", portaHost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao provisionar cenário no Floci");
        }
    }
}

