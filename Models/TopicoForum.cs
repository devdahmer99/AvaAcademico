using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class TopicoForum
{
    public int Id { get; set; }

    [Required]
    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    [Required(ErrorMessage = "O título do tópico é obrigatório.")]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A mensagem é obrigatória.")]
    [StringLength(4000)]
    public string Conteudo { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public bool Resolvido { get; set; } = false;

    public List<RespostaForum> Respostas { get; set; } = new();
}
