using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Certificados;

public class ValidarModel : PageModel
{
    private readonly AvaContext _context;

    public ValidarModel(AvaContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? Codigo { get; set; }

    public Certificado? CertificadoEncontrado { get; set; }
    public bool Pesquisou { get; set; }

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Codigo))
        {
            Pesquisou = true;
            CertificadoEncontrado = await _context.Certificados
                .Include(c => c.Usuario)
                .Include(c => c.Curso)
                .FirstOrDefaultAsync(c => c.CodigoAutenticidade == Codigo.Trim());
        }
    }
}
