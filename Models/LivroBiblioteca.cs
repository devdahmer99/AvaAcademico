using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class LivroBiblioteca
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "O autor é obrigatório.")]
    [StringLength(150)]
    public string Autor { get; set; } = string.Empty;

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    [StringLength(100)]
    public string Categoria { get; set; } = "Geral"; // "Tecnologia", "Cibersegurança", "Programação", "Redes", "Geral"

    [StringLength(2000)]
    public string? Descricao { get; set; }

    [Required]
    [StringLength(255)]
    public string NomeArquivoPdf { get; set; } = string.Empty;

    [StringLength(255)]
    public string? NomeArquivoCapa { get; set; }

    public int? NumeroPaginas { get; set; }

    public long TamanhoBytes { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
