using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Admin;

[Authorize(Roles = "Administrador")]
public class DashboardModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(AvaContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public int TotalAlunos { get; set; }
    public int TotalCursos { get; set; }
    public int TotalMatriculasAtivas { get; set; }
    public int TotalAssinaturasAtivas { get; set; }
    public int TotalCertificadosEmitidos { get; set; }
    public int TotalLivrosBiblioteca { get; set; }

    public List<MatriculaCurso> UltimasMatriculas { get; set; } = new();
    public List<TopicoForum> UltimasDuvidasForum { get; set; } = new();

    public async Task OnGetAsync()
    {
        TotalAlunos = await _userManager.Users.CountAsync();
        TotalCursos = await _context.Cursos.CountAsync();
        TotalMatriculasAtivas = await _context.Matriculas.Where(m => m.Status == "Ativa").CountAsync();
        TotalAssinaturasAtivas = await _context.Assinaturas.Where(a => a.Status == "Ativa").CountAsync();
        TotalCertificadosEmitidos = await _context.Certificados.CountAsync();
        TotalLivrosBiblioteca = await _context.LivrosBiblioteca.CountAsync();

        UltimasMatriculas = await _context.Matriculas
            .Include(m => m.Usuario)
            .Include(m => m.Curso)
            .OrderByDescending(m => m.DataMatricula)
            .Take(8)
            .ToListAsync();

        UltimasDuvidasForum = await _context.TopicosForum
            .Include(t => t.Usuario)
            .Include(t => t.Curso)
            .Include(t => t.Respostas)
            .OrderByDescending(t => t.CriadoEm)
            .Take(5)
            .ToListAsync();
    }
}
