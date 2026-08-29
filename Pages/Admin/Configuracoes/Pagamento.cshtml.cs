using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Admin.Configuracoes;

[Authorize(Roles = "Administrador")]
public class PagamentoModel : PageModel
{
    private readonly AvaContext _context;

    public PagamentoModel(AvaContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ConfiguracaoPagamento Configuracao { get; set; } = new();

    public async Task OnGetAsync()
    {
        var config = await _context.ConfiguracoesPagamento.FirstOrDefaultAsync();
        if (config != null)
        {
            Configuracao = config;
        }
        else
        {
            Configuracao = new ConfiguracaoPagamento
            {
                ProvedorPadrao = "PixDireto",
                ChavePix = "contato@ejlacademy.com.br",
                TipoChavePix = "Email",
                NomeBeneficiarioPix = "EJL Academy",
                CidadeBeneficiarioPix = "Sao Paulo",
                ModoSandbox = true
            };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var configExistente = await _context.ConfiguracoesPagamento.FirstOrDefaultAsync();
        if (configExistente != null)
        {
            configExistente.ProvedorPadrao = Configuracao.ProvedorPadrao;
            configExistente.ChavePix = Configuracao.ChavePix;
            configExistente.TipoChavePix = Configuracao.TipoChavePix;
            configExistente.NomeBeneficiarioPix = Configuracao.NomeBeneficiarioPix;
            configExistente.CidadeBeneficiarioPix = Configuracao.CidadeBeneficiarioPix;
            configExistente.ChavePublica = Configuracao.ChavePublica;
            configExistente.ChavePrivadaToken = Configuracao.ChavePrivadaToken;
            configExistente.ModoSandbox = Configuracao.ModoSandbox;
            configExistente.InstrucoesPagamento = Configuracao.InstrucoesPagamento;
            configExistente.AtualizadoEm = DateTime.UtcNow;
        }
        else
        {
            Configuracao.AtualizadoEm = DateTime.UtcNow;
            _context.ConfiguracoesPagamento.Add(Configuracao);
        }

        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Configurações de meio de pagamento salvas com sucesso!";
        return RedirectToPage();
    }
}
