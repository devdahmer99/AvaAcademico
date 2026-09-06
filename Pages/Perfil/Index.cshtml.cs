using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace AvaAcademico.Pages.Perfil;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _env;
    private readonly AvaAcademico.Services.IGamificacaoService _gamificacaoService;

    public IndexModel(
        AvaContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment env,
        AvaAcademico.Services.IGamificacaoService gamificacaoService)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _env = env;
        _gamificacaoService = gamificacaoService;
    }

    public ApplicationUser Usuario { get; set; } = null!;
    public AvaAcademico.Models.Assinatura? AssinaturaAtiva { get; set; }
    public List<CursoComProgressoViewModel> CursosMatriculados { get; set; } = new();
    public List<Certificado> CertificadosObtidos { get; set; } = new();
    public AvaAcademico.Services.PerfilGamificacaoDto PerfilGamificacao { get; set; } = null!;

    public int TotalAulasConcluidas { get; set; }
    public int HorasEstimadasEstudo { get; set; }

    [BindProperty]
    public PerfilInputModel InputPerfil { get; set; } = new();

    [BindProperty]
    public SenhaInputModel InputSenha { get; set; } = new();

    [BindProperty]
    public IFormFile? FotoUpload { get; set; }

    public class PerfilInputModel
    {
        public string NomeCompleto { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public string? Bio { get; set; }
    }

    public class SenhaInputModel
    {
        public string SenhaAtual { get; set; } = string.Empty;
        public string NovaSenha { get; set; } = string.Empty;
        public string ConfirmarSenha { get; set; } = string.Empty;
    }

    public class CursoComProgressoViewModel
    {
        public int CursoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int TotalAulas { get; set; }
        public int AulasConcluidas { get; set; }
        public int PercentualProgresso { get; set; }
        public bool Concluido { get; set; }
        public int? CertificadoId { get; set; }
        public string? CodigoCertificado { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        Usuario = user;

        InputPerfil = new PerfilInputModel
        {
            NomeCompleto = user.NomeCompleto ?? user.UserName ?? string.Empty,
            Telefone = user.Telefone,
            Bio = user.Bio
        };

        await CarregarDadosAcademicosAsync(user.Id);

        return Page();
    }

    public async Task<IActionResult> OnPostAtualizarPerfilAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        Usuario = user;

        if (string.IsNullOrWhiteSpace(InputPerfil.NomeCompleto))
        {
            ModelState.AddModelError(string.Empty, "O nome completo é obrigatório.");
            await CarregarDadosAcademicosAsync(user.Id);
            return Page();
        }

        user.NomeCompleto = InputPerfil.NomeCompleto.Trim();
        user.Telefone = InputPerfil.Telefone?.Trim();
        user.Bio = InputPerfil.Bio?.Trim();

        // Processamento seguro de upload de foto
        if (FotoUpload != null && FotoUpload.Length > 0)
        {
            var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extensao = Path.GetExtension(FotoUpload.FileName).ToLowerInvariant();

            if (extensoesPermitidas.Contains(extensao))
            {
                var pastaAvatares = Path.Combine(_env.WebRootPath, "uploads", "avatares");
                Directory.CreateDirectory(pastaAvatares);

                var nomeFoto = $"avatar_{user.Id}_{Guid.NewGuid()}{extensao}";
                var caminhoFisico = Path.Combine(pastaAvatares, nomeFoto);

                using (var stream = new FileStream(caminhoFisico, FileMode.Create))
                {
                    await FotoUpload.CopyToAsync(stream);
                }

                user.FotoUrl = $"/uploads/avatares/{nomeFoto}";
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Formato de foto inválido. Use JPG, PNG ou WebP.");
                await CarregarDadosAcademicosAsync(user.Id);
                return Page();
            }
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (updateResult.Succeeded)
        {
            TempData["MensagemSucesso"] = "Perfil atualizado com sucesso!";
            return RedirectToPage();
        }

        foreach (var error in updateResult.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        await CarregarDadosAcademicosAsync(user.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostAlterarSenhaAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        Usuario = user;

        if (string.IsNullOrWhiteSpace(InputSenha.SenhaAtual) || string.IsNullOrWhiteSpace(InputSenha.NovaSenha))
        {
            ModelState.AddModelError(string.Empty, "Preencha todos os campos para alterar a senha.");
            await CarregarDadosAcademicosAsync(user.Id);
            return Page();
        }

        if (InputSenha.NovaSenha != InputSenha.ConfirmarSenha)
        {
            ModelState.AddModelError(string.Empty, "A confirmação de senha não coincide com a nova senha.");
            await CarregarDadosAcademicosAsync(user.Id);
            return Page();
        }

        var changePasswordResult = await _userManager.ChangePasswordAsync(user, InputSenha.SenhaAtual, InputSenha.NovaSenha);
        if (changePasswordResult.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["MensagemSucesso"] = "Sua senha foi alterada com sucesso!";
            return RedirectToPage();
        }

        foreach (var error in changePasswordResult.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        await CarregarDadosAcademicosAsync(user.Id);
        return Page();
    }

    private async Task CarregarDadosAcademicosAsync(string userId)
    {
        // 1. Assinatura ativa (se não possuir, ativa o Plano VIP Master Pro Vitalício)
        AssinaturaAtiva = await _context.Assinaturas
            .Include(a => a.Plano)
            .Where(a => a.UsuarioId == userId && a.Status == "Ativa")
            .OrderByDescending(a => a.DataInicio)
            .FirstOrDefaultAsync();

        if (AssinaturaAtiva == null)
        {
            var planoVip = await _context.Planos.FirstOrDefaultAsync(p => p.Destaque || p.Nome.Contains("VIP"))
                ?? await _context.Planos.OrderByDescending(p => p.Preco).FirstOrDefaultAsync();

            if (planoVip != null)
            {
                AssinaturaAtiva = new AvaAcademico.Models.Assinatura
                {
                    UsuarioId = userId,
                    PlanoId = planoVip.Id,
                    Plano = planoVip,
                    Status = "Ativa",
                    DataInicio = DateTime.UtcNow,
                    DataFim = DateTime.UtcNow.AddYears(10), // Vitalício
                    MetodoPagamento = "Acesso VIP Concedido",
                    ValorPago = planoVip.Preco,
                    CodigoTransacao = $"VIP-{Guid.NewGuid().ToString("N")[..8].ToUpper()}"
                };

                _context.Assinaturas.Add(AssinaturaAtiva);
                await _context.SaveChangesAsync();
            }
        }

        // 2. Aulas concluídas
        var aulasConcluidasIds = await _context.ProgressosAulas
            .Where(p => p.UsuarioId == userId)
            .Select(p => p.AulaId)
            .ToListAsync();

        TotalAulasConcluidas = aulasConcluidasIds.Count;
        HorasEstimadasEstudo = (int)Math.Ceiling(TotalAulasConcluidas * 0.35); // média de 20 min por aula

        // 3. Certificados
        CertificadosObtidos = await _context.Certificados
            .Include(c => c.Curso)
            .Where(c => c.UsuarioId == userId)
            .OrderByDescending(c => c.DataEmissao)
            .ToListAsync();

        // 4. Cursos matriculados ou disponíveis
        var matriculas = await _context.Matriculas
            .Include(m => m.Curso)
            .ThenInclude(c => c.Modulos)
            .ThenInclude(mod => mod.Aulas)
            .Where(m => m.UsuarioId == userId)
            .ToListAsync();

        CursosMatriculados = matriculas.Select(m =>
        {
            var todasAulasDoCurso = m.Curso.Modulos.SelectMany(mod => mod.Aulas).ToList();
            var totalAulas = todasAulasDoCurso.Count;
            var concluidas = todasAulasDoCurso.Count(a => aulasConcluidasIds.Contains(a.Id));
            var pct = totalAulas > 0 ? (int)Math.Round((double)concluidas / totalAulas * 100) : 0;
            var cert = CertificadosObtidos.FirstOrDefault(c => c.CursoId == m.CursoId);

            return new CursoComProgressoViewModel
            {
                CursoId = m.CursoId,
                Titulo = m.Curso.Titulo,
                Descricao = m.Curso.Descricao,
                TotalAulas = totalAulas,
                AulasConcluidas = concluidas,
                PercentualProgresso = pct,
                Concluido = totalAulas > 0 && concluidas >= totalAulas,
                CertificadoId = cert?.Id,
                CodigoCertificado = cert?.CodigoAutenticidade
            };
        }).ToList();

        // 5. Dados de Gamificação e Conquistas
        PerfilGamificacao = await _gamificacaoService.ObterPerfilGamificacaoAsync(userId);
    }
}
