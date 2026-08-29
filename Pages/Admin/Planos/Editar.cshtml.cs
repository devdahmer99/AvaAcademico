using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AvaAcademico.Pages.Admin.Planos;

[Authorize(Roles = "Administrador")]
public class EditarModel : PageModel
{
    private readonly AvaContext _context;

    public EditarModel(AvaContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Plano Plano { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var plano = await _context.Planos.FindAsync(id);
        if (plano == null) return NotFound();

        Plano = plano;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var planoDb = await _context.Planos.FindAsync(Plano.Id);
        if (planoDb == null) return NotFound();

        planoDb.Nome = Plano.Nome;
        planoDb.Descricao = Plano.Descricao;
        planoDb.Preco = Plano.Preco;
        planoDb.IntervaloMeses = Plano.IntervaloMeses;
        planoDb.Beneficios = Plano.Beneficios;
        planoDb.Destaque = Plano.Destaque;
        planoDb.Ativo = Plano.Ativo;

        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = $"Plano \"{planoDb.Nome}\" atualizado com sucesso!";
        return RedirectToPage("/Admin/Planos/Index");
    }
}
