using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Planos;

public class IndexModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(AvaContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<Plano> Planos { get; set; } = new();
    public AvaAcademico.Models.Assinatura? AssinaturaAtual { get; set; }

    public async Task OnGetAsync()
    {
        Planos = await _context.Planos
            .Where(p => p.Ativo)
            .OrderBy(p => p.Preco)
            .ToListAsync();

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                AssinaturaAtual = await _context.Assinaturas
                    .Include(a => a.Plano)
                    .Where(a => a.UsuarioId == user.Id && a.Status == "Ativa")
                    .OrderByDescending(a => a.DataInicio)
                    .FirstOrDefaultAsync();
            }
        }
    }
}
