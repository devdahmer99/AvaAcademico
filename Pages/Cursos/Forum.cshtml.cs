using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Cursos;

[Authorize]
public class ForumModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ForumModel(AvaContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Curso? Curso { get; set; }
    public List<Curso> TodosCursos { get; set; } = new();
    public List<TopicoForum> Topicos { get; set; } = new();
    public TopicoForum? TopicoSelecionado { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CursoId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; } // Suporte a ?id=X como alias de cursoId

    [BindProperty(SupportsGet = true)]
    public int? TopicoId { get; set; }

    [BindProperty]
    public NovoTopicoInputModel NovoTopico { get; set; } = new();

    [BindProperty]
    public NovaRespostaInputModel NovaResposta { get; set; } = new();

    public class NovoTopicoInputModel
    {
        public int CursoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
    }

    public class NovaRespostaInputModel
    {
        public string Conteudo { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        TodosCursos = await _context.Cursos.OrderBy(c => c.Titulo).ToListAsync();

        int idDoCurso = (CursoId.HasValue && CursoId.Value > 0) 
            ? CursoId.Value 
            : (Id.HasValue && Id.Value > 0 ? Id.Value : 0);

        if (idDoCurso > 0)
        {
            Curso = await _context.Cursos.FindAsync(idDoCurso);
            CursoId = idDoCurso;
        }
        else if (TodosCursos.Any())
        {
            // Se nenhum curso foi passado na URL, assume o primeiro curso
            Curso = TodosCursos.First();
            CursoId = Curso.Id;
        }

        var query = _context.TopicosForum
            .Include(t => t.Usuario)
            .Include(t => t.Curso)
            .Include(t => t.Respostas)
            .ThenInclude(r => r.Usuario)
            .AsQueryable();

        if (CursoId.HasValue && CursoId.Value > 0)
        {
            query = query.Where(t => t.CursoId == CursoId.Value);
        }

        Topicos = await query
            .OrderByDescending(t => t.CriadoEm)
            .ToListAsync();

        if (TopicoId.HasValue && TopicoId.Value > 0)
        {
            TopicoSelecionado = await _context.TopicosForum
                .Include(t => t.Usuario)
                .Include(t => t.Curso)
                .Include(t => t.Respostas)
                .ThenInclude(r => r.Usuario)
                .FirstOrDefaultAsync(t => t.Id == TopicoId.Value);

            if (TopicoSelecionado != null && (Curso == null || Curso.Id != TopicoSelecionado.CursoId))
            {
                Curso = TopicoSelecionado.Curso;
                CursoId = TopicoSelecionado.CursoId;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCriarTopicoAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        int idDoCurso = NovoTopico.CursoId > 0 
            ? NovoTopico.CursoId 
            : (CursoId.HasValue && CursoId.Value > 0 ? CursoId.Value : (Id ?? 0));

        if (idDoCurso <= 0)
        {
            TempData["MensagemErro"] = "Selecione um curso válido para criar o tópico.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(NovoTopico.Titulo))
        {
            TempData["MensagemErro"] = "O título do tópico é obrigatório.";
            return RedirectToPage(new { cursoId = idDoCurso });
        }

        if (string.IsNullOrWhiteSpace(NovoTopico.Conteudo))
        {
            TempData["MensagemErro"] = "A descrição/conteúdo do tópico é obrigatória.";
            return RedirectToPage(new { cursoId = idDoCurso });
        }

        var topico = new TopicoForum
        {
            CursoId = idDoCurso,
            UsuarioId = user.Id,
            Titulo = NovoTopico.Titulo.Trim(),
            Conteudo = NovoTopico.Conteudo.Trim(),
            CriadoEm = DateTime.UtcNow,
            Resolvido = false
        };

        _context.TopicosForum.Add(topico);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Tópico publicado no fórum com sucesso!";
        return RedirectToPage(new { cursoId = idDoCurso, topicoId = topico.Id });
    }

    public async Task<IActionResult> OnPostResponderAsync(int topicoId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        if (string.IsNullOrWhiteSpace(NovaResposta.Conteudo))
        {
            TempData["MensagemErro"] = "A resposta não pode ficar em branco.";
            return RedirectToPage(new { cursoId = CursoId, topicoId = topicoId });
        }

        var topico = await _context.TopicosForum.FindAsync(topicoId);
        if (topico == null) return NotFound("Tópico não encontrado.");

        var resposta = new RespostaForum
        {
            TopicoForumId = topicoId,
            UsuarioId = user.Id,
            Conteudo = NovaResposta.Conteudo.Trim(),
            CriadoEm = DateTime.UtcNow
        };

        _context.RespostasForum.Add(resposta);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Resposta enviada com sucesso!";
        return RedirectToPage(new { cursoId = topico.CursoId, topicoId = topicoId });
    }
}
