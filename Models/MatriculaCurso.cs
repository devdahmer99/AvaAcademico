using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class MatriculaCurso
{
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    [Required]
    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    public DateTime DataMatricula { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Ativa"; // "Ativa", "Concluida", "Cancelada"

    public DateTime? DataConclusao { get; set; }
}
