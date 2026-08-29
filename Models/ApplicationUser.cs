using Microsoft.AspNetCore.Identity;

namespace AvaAcademico.Models;

public class ApplicationUser : IdentityUser
{
    public string? NomeCompleto { get; set; }
    public string? GoogleId { get; set; }
    public string? FotoUrl { get; set; }
    public string? Bio { get; set; }
    public string? Telefone { get; set; }
    public DateTime? CriadoEm { get; set; } = DateTime.UtcNow;

    // Relacionamentos
    public List<Assinatura> Assinaturas { get; set; } = new();
    public List<MatriculaCurso> Matriculas { get; set; } = new();
    public List<ProgressoAula> AulasConcluidas { get; set; } = new();
    public List<Certificado> Certificados { get; set; } = new();
}
