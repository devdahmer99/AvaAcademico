using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Admin;

[Authorize(Roles = "Administrador")]
public class MatriculasModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MatriculasModel(AvaContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<Curso> Cursos { get; set; } = new();
    public List<ApplicationUser> AlunosDisponiveis { get; set; } = new();
    public List<MatriculaDetalhadaViewModel> Matriculas { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? CursoId { get; set; }

    [BindProperty]
    public NovaMatriculaInputModel NovaMatricula { get; set; } = new();

    public class NovaMatriculaInputModel
    {
        public string UsuarioId { get; set; } = string.Empty;
        public int CursoId { get; set; }
    }

    public class MatriculaDetalhadaViewModel
    {
        public int MatriculaId { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public string NomeAluno { get; set; } = string.Empty;
        public string EmailAluno { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public string TituloCurso { get; set; } = string.Empty;
        public int CursoId { get; set; }
        public DateTime DataMatricula { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalAulas { get; set; }
        public int AulasConcluidas { get; set; }
        public int PercentualProgresso { get; set; }
        public bool PossuiCertificado { get; set; }
    }

    public async Task OnGetAsync()
    {
        Cursos = await _context.Cursos.OrderBy(c => c.Titulo).ToListAsync();
        AlunosDisponiveis = await _userManager.Users.OrderBy(u => u.NomeCompleto ?? u.UserName).ToListAsync();

        var query = _context.Matriculas
            .Include(m => m.Usuario)
            .Include(m => m.Curso)
            .ThenInclude(c => c.Modulos)
            .ThenInclude(mod => mod.Aulas)
            .AsQueryable();

        if (CursoId.HasValue && CursoId.Value > 0)
        {
            query = query.Where(m => m.CursoId == CursoId.Value);
        }

        var matriculasList = await query.OrderByDescending(m => m.DataMatricula).ToListAsync();

        var todosProgressos = await _context.ProgressosAulas.ToListAsync();
        var todosCertificados = await _context.Certificados.ToListAsync();

        Matriculas = matriculasList.Select(m =>
        {
            var todasAulas = m.Curso.Modulos.SelectMany(mod => mod.Aulas).Select(a => a.Id).ToList();
            var concluidas = todosProgressos.Count(p => p.UsuarioId == m.UsuarioId && todasAulas.Contains(p.AulaId));
            var total = todasAulas.Count;
            var pct = total > 0 ? (int)Math.Round((double)concluidas / total * 100) : 0;
            var temCert = todosCertificados.Any(c => c.UsuarioId == m.UsuarioId && c.CursoId == m.CursoId);

            return new MatriculaDetalhadaViewModel
            {
                MatriculaId = m.Id,
                UsuarioId = m.UsuarioId,
                NomeAluno = m.Usuario?.NomeCompleto ?? m.Usuario?.UserName ?? "Aluno",
                EmailAluno = m.Usuario?.Email ?? "",
                FotoUrl = m.Usuario?.FotoUrl,
                TituloCurso = m.Curso.Titulo,
                CursoId = m.CursoId,
                DataMatricula = m.DataMatricula,
                Status = m.Status,
                TotalAulas = total,
                AulasConcluidas = concluidas,
                PercentualProgresso = pct,
                PossuiCertificado = temCert
            };
        }).ToList();
    }

    public async Task<IActionResult> OnPostMatricularManualAsync()
    {
        if (string.IsNullOrEmpty(NovaMatricula.UsuarioId) || NovaMatricula.CursoId <= 0)
        {
            TempData["MensagemErro"] = "Selecione o aluno e o curso para realizar a matrícula.";
            return RedirectToPage(new { cursoId = CursoId });
        }

        var jaMatriculado = await _context.Matriculas
            .AnyAsync(m => m.UsuarioId == NovaMatricula.UsuarioId && m.CursoId == NovaMatricula.CursoId);

        if (!jaMatriculado)
        {
            _context.Matriculas.Add(new MatriculaCurso
            {
                UsuarioId = NovaMatricula.UsuarioId,
                CursoId = NovaMatricula.CursoId,
                DataMatricula = DateTime.UtcNow,
                Status = "Ativa"
            });
            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = "Aluno matriculado com sucesso no curso!";
        }
        else
        {
            TempData["MensagemErro"] = "Este aluno já está matriculado neste curso.";
        }

        return RedirectToPage(new { cursoId = NovaMatricula.CursoId });
    }

    public async Task<IActionResult> OnPostCancelarMatriculaAsync(int matriculaId)
    {
        var matricula = await _context.Matriculas.FindAsync(matriculaId);
        if (matricula != null)
        {
            _context.Matriculas.Remove(matricula);
            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = "Matrícula removida com sucesso.";
        }
        return RedirectToPage(new { cursoId = CursoId });
    }
}
