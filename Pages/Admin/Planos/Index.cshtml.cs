using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Admin.Planos;

[Authorize(Roles = "Administrador")]
public class IndexModel : PageModel
{
    private readonly AvaContext _context;

    public IndexModel(AvaContext context)
    {
        _context = context;
    }

    public List<PlanoComTotalAssinantesViewModel> Planos { get; set; } = new();

    public class PlanoComTotalAssinantesViewModel
    {
        public Plano Plano { get; set; } = null!;
        public int TotalAssinantesAtivos { get; set; }
    }

    public async Task OnGetAsync()
    {
        var planosList = await _context.Planos
            .Include(p => p.Assinaturas)
            .OrderBy(p => p.Preco)
            .ToListAsync();

        Planos = planosList.Select(p => new PlanoComTotalAssinantesViewModel
        {
            Plano = p,
            TotalAssinantesAtivos = p.Assinaturas.Count(a => a.Status == "Ativa")
        }).ToList();
    }

    public async Task<IActionResult> OnPostAlternarStatusAsync(int id)
    {
        var plano = await _context.Planos.FindAsync(id);
        if (plano != null)
        {
            plano.Ativo = !plano.Ativo;
            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = $"Status do plano \"{plano.Nome}\" alterado com sucesso!";
        }
        return RedirectToPage();
    }
}
