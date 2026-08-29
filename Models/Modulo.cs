using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class Modulo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O título do módulo é obrigatório.")]
    [StringLength(150)]
    public string Titulo { get; set; } = string.Empty;

    public int Ordem { get; set; } = 1;

    [Required(ErrorMessage = "Selecione o curso correspondente.")]
    public int CursoId { get; set; }

    public Curso? Curso { get; set; }

    public List<Aula> Aulas { get; set; } = new();
}