using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Certificados;

[Authorize]
public class EmitirModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public EmitirModel(AvaContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Certificado Certificado { get; set; } = null!;
    public ApplicationUser Aluno { get; set; } = null!;
    public Curso Curso { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int cursoId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        Aluno = user;

        Curso = await _context.Cursos
            .Include(c => c.Modulos)
            .ThenInclude(m => m.Aulas)
            .FirstOrDefaultAsync(c => c.Id == cursoId) ?? null!;

        if (Curso == null) return NotFound("Curso não encontrado.");

        var todasAulasIds = Curso.Modulos.SelectMany(m => m.Aulas).Select(a => a.Id).ToList();
        if (!todasAulasIds.Any())
        {
            return BadRequest("Este curso ainda não possui aulas cadastradas.");
        }

        // Verifica se o aluno concluiu todas as aulas
        var aulasConcluidasCount = await _context.ProgressosAulas
            .Where(p => p.UsuarioId == user.Id && todasAulasIds.Contains(p.AulaId))
            .CountAsync();

        // Se for admin ou se tiver concluído todas as aulas, emite o certificado
        bool isConcluido = aulasConcluidasCount >= todasAulasIds.Count;
        bool isAdmin = User.IsInRole("Administrador");

        if (!isConcluido && !isAdmin)
        {
            TempData["MensagemErro"] = "Você precisa concluir 100% das aulas deste curso para desbloquear seu certificado.";
            return RedirectToPage("/Cursos/Detalhes", new { id = cursoId });
        }

        // Busca ou gera o certificado
        var cert = await _context.Certificados
            .FirstOrDefaultAsync(c => c.UsuarioId == user.Id && c.CursoId == cursoId);

        if (cert == null)
        {
            var codigoUnico = $"EJL-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            cert = new Certificado
            {
                UsuarioId = user.Id,
                CursoId = cursoId,
                CodigoAutenticidade = codigoUnico,
                CargaHorariaHoras = Curso.CargaHorariaHoras > 0 ? Curso.CargaHorariaHoras : 40,
                DataEmissao = DateTime.UtcNow
            };

            _context.Certificados.Add(cert);
            await _context.SaveChangesAsync();
        }

        Certificado = cert;
        return Page();
    }
}
