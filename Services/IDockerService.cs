using AvaAcademico.Models;

namespace AvaAcademico.Services;

public class ResultadoDocker
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string? ContainerId { get; set; }
    public int PortaHost { get; set; }
}

public interface IDockerService
{
    Task<bool> IsDockerDisponivelAsync();
    Task<ResultadoDocker> IniciarLaboratorioAsync(Laboratorio lab, string usuarioId);
    Task<bool> PararLaboratorioAsync(string containerId);
    Task<bool> IsContainerExecutandoAsync(string containerId);
    Task LimparInstanciasExpiradasAsync();
}

