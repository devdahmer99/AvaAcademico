using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class Certificado
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string CodigoAutenticidade { get; set; } = string.Empty;

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    [Required]
    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    public DateTime DataEmissao { get; set; } = DateTime.UtcNow;

    public int CargaHorariaHoras { get; set; } = 40;
}
