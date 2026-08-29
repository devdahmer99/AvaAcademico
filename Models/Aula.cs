using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class Aula
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O título da aula é obrigatório.")]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    public string? NomeArquivoVideo { get; set; }

    public int Ordem { get; set; } = 1;
    public bool Concluida { get; set; }

    public int DuracaoMinutos { get; set; } = 15;

    public int ModuloId { get; set; }
    public Modulo? Modulo { get; set; }

    public List<DuvidaAula> Duvidas { get; set; } = new();
    public List<ProgressoAula> Progressos { get; set; } = new();
}