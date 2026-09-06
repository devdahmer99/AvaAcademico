using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvaAcademico.Models;

public class Laboratorio
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "O título do laboratório é obrigatório.")]
    [MaxLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a imagem Docker (ex: vulnerables/web-dvwa).")]
    [MaxLength(200)]
    public string ImagemDocker { get; set; } = string.Empty;

    public int PortaPadraoContainer { get; set; } = 80;

    [MaxLength(150)]
    public string? Flag { get; set; }

    public int Pontos { get; set; } = 100;

    public int TempoLimiteMinutos { get; set; } = 60;

    [MaxLength(1000)]
    public string? ParametrosAmbiente { get; set; }

    [MaxLength(50)]
    public string Dificuldade { get; set; } = "Intermediário";

    [MaxLength(150)]
    public string VetorAtaque { get; set; } = "Geral";

    [MaxLength(4000)]
    public string? CenarioBriefing { get; set; }

    [MaxLength(4000)]
    public string? Pistas { get; set; }

    [MaxLength(4000)]
    public string? Writeup { get; set; }

    [MaxLength(2000)]
    public string? ComandoCustomizado { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Vinculações opcionais: pode ser por aula ou por módulo
    public int? AulaId { get; set; }
    [ForeignKey("AulaId")]
    public Aula? Aula { get; set; }

    public int? ModuloId { get; set; }
    [ForeignKey("ModuloId")]
    public Modulo? Modulo { get; set; }

    public ICollection<InstanciaLaboratorio> Instancias { get; set; } = new List<InstanciaLaboratorio>();
}

