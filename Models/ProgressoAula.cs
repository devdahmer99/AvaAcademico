using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class ProgressoAula
{
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    [Required]
    public int AulaId { get; set; }
    public Aula Aula { get; set; } = null!;

    public DateTime ConcluidaEm { get; set; } = DateTime.UtcNow;
}
