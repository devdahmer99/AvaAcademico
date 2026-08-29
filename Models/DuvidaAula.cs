using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class DuvidaAula
{
    public int Id { get; set; }

    [Required]
    public int AulaId { get; set; }
    public Aula Aula { get; set; } = null!;

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    [Required(ErrorMessage = "A pergunta é obrigatória.")]
    [StringLength(2000)]
    public string Pergunta { get; set; } = string.Empty;

    public int? MinutoVideo { get; set; } // Segundo/minuto da dúvida no vídeo

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public bool Respondida { get; set; } = false;

    public List<RespostaDuvidaAula> Respostas { get; set; } = new();
}
