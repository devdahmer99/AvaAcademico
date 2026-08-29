using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvaAcademico.Models;

public class Assinatura
{
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    [Required]
    public int PlanoId { get; set; }
    public Plano Plano { get; set; } = null!;

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Ativa"; // "Ativa", "Pendente", "Cancelada", "Expirada"

    public DateTime DataInicio { get; set; } = DateTime.UtcNow;
    public DateTime? DataFim { get; set; }

    [StringLength(50)]
    public string MetodoPagamento { get; set; } = "Pix"; // "Pix", "Cartao", "Boleto", "Gratuito"

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorPago { get; set; }

    [StringLength(100)]
    public string? CodigoTransacao { get; set; }
}
