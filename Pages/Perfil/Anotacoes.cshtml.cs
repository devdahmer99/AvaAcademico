using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Perfil;

[Authorize]
public class AnotacoesModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AnotacoesModel(AvaContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<AnotacaoItemDto> Anotacoes { get; set; } = new();
    public string? TermoBusca { get; set; }
    public int TotalAnotacoes { get; set; }
    public int TotalCursosAnotados { get; set; }

    public class AnotacaoItemDto
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string AulaTitulo { get; set; } = string.Empty;
        public int ModuloOrdem { get; set; }
        public string ModuloTitulo { get; set; } = string.Empty;
        public int CursoId { get; set; }
        public string CursoTitulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public DateTime AtualizadoEm { get; set; }
        public DateTime CriadoEm { get; set; }
        public int Caracteres => Conteudo?.Length ?? 0;
        public int QuantidadeLinhas => string.IsNullOrEmpty(Conteudo) ? 0 : Conteudo.Split('\n').Length;
    }

    public async Task<IActionResult> OnGetAsync(string? q)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        TermoBusca = q?.Trim();

        var query = _context.AnotacoesAulas
            .Include(a => a.Aula)
                .ThenInclude(au => au!.Modulo)
                    .ThenInclude(m => m!.Curso)
            .Where(a => a.UsuarioId == user.Id && !string.IsNullOrWhiteSpace(a.Conteudo));

        if (!string.IsNullOrWhiteSpace(TermoBusca))
        {
            var termo = TermoBusca.ToLower();
            query = query.Where(a =>
                a.Conteudo.ToLower().Contains(termo) ||
                a.Aula!.Titulo.ToLower().Contains(termo) ||
                a.Aula!.Modulo!.Titulo.ToLower().Contains(termo) ||
                a.Aula!.Modulo!.Curso!.Titulo.ToLower().Contains(termo));
        }

        var lista = await query
            .OrderByDescending(a => a.AtualizadoEm)
            .Select(a => new AnotacaoItemDto
            {
                Id = a.Id,
                AulaId = a.AulaId,
                AulaTitulo = a.Aula!.Titulo,
                ModuloOrdem = a.Aula!.Modulo!.Ordem,
                ModuloTitulo = a.Aula!.Modulo!.Titulo,
                CursoId = a.Aula!.Modulo!.CursoId,
                CursoTitulo = a.Aula!.Modulo!.Curso!.Titulo,
                Conteudo = a.Conteudo,
                AtualizadoEm = a.AtualizadoEm,
                CriadoEm = a.CriadoEm
            })
            .ToListAsync();

        Anotacoes = lista;

        // Métricas do Aluno
        TotalAnotacoes = await _context.AnotacoesAulas
            .CountAsync(a => a.UsuarioId == user.Id && !string.IsNullOrWhiteSpace(a.Conteudo));

        TotalCursosAnotados = await _context.AnotacoesAulas
            .Where(a => a.UsuarioId == user.Id && !string.IsNullOrWhiteSpace(a.Conteudo))
            .Select(a => a.Aula!.Modulo!.CursoId)
            .Distinct()
            .CountAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        var anotacao = await _context.AnotacoesAulas
            .FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == user.Id);

        if (anotacao != null)
        {
            _context.AnotacoesAulas.Remove(anotacao);
            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = "Anotação removida com sucesso!";
        }

        return RedirectToPage(new { q = TermoBusca });
    }
}

