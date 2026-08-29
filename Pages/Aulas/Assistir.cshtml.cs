using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace AvaAcademico.Pages.Aulas;

[Authorize]
public class AssistirModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public AssistirModel(
        AvaContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    public Aula AulaAtual { get; set; } = null!;
    public Curso CursoAtual { get; set; } = null!;
    public Aula? ProximaAula { get; set; }
    public Aula? AulaAnterior { get; set; }

    public List<int> AulasConcluidasIds { get; set; } = new();

    public bool IsConcluidaPorMim { get; set; }
    public int TotalAulasCurso { get; set; }
    public int MinhasAulasConcluidasCount { get; set; }
    public int PercentualProgresso { get; set; }
    public bool Curso100PorCentoConcluido { get; set; }

    public List<DuvidaAula> Duvidas { get; set; } = new();

    [BindProperty]
    public NovaDuvidaInputModel NovaDuvida { get; set; } = new();

    [BindProperty]
    public NovaRespostaDuvidaInputModel NovaRespostaDuvida { get; set; } = new();

    public class NovaDuvidaInputModel
    {
        public string Pergunta { get; set; } = string.Empty;
        public int? MinutoVideo { get; set; }
    }

    public class NovaRespostaDuvidaInputModel
    {
        public int DuvidaId { get; set; }
        public string Resposta { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        AulaAtual = await _context.Aulas
            .Include(a => a.Modulo)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (AulaAtual == null || AulaAtual.Modulo == null) return NotFound();

        CursoAtual = await _context.Cursos
            .Include(c => c.Modulos)
                .ThenInclude(m => m.Aulas)
            .FirstOrDefaultAsync(c => c.Id == AulaAtual.Modulo.CursoId);

        if (CursoAtual == null) return NotFound();

        // Determina aula anterior e prÃ³xima do curso
        var todasAulasOrdenadas = CursoAtual.Modulos
            .OrderBy(m => m.Ordem)
            .SelectMany(m => m.Aulas.OrderBy(a => a.Ordem))
            .ToList();

        var currentIndex = todasAulasOrdenadas.FindIndex(a => a.Id == AulaAtual.Id);
        if (currentIndex >= 0 && currentIndex < todasAulasOrdenadas.Count - 1)
        {
            ProximaAula = todasAulasOrdenadas[currentIndex + 1];
        }
        if (currentIndex > 0)
        {
            AulaAnterior = todasAulasOrdenadas[currentIndex - 1];
        }

        // 1. Carrega progresso individual do aluno
        AulasConcluidasIds = await _context.ProgressosAulas
            .Where(p => p.UsuarioId == user.Id)
            .Select(p => p.AulaId)
            .ToListAsync();

        IsConcluidaPorMim = AulasConcluidasIds.Contains(AulaAtual.Id);

        var todasAulasIds = todasAulasOrdenadas.Select(a => a.Id).ToList();
        TotalAulasCurso = todasAulasIds.Count;
        MinhasAulasConcluidasCount = todasAulasIds.Count(aId => AulasConcluidasIds.Contains(aId));
        PercentualProgresso = TotalAulasCurso > 0 ? (int)Math.Round((double)MinhasAulasConcluidasCount / TotalAulasCurso * 100) : 0;
        Curso100PorCentoConcluido = TotalAulasCurso > 0 && MinhasAulasConcluidasCount >= TotalAulasCurso;

        // 2. Carrega DÃºvidas da Aula
        Duvidas = await _context.DuvidasAulas
            .Include(d => d.Usuario)
            .Include(d => d.Respostas)
                .ThenInclude(r => r.Usuario)
            .Where(d => d.AulaId == id)
            .OrderByDescending(d => d.CriadoEm)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostMarcarConcluidaAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        var jaExiste = await _context.ProgressosAulas
            .AnyAsync(p => p.UsuarioId == user.Id && p.AulaId == id);

        if (!jaExiste)
        {
            _context.ProgressosAulas.Add(new ProgressoAula
            {
                UsuarioId = user.Id,
                AulaId = id,
                ConcluidaEm = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["mensagemSuhåsso"] = "Aula marcada como concluÃ­da! Seu progresso foi atualizado.";
        }

        return RedirectToPage(new { id = id });
    }

    public async Task<IActionResult> OnPostMarcarConcluidaAjaxAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return new StatusCodeResult(401);


        var jaExiste = await _context.ProgressosAulas
            .AnyAsync(p => p.UsuarioId == user.Id && p.AulaId == id);

        if (!jaExiste)
        {
            _context.ProgressosAulas.Add(new ProgressoAula
            {
                UsuarioId = user.Id,
                AulaId = id,
                ConcluidaEm = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostEnviarDuvidaAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        if (string.IsNullOrWhiteSpace(NovaDuvida.Pergunta))
        {
            TempData["MensagemErro"] = "Digite sua dÃºvida antes de enviar.";
            return RedirectToPage(new { id = id });
        }

        var duvida = new DuvidaAula
        {
            AulaId = id,
            UsuarioId = user.Id,
            Pergunta = NovaDuvida.Pergunta.Trim(),
            MinutoVideo = NovaDuvida.MinutoVideo,
            CriadoEm = DateTime.UtcNow
        };

        _context.DuvidasAulas.Add(duvida);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Sua pergunta foi enviada aos instrutores e colegas!";
        return RedirectToPage(new { id = id });
    }

    public async Task<IActionResult> OnPostResponderDuvidaAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        if (string.IsNullOrWhiteSpace(NovaRespostaDuvida.Resposta) || NovaRespostaDuvida.DuvidaId <= 0)
        {
            TempData["MensagemErro"] = "Digite uma resposta antes de enviar.";
            return RedirectToPage(new { id = id });
        }

        bool isInstrutor = User.IsInRole("Administrador");

        var resposta = new RespostaDuvidaAula
        {
            DuvidaAulaId = NovaRespostaDuvida.DuvidaId,
            UsuarioId = user.Id,
            Resposta = NovaRespostaDuvida.Resposta.Trim(),
            IsInstrutorOuAdmin = isInstrutor,
            CriadoEm = DateTime.UtcNow
        };

        _context.RespostasDuvidasAulas.Add(resposta);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Resposta publicada com sucesso!";
        return RedirectToPage(new { id = id });
    }

    // Stream do vÃ­deo sem travar
    public IActionResult OnGetStream(string arquivo)
    {
        if (string.IsNullOrEmpty(arquivo)) return NotFound();
        var caminho = Path.Combine(_env.WebRootPath, "videos", arquivo);
        if (!System.IO.File.Exists(caminho)) return NotFound();

        return new PhysicalFileResult(caminho, "video/mp4")
        {
            EnableRangeProcessing = true
        };
    }
}

