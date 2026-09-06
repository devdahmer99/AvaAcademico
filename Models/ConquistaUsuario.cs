using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvaAcademico.Models;

public class ConquistaUsuario
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    [ForeignKey("UsuarioId")]
    public ApplicationUser Usuario { get; set; } = null!;

    [Required]
    public int ConquistaId { get; set; }
    [ForeignKey("ConquistaId")]
    public Conquista Conquista { get; set; } = null!;

    public DateTime DesbloqueadoEm { get; set; } = DateTime.UtcNow;
}

