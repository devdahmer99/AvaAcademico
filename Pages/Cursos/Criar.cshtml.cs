using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AvaAcademico.Pages.Cursos;

[Authorize(Roles = "Administrador")]
public class CriarModel : PageModel
{
    private readonly AvaContext _context;

    public CriarModel(AvaContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Curso Curso { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        _context.Cursos.Add(Curso);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Index");
    }
}