using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(AvaContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<Curso> Cursos { get; set; } = new();
    public AvaAcademico.Models.Assinatura? AssinaturaAtiva { get; set; }
    public List<int> CursosMatriculadosIds { get; set; } = new();

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        Cursos = await _context.Cursos
            .Include(c => c.Modulos)
            .ThenInclude(m => m.Aulas)
            .ToListAsync();

        if (user != null)
        {
            AssinaturaAtiva = await _context.Assinaturas
                .Include(a => a.Plano)
                .Where(a => a.UsuarioId == user.Id && a.Status == "Ativa")
                .OrderByDescending(a => a.DataInicio)
                .FirstOrDefaultAsync();

            CursosMatriculadosIds = await _context.Matriculas
                .Where(m => m.UsuarioId == user.Id && m.Status == "Ativa")
                .Select(m => m.CursoId)
                .ToListAsync();
        }
    }
}