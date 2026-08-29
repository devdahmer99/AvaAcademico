using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace AvaAcademico.Pages.Aulas;

[Authorize(Roles = "Administrador")]
public class CriarModel : PageModel
{
    private readonly AvaContext _context;
    private readonly IWebHostEnvironment _env;

    public CriarModel(AvaContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [BindProperty]
    public Aula Aula { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? ModuloId { get; set; }

    [BindProperty]
    public IFormFile VideoUpload { get; set; }

    public List<Curso> CursosComModulos { get; set; } = new();

    public async Task OnGetAsync()
    {
        CursosComModulos = await _context.Cursos
            .Include(c => c.Modulos)
            .ToListAsync();

        if (ModuloId.HasValue && ModuloId.Value > 0)
        {
            var maximaOrdem = await _context.Aulas
                .Where(a => a.ModuloId == ModuloId.Value)
                .MaxAsync(a => (int?)a.Ordem) ?? 0;

            Aula.ModuloId = ModuloId.Value;
            Aula.Ordem = maximaOrdem + 1;
        }
        else if (Aula.Ordem == 0)
        {
            Aula.Ordem = 1;
        }
    }

    // Busca a última ordem do módulo selecionado via AJAX
    public async Task<IActionResult> OnGetUltimaOrdemAsync(int moduloId)
    {
        var maximaOrdem = await _context.Aulas
            .Where(a => a.ModuloId == moduloId)
            .MaxAsync(a => (int?)a.Ordem) ?? 0;

        return new JsonResult(new { proximaOrdem = maximaOrdem + 1 });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (VideoUpload != null && VideoUpload.Length > 0)
        {
            var pastaDestino = Path.Combine(_env.WebRootPath, "videos");
            Directory.CreateDirectory(pastaDestino);

            var nomeArquivo = Guid.NewGuid().ToString() + Path.GetExtension(VideoUpload.FileName);
            var caminhoFisico = Path.Combine(pastaDestino, nomeArquivo);

            using (var stream = new FileStream(caminhoFisico, FileMode.Create))
            {
                await VideoUpload.CopyToAsync(stream);
            }

            Aula.NomeArquivoVideo = nomeArquivo;
        }

        _context.Aulas.Add(Aula);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = $"Aula \"{Aula.Titulo}\" cadastrada com sucesso!";

        // Recarrega mantendo o mesmo módulo selecionado e com a ordem incrementada
        return RedirectToPage(new { moduloId = Aula.ModuloId });
    }
}