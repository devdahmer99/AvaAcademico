namespace AvaAcademico.Services;

public class LabCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LabCleanupBackgroundService> _logger;

    public LabCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<LabCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de limpeza de laboratórios iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dockerService = scope.ServiceProvider.GetRequiredService<IDockerService>();

                await dockerService.LimparInstanciasExpiradasAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar ciclo de limpeza de laboratórios.");
            }

            // Aguarda 2 minutos entre as checagens
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}

