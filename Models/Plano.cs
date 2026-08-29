using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvaAcademico.Models;

public class Plano
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome do plano é obrigatório.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(500)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 99999.99, ErrorMessage = "Preço inválido.")]
    public decimal Preco { get; set; }

    [Required]
    [Range(1, 120, ErrorMessage = "Intervalo em meses deve ser entre 1 e 120.")]
    public int IntervaloMeses { get; set; } = 1; // 1 = Mensal, 3 = Trimestral, 12 = Anual

    [StringLength(2000)]
    public string? Beneficios { get; set; } // Itens separados por linha

    public bool Ativo { get; set; } = true;

    public bool Destaque { get; set; } = false;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Relacionamentos
    public List<Assinatura> Assinaturas { get; set; } = new();
}
