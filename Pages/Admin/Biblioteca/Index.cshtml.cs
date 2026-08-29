using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Admin.Biblioteca;

[Authorize(Roles = "Administrador")]
public class IndexModel : PageModel
{
    private readonly AvaContext _context;

    public IndexModel(AvaContext context)
    {
        _context = context;
    }

    public List<LivroBiblioteca> Livros { get; set; } = new();

    public async Task OnGetAsync()
    {
        Livros = await _context.LivrosBiblioteca
            .OrderByDescending(l => l.CriadoEm)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var livro = await _context.LivrosBiblioteca.FindAsync(id);
        if (livro != null)
        {
            _context.LivrosBiblioteca.Remove(livro);
            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = $"Livro \"{livro.Titulo}\" removido com sucesso!";
        }
        return RedirectToPage();
    }
}
