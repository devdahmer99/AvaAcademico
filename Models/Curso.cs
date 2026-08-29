using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class Curso
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Descricao { get; set; } = string.Empty;

    public int CargaHorariaHoras { get; set; } = 40;

    public string? Categoria { get; set; } = "Tecnologia";

    public string? Icone { get; set; } = "bi-mortarboard";

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Relacionamentos
    public List<Modulo> Modulos { get; set; } = new();
    public List<MatriculaCurso> Matriculas { get; set; } = new();
    public List<TopicoForum> TopicosForum { get; set; } = new();
    public List<Certificado> Certificados { get; set; } = new();
}