using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Modulos;

[Authorize(Roles = "Administrador")]
public class CriarModel : PageModel
{
    private readonly AvaContext _context;

    public CriarModel(AvaContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Modulo Modulo { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? CursoId { get; set; }

    public List<Curso> CursosDisponiveis { get; set; } = new();

    public async Task OnGetAsync()
    {
        CursosDisponiveis = await _context.Cursos.OrderBy(c => c.Titulo).ToListAsync();

        if (CursoId.HasValue && CursoId.Value > 0)
        {
            var maximaOrdem = await _context.Modulos
                .Where(m => m.CursoId == CursoId.Value)
                .MaxAsync(m => (int?)m.Ordem) ?? 0;

            Modulo.CursoId = CursoId.Value;
            Modulo.Ordem = maximaOrdem + 1;
        }
        else if (CursosDisponiveis.Any() && Modulo.Ordem == 0)
        {
            Modulo.Ordem = 1;
        }
    }

    public async Task<IActionResult> OnGetUltimaOrdemAsync(int cursoId)
    {
        var maximaOrdem = await _context.Modulos
            .Where(m => m.CursoId == cursoId)
            .MaxAsync(m => (int?)m.Ordem) ?? 0;

        return new JsonResult(new { proximaOrdem = maximaOrdem + 1 });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Modulo.Curso");
        ModelState.Remove("Modulo.Aulas");

        if (Modulo.CursoId <= 0)
        {
            ModelState.AddModelError("Modulo.CursoId", "Selecione um curso válido.");
        }

        if (!ModelState.IsValid)
        {
            CursosDisponiveis = await _context.Cursos.OrderBy(c => c.Titulo).ToListAsync();
            return Page();
        }

        _context.Modulos.Add(Modulo);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = $"Módulo \"{Modulo.Titulo}\" cadastrado com sucesso!";
        return RedirectToPage("/Modulos/Criar", new { cursoId = Modulo.CursoId });
    }
}