using AvaAcademico.Models;

namespace AvaAcademico.Services;

public class ResultadoGamificacao
{
    public bool Sucesso { get; set; }
    public int XpGanho { get; set; }
    public int XpTotal { get; set; }
    public int Nivel { get; set; }
    public string TituloNivel { get; set; } = string.Empty;
    public bool SubiuDeNivel { get; set; }
    public Conquista? ConquistaDesbloqueada { get; set; }
    public bool ModuloConcluido { get; set; }
    public string? NomeModuloConcluido { get; set; }
}

public class ConquistaDetalhadaDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    public string CorDestaque { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int XpRecompensa { get; set; }
    public bool Desbloqueada { get; set; }
    public DateTime? DesbloqueadoEm { get; set; }
}

public class PerfilGamificacaoDto
{
    public int PontosXp { get; set; }
    public int Nivel { get; set; }
    public string TituloNivel { get; set; } = string.Empty;
    public int XpMinimoNivelAtual { get; set; }
    public int XpProximoNivel { get; set; }
    public int PercentualProgressoNivel { get; set; }
    public int TotalConquistasDesbloqueadas { get; set; }
    public int TotalConquistasDisponiveis { get; set; }
    public List<ConquistaDetalhadaDto> Conquistas { get; set; } = new();
}

public interface IGamificacaoService
{
    Task<ResultadoGamificacao> ProcessarConclusaoAulaAsync(string usuarioId, int aulaId);
    Task<ResultadoGamificacao> CreditarXpAsync(string usuarioId, int xp, string motivo);
    Task<ResultadoGamificacao> DesbloquearConquistaAsync(string usuarioId, string codigoConquista);
    Task<PerfilGamificacaoDto> ObterPerfilGamificacaoAsync(string usuarioId);
    Task GerarConquistasPadraoModulosAsync(int cursoId);
}

