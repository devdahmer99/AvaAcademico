using System.ComponentModel.DataAnnotations;
using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Admin.Laboratorios;

[Authorize(Roles = "Administrador")]
public class CriarModel : PageModel
{
    private readonly AvaContext _context;

    public CriarModel(AvaContext context)
    {
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<Curso> CursosComModulosEAulas { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [MaxLength(150)]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "A imagem Docker é obrigatória.")]
        [MaxLength(200)]
        public string ImagemDocker { get; set; } = string.Empty;

        public int PortaPadraoContainer { get; set; } = 80;

        [MaxLength(150)]
        public string? Flag { get; set; }

        public int Pontos { get; set; } = 100;

        public int TempoLimiteMinutos { get; set; } = 60;

        [MaxLength(1000)]
        public string? ParametrosAmbiente { get; set; }

        public int? AulaId { get; set; }
        public int? ModuloId { get; set; }
    }

    public async Task OnGetAsync()
    {
        CursosComModulosEAulas = await _context.Cursos
            .Include(c => c.Modulos)
                .ThenInclude(m => m.Aulas)
            .OrderBy(c => c.Titulo)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            CursosComModulosEAulas = await _context.Cursos
                .Include(c => c.Modulos)
                    .ThenInclude(m => m.Aulas)
                .OrderBy(c => c.Titulo)
                .ToListAsync();
            return Page();
        }

        var lab = new Laboratorio
        {
            Titulo = Input.Titulo.Trim(),
            Descricao = Input.Descricao?.Trim() ?? string.Empty,
            ImagemDocker = Input.ImagemDocker.Trim(),
            PortaPadraoContainer = Input.PortaPadraoContainer > 0 ? Input.PortaPadraoContainer : 80,
            Flag = Input.Flag?.Trim(),
            Pontos = Input.Pontos > 0 ? Input.Pontos : 100,
            TempoLimiteMinutos = Input.TempoLimiteMinutos > 0 ? Input.TempoLimiteMinutos : 60,
            ParametrosAmbiente = Input.ParametrosAmbiente?.Trim(),
            AulaId = Input.AulaId > 0 ? Input.AulaId : null,
            ModuloId = Input.ModuloId > 0 ? Input.ModuloId : null,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };

        _context.Laboratorios.Add(lab);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = $"Laboratório '{lab.Titulo}' cadastrado com sucesso!";
        return RedirectToPage("/Admin/Laboratorios/Index");
    }
}

