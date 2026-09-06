using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvaAcademico.Models;

public class AnotacaoAula
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AulaId { get; set; }

    [ForeignKey("AulaId")]
    public Aula? Aula { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    [ForeignKey("UsuarioId")]
    public ApplicationUser? Usuario { get; set; }

    [MaxLength(10000)]
    public string Conteudo { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

