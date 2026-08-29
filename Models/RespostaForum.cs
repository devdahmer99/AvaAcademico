using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class RespostaForum
{
    public int Id { get; set; }

    [Required]
    public int TopicoForumId { get; set; }
    public TopicoForum TopicoForum { get; set; } = null!;

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    [Required(ErrorMessage = "A resposta não pode ficar em branco.")]
    [StringLength(4000)]
    public string Conteudo { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
