using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Biblioteca;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AvaContext _context;

    public IndexModel(AvaContext context)
    {
        _context = context;
    }

    public List<LivroBiblioteca> Livros { get; set; } = new();
    public List<string> Categorias { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Busca { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Categoria { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.LivrosBiblioteca.AsQueryable();

        if (!string.IsNullOrWhiteSpace(Busca))
        {
            var termo = Busca.Trim().ToLower();
            query = query.Where(l => l.Titulo.ToLower().Contains(termo) 
                                  || l.Autor.ToLower().Contains(termo) 
                                  || (l.Descricao != null && l.Descricao.ToLower().Contains(termo)));
        }

        if (!string.IsNullOrWhiteSpace(Categoria) && Categoria != "Todas")
        {
            query = query.Where(l => l.Categoria == Categoria);
        }

        Livros = await query.OrderByDescending(l => l.CriadoEm).ToListAsync();

        Categorias = await _context.LivrosBiblioteca
            .Select(l => l.Categoria)
            .Distinct()
            .ToListAsync();
    }
}
