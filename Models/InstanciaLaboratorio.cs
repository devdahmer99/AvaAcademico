using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvaAcademico.Models;

public class InstanciaLaboratorio
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int LaboratorioId { get; set; }
    [ForeignKey("LaboratorioId")]
    public Laboratorio Laboratorio { get; set; } = null!;

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    [ForeignKey("UsuarioId")]
    public ApplicationUser Usuario { get; set; } = null!;

    [MaxLength(100)]
    public string ContainerId { get; set; } = string.Empty;

    public int PortaHost { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Executando"; // "Executando", "Finalizado", "Expirado", "Erro"

    public DateTime IniciadoEm { get; set; } = DateTime.UtcNow;

    public DateTime ExpiraEm { get; set; }

    public bool Resolvido { get; set; }

    public DateTime? ResolvidoEm { get; set; }

    [MaxLength(150)]
    public string? FlagSubmetida { get; set; }

    [NotMapped]
    public bool EstaAtivo => Status == "Executando" && DateTime.UtcNow < ExpiraEm;

    [NotMapped]
    public int SegundosRestantes => EstaAtivo ? Math.Max(0, (int)(ExpiraEm - DateTime.UtcNow).TotalSeconds) : 0;
}

