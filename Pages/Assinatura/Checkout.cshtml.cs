using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AvaAcademico.Pages.Assinatura;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutModel(AvaContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Plano Plano { get; set; } = null!;
    public ConfiguracaoPagamento ConfiguracaoPagamento { get; set; } = null!;
    public string PixCopiaECola { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;

    [BindProperty]
    public int PlanoId { get; set; }

    [BindProperty]
    public string MetodoEscolhido { get; set; } = "Pix"; // "Pix" ou "Cartao"

    [BindProperty]
    public CartaoInputModel InputCartao { get; set; } = new();

    public class CartaoInputModel
    {
        public string? NumeroCartao { get; set; }
        public string? NomeTitular { get; set; }
        public string? Validade { get; set; }
        public string? CVV { get; set; }
        public int Parcelas { get; set; } = 1;
    }

    public async Task<IActionResult> OnGetAsync(int planoId)
    {
        var plano = await _context.Planos.FindAsync(planoId);
        if (plano == null || !plano.Ativo)
        {
            return RedirectToPage("/Planos/Index");
        }

        Plano = plano;
        PlanoId = plano.Id;

        ConfiguracaoPagamento = await _context.ConfiguracoesPagamento.FirstOrDefaultAsync() 
            ?? new ConfiguracaoPagamento();

        GerarDadosPix(plano);

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmarPagamentoAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        var plano = await _context.Planos.FindAsync(PlanoId);
        if (plano == null || !plano.Ativo)
        {
            return RedirectToPage("/Planos/Index");
        }

        // 1. Cria a assinatura ativa
        var codigoTransacao = $"TX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        var dataFim = DateTime.UtcNow.AddMonths(plano.IntervaloMeses);

        var novaAssinatura = new Models.Assinatura
        {
            UsuarioId = user.Id,
            PlanoId = plano.Id,
            Status = "Ativa",
            MetodoPagamento = MetodoEscolhido,
            ValorPago = plano.Preco,
            DataInicio = DateTime.UtcNow,
            DataFim = dataFim,
            CodigoTransacao = codigoTransacao
        };

        _context.Assinaturas.Add(novaAssinatura);

        // 2. Matricula o aluno em todos os cursos existentes
        var cursosIds = await _context.Cursos.Select(c => c.Id).ToListAsync();
        var matriculasExistentes = await _context.Matriculas
            .Where(m => m.UsuarioId == user.Id)
            .Select(m => m.CursoId)
            .ToListAsync();

        foreach (var cursoId in cursosIds)
        {
            if (!matriculasExistentes.Contains(cursoId))
            {
                _context.Matriculas.Add(new MatriculaCurso
                {
                    UsuarioId = user.Id,
                    CursoId = cursoId,
                    DataMatricula = DateTime.UtcNow,
                    Status = "Ativa"
                });
            }
        }

        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = $"Parabéns! Sua assinatura do {plano.Nome} foi confirmada com sucesso. Você já possui acesso a todos os cursos!";
        return RedirectToPage("/Perfil/Index");
    }

    private void GerarDadosPix(Plano plano)
    {
        var chave = ConfiguracaoPagamento?.ChavePix ?? "contato@ejlacademy.com.br";
        var beneficiario = ConfiguracaoPagamento?.NomeBeneficiarioPix ?? "EJL Academy";
        var cidade = ConfiguracaoPagamento?.CidadeBeneficiarioPix ?? "Sao Paulo";

        // Cria o código Pix simulado padrão BR Code
        PixCopiaECola = $"00020126580014BR.GOV.BCB.PIX0114{chave}5204000053039865405{plano.Preco:0.00}5802BR5915{beneficiario}6009{cidade}62070503***6304ABCD";

        // URL para renderizar o QR code dinâmico via API pública segura
        QrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={Uri.EscapeDataString(PixCopiaECola)}";
    }
}
