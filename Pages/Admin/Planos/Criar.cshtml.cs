using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AvaAcademico.Pages.Admin.Planos;

[Authorize(Roles = "Administrador")]
public class CriarModel : PageModel
{
    private readonly AvaContext _context;

    public CriarModel(AvaContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Plano Plano { get; set; } = new()
    {
        IntervaloMeses = 1,
        Preco = 49.90m,
        Ativo = true
    };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Plano.CriadoEm = DateTime.UtcNow;
        _context.Planos.Add(Plano);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = $"Plano \"{Plano.Nome}\" criado com sucesso!";
        return RedirectToPage("/Admin/Planos/Index");
    }
}
