using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Models;

public class ConfiguracaoPagamento
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string ProvedorPadrao { get; set; } = "PixDireto"; // "PixDireto", "MercadoPago", "Asaas", "Stripe"

    // Configurações PIX Direto
    [StringLength(100)]
    public string? ChavePix { get; set; }

    [StringLength(20)]
    public string? TipoChavePix { get; set; } = "CPF"; // CPF, CNPJ, Email, Telefone, Aleatoria

    [StringLength(150)]
    public string? NomeBeneficiarioPix { get; set; }

    [StringLength(50)]
    public string? CidadeBeneficiarioPix { get; set; }

    // Configurações Gateways (Mercado Pago / Asaas / Stripe)
    [StringLength(255)]
    public string? ChavePublica { get; set; }

    [StringLength(255)]
    public string? ChavePrivadaToken { get; set; }

    public bool ModoSandbox { get; set; } = true;

    [StringLength(1000)]
    public string? InstrucoesPagamento { get; set; }

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
