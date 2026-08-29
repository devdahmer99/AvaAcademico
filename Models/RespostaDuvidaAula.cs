using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class RespostaDuvidaAula
{
    public int Id { get; set; }

    [Required]
    public int DuvidaAulaId { get; set; }
    public DuvidaAula DuvidaAula { get; set; } = null!;

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    [Required(ErrorMessage = "A resposta é obrigatória.")]
    [StringLength(2000)]
    public string Resposta { get; set; } = string.Empty;

    public bool IsInstrutorOuAdmin { get; set; } = false;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
