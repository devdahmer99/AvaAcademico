using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Cursos;

[Authorize]
public class DetalhesModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetalhesModel(AvaContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Curso Curso { get; set; } = null!;
    public bool IsMatriculado { get; set; }
    public List<int> AulasConcluidasIds { get; set; } = new();
    public int PercentualProgresso { get; set; }
    public bool Concluido100PorCento { get; set; }
    public Certificado? CertificadoExistente { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        Curso = await _context.Cursos
            .Include(c => c.Modulos)
            .ThenInclude(m => m.Aulas)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (Curso == null) return NotFound();

        // 1. Verifica matrícula
        IsMatriculado = await _context.Matriculas
            .AnyAsync(m => m.UsuarioId == user.Id && m.CursoId == id && m.Status == "Ativa");

        if (User.IsInRole("Administrador"))
        {
            IsMatriculado = true; // Admin sempre tem acesso
        }

        // 2. Progresso individual
        var todasAulasIds = Curso.Modulos.SelectMany(m => m.Aulas).Select(a => a.Id).ToList();
        if (todasAulasIds.Any())
        {
            AulasConcluidasIds = await _context.ProgressosAulas
                .Where(p => p.UsuarioId == user.Id && todasAulasIds.Contains(p.AulaId))
                .Select(p => p.AulaId)
                .ToListAsync();

            PercentualProgresso = (int)Math.Round((double)AulasConcluidasIds.Count / todasAulasIds.Count * 100);
            Concluido100PorCento = AulasConcluidasIds.Count >= todasAulasIds.Count;
        }

        CertificadoExistente = await _context.Certificados
            .FirstOrDefaultAsync(c => c.UsuarioId == user.Id && c.CursoId == id);

        return Page();
    }

    public async Task<IActionResult> OnPostMatricularAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        var jaMatriculado = await _context.Matriculas
            .AnyAsync(m => m.UsuarioId == user.Id && m.CursoId == id);

        if (!jaMatriculado)
        {
            _context.Matriculas.Add(new MatriculaCurso
            {
                UsuarioId = user.Id,
                CursoId = id,
                DataMatricula = DateTime.UtcNow,
                Status = "Ativa"
            });
            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = "Matrícula realizada com sucesso! Bons estudos!";
        }

        return RedirectToPage(new { id = id });
    }
}