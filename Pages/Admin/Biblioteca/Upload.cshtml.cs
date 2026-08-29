using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace AvaAcademico.Pages.Admin.Biblioteca;

[Authorize(Roles = "Administrador")]
public class UploadModel : PageModel
{
    private readonly AvaContext _context;
    private readonly IWebHostEnvironment _env;

    public UploadModel(AvaContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [BindProperty]
    public LivroInputModel InputLivro { get; set; } = new();

    [BindProperty]
    [Required(ErrorMessage = "O arquivo PDF é obrigatório.")]
    public IFormFile ArquivoPdf { get; set; } = null!;

    public class LivroInputModel
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O autor é obrigatório.")]
        [StringLength(150)]
        public string Autor { get; set; } = string.Empty;

        [Required(ErrorMessage = "A categoria é obrigatória.")]
        [StringLength(100)]
        public string Categoria { get; set; } = "Tecnologia";

        [StringLength(2000)]
        public string? Descricao { get; set; }

        public int? NumeroPaginas { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (ArquivoPdf == null || ArquivoPdf.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Selecione um arquivo PDF válido.");
            return Page();
        }

        var extensao = Path.GetExtension(ArquivoPdf.FileName).ToLowerInvariant();
        if (extensao != ".pdf")
        {
            ModelState.AddModelError(string.Empty, "O arquivo precisa ter a extensão .PDF.");
            return Page();
        }

        var pastaDestino = Path.Combine(_env.WebRootPath, "uploads", "livros");
        Directory.CreateDirectory(pastaDestino);

        var nomeArquivoSeguro = $"livro_{Guid.NewGuid()}{extensao}";
        var caminhoFisico = Path.Combine(pastaDestino, nomeArquivoSeguro);

        using (var stream = new FileStream(caminhoFisico, FileMode.Create))
        {
            await ArquivoPdf.CopyToAsync(stream);
        }

        var livro = new LivroBiblioteca
        {
            Titulo = InputLivro.Titulo.Trim(),
            Autor = InputLivro.Autor.Trim(),
            Categoria = InputLivro.Categoria.Trim(),
            Descricao = InputLivro.Descricao?.Trim(),
            NumeroPaginas = InputLivro.NumeroPaginas,
            NomeArquivoPdf = nomeArquivoSeguro,
            TamanhoBytes = ArquivoPdf.Length,
            CriadoEm = DateTime.UtcNow
        };

        _context.LivrosBiblioteca.Add(livro);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = $"Livro \"{livro.Titulo}\" enviado e adicionado à biblioteca com sucesso!";
        return RedirectToPage("/Admin/Biblioteca/Index");
    }
}
