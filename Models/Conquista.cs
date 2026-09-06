using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvaAcademico.Models;

public class Conquista
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Descricao { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Icone { get; set; } = "bi-award-fill";

    [MaxLength(30)]
    public string CorDestaque { get; set; } = "#3b82f6"; // Hexadecimal da cor

    [MaxLength(50)]
    public string Categoria { get; set; } = "Modulo"; // "Modulo", "Laboratorio", "Curso", "Geral"

    public int XpRecompensa { get; set; } = 100;

    public int? ModuloId { get; set; }
    [ForeignKey("ModuloId")]
    public Modulo? Modulo { get; set; }

    public int? CursoId { get; set; }
    [ForeignKey("CursoId")]
    public Curso? Curso { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<ConquistaUsuario> Usuarios { get; set; } = new List<ConquistaUsuario>();
}

