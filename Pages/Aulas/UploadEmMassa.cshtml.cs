using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace AvaAcademico.Pages.Aulas;

[Authorize(Roles = "Administrador")]
[IgnoreAntiforgeryToken(Order = 1001)] // Permite requisições assíncronas de upload via Fetch/FormData
[RequestSizeLimit(524288000)]
public class UploadEmMassaModel : PageModel
{
    private readonly AvaContext _context;
    private readonly IWebHostEnvironment _env;

    public UploadEmMassaModel(AvaContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [BindProperty(SupportsGet = true)]
    public int? ModuloId { get; set; }

    public List<Curso> CursosComModulos { get; set; } = new();

    public async Task OnGetAsync()
    {
        CursosComModulos = await _context.Cursos
            .Include(c => c.Modulos)
            .ToListAsync();
    }

    // Busca a próxima ordem disponível para o módulo
    public async Task<IActionResult> OnGetUltimaOrdemAsync(int moduloId)
    {
        var maximaOrdem = await _context.Aulas
            .Where(a => a.ModuloId == moduloId)
            .MaxAsync(a => (int?)a.Ordem) ?? 0;

        return new JsonResult(new { proximaOrdem = maximaOrdem + 1 });
    }

    // Endpoint assíncrono chamado sequencialmente para cada arquivo de aula
    public async Task<IActionResult> OnPostUploadIndividualAsync(
        [FromForm] int moduloId,
        [FromForm] string titulo,
        [FromForm] int ordem,
        [FromForm] IFormFile video)
    {
        if (moduloId <= 0)
        {
            return BadRequest(new { success = false, message = "Módulo inválido ou não informado." });
        }

        if (string.IsNullOrWhiteSpace(titulo))
        {
            return BadRequest(new { success = false, message = "O título da aula é obrigatório." });
        }

        if (video == null || video.Length == 0)
        {
            return BadRequest(new { success = false, message = "Arquivo de vídeo não enviado ou vazio." });
        }

        try
        {
            var pastaDestino = Path.Combine(_env.WebRootPath, "videos");
            Directory.CreateDirectory(pastaDestino);

            var nomeArquivo = Guid.NewGuid().ToString() + Path.GetExtension(video.FileName);
            var caminhoFisico = Path.Combine(pastaDestino, nomeArquivo);

            using (var stream = new FileStream(caminhoFisico, FileMode.Create))
            {
                await video.CopyToAsync(stream);
            }

            var aula = new Aula
            {
                ModuloId = moduloId,
                Titulo = titulo.Trim(),
                Ordem = ordem > 0 ? ordem : 1,
                NomeArquivoVideo = nomeArquivo,
                Concluida = false
            };

            _context.Aulas.Add(aula);
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                aulaId = aula.Id,
                titulo = aula.Titulo,
                ordem = aula.Ordem,
                message = "Aula cadastrada com sucesso!"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao salvar aula: {ex.Message}" });
        }
    }
}
