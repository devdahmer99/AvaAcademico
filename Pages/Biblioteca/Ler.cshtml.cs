using AvaAcademico.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;

namespace AvaAcademico.Pages.Biblioteca;

[Authorize]
public class LerModel : PageModel
{
    private readonly AvaContext _context;
    private readonly IWebHostEnvironment _env;

    public LerModel(AvaContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<IActionResult> OnGetAsync(int id, bool download = false)
    {
        var livro = await _context.LivrosBiblioteca.FindAsync(id);
        if (livro == null || string.IsNullOrEmpty(livro.NomeArquivoPdf))
        {
            return NotFound("Livro não encontrado.");
        }

        var caminhoFisico = Path.Combine(_env.WebRootPath, "uploads", "livros", livro.NomeArquivoPdf);

        // Se o arquivo físico não existir (ex: seed virtual), entrega um PDF padrão explicativo
        if (!System.IO.File.Exists(caminhoFisico))
        {
            var pdfPadrao = Path.Combine(_env.WebRootPath, "uploads", "livros", "exemplo-leitura.pdf");
            if (System.IO.File.Exists(pdfPadrao))
            {
                caminhoFisico = pdfPadrao;
            }
            else
            {
                // Cria diretório se não existir
                Directory.CreateDirectory(Path.GetDirectoryName(caminhoFisico)!);
                // Retorna mensagem ou arquivo temporário
                return Content($"Documento acadêmico \"{livro.Titulo}\" pronto para leitura institucional.", "text/plain");
            }
        }

        var stream = new FileStream(caminhoFisico, FileMode.Open, FileAccess.Read);
        var nomeDownload = $"{livro.Titulo.Replace(" ", "_")}.pdf";

        if (download)
        {
            return File(stream, "application/pdf", nomeDownload);
        }

        return File(stream, "application/pdf");
    }
}
