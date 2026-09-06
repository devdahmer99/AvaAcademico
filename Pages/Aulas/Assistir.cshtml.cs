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
    private readonly AvaAcademico.Services.IDockerService _dockerService;
    private readonly AvaAcademico.Services.IGamificacaoService _gamificacaoService;

    public AssistirModel(
        AvaContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env,
        AvaAcademico.Services.IDockerService dockerService,
        AvaAcademico.Services.IGamificacaoService gamificacaoService)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
        _dockerService = dockerService;
        _gamificacaoService = gamificacaoService;
    }

    public Laboratorio? LaboratorioDisponivel { get; set; }
    public InstanciaLaboratorio? InstanciaAtiva { get; set; }
    public bool LabResolvidoPorMim { get; set; }
    public bool DockerDisponivel { get; set; } = true;

    [BindProperty]
    public string? FlagSubmissao { get; set; }

    [BindProperty]
    public string? ConteudoAnotacao { get; set; }

    public AnotacaoAula? MinhaAnotacao { get; set; }

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
            .FirstOrDefaultAsync(a => a.Id == id) ?? null!;

        if (AulaAtual == null || AulaAtual.Modulo == null) return NotFound();

        CursoAtual = await _context.Cursos
            .Include(c => c.Modulos)
                .ThenInclude(m => m.Aulas)
            .FirstOrDefaultAsync(c => c.Id == AulaAtual.Modulo.CursoId) ?? null!;

        if (CursoAtual == null) return NotFound();

        // Determina aula anterior e próxima do curso
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

        // 2. Carrega Dúvidas da Aula
        Duvidas = await _context.DuvidasAulas
            .Include(d => d.Usuario)
            .Include(d => d.Respostas)
                .ThenInclude(r => r.Usuario)
            .Where(d => d.AulaId == id)
            .OrderByDescending(d => d.CriadoEm)
            .ToListAsync();

        // 3. Carrega Laboratório associado à Aula ou ao Módulo
        LaboratorioDisponivel = await _context.Laboratorios
            .FirstOrDefaultAsync(l => l.Ativo && (l.AulaId == id || (l.ModuloId == AulaAtual.ModuloId && l.AulaId == null)));

        if (LaboratorioDisponivel != null)
        {
            DockerDisponivel = await _dockerService.IsDockerDisponivelAsync();

            LabResolvidoPorMim = await _context.InstanciasLaboratorios
                .AnyAsync(i => i.LaboratorioId == LaboratorioDisponivel.Id && i.UsuarioId == user.Id && i.Resolvido);

            InstanciaAtiva = await _context.InstanciasLaboratorios
                .FirstOrDefaultAsync(i => i.LaboratorioId == LaboratorioDisponivel.Id && i.UsuarioId == user.Id && i.Status == "Executando" && i.ExpiraEm > DateTime.UtcNow);

            // Confirma se o container continua rodando no Docker
            if (InstanciaAtiva != null && DockerDisponivel)
            {
                bool aindaExecutando = await _dockerService.IsContainerExecutandoAsync(InstanciaAtiva.ContainerId);
                if (!aindaExecutando)
                {
                    InstanciaAtiva.Status = "Finalizado";
                    await _context.SaveChangesAsync();
                    InstanciaAtiva = null;
                }
            }
        }

        // 4. Carrega anotação pessoal do aluno para esta aula
        MinhaAnotacao = await _context.AnotacoesAulas
            .FirstOrDefaultAsync(a => a.AulaId == id && a.UsuarioId == user.Id);
        ConteudoAnotacao = MinhaAnotacao?.Conteudo ?? string.Empty;

        return Page();
    }

    public async Task<IActionResult> OnPostIniciarLabAsync(int id, int labId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        var lab = await _context.Laboratorios.FindAsync(labId);
        if (lab == null || !lab.Ativo)
        {
            TempData["MensagemErro"] = "Laboratório não encontrado ou inativo.";
            return RedirectToPage(new { id = id });
        }

        var resultado = await _dockerService.IniciarLaboratorioAsync(lab, user.Id);
        if (resultado.Sucesso)
        {
            TempData["MensagemSucesso"] = $"Laboratório iniciado com sucesso na porta {resultado.PortaHost}! Alvo pronto para teste.";
        }
        else
        {
            TempData["MensagemErro"] = resultado.Mensagem;
        }

        return RedirectToPage(new { id = id });
    }

    public async Task<IActionResult> OnPostPararLabAsync(int id, int instanciaId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        var instancia = await _context.InstanciasLaboratorios.FindAsync(instanciaId);
        if (instancia != null && instancia.UsuarioId == user.Id)
        {
            await _dockerService.PararLaboratorioAsync(instancia.ContainerId);
            TempData["MensagemSucesso"] = "Laboratório encerrado com sucesso! Os recursos do computador foram liberados.";
        }

        return RedirectToPage(new { id = id });
    }

    public async Task<IActionResult> OnPostSubmeterFlagAsync(int id, int instanciaId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        var instancia = await _context.InstanciasLaboratorios
            .Include(i => i.Laboratorio)
            .FirstOrDefaultAsync(i => i.Id == instanciaId && i.UsuarioId == user.Id);

        if (instancia == null || instancia.Laboratorio == null)
        {
            TempData["MensagemErro"] = "Instância de laboratório não encontrada.";
            return RedirectToPage(new { id = id });
        }

        if (string.IsNullOrWhiteSpace(FlagSubmissao))
        {
            TempData["MensagemErro"] = "Digite a flag capturada antes de submeter.";
            return RedirectToPage(new { id = id });
        }

        var flagEsperada = instancia.Laboratorio.Flag?.Trim() ?? "";
        var flagInformada = FlagSubmissao.Trim();

        if (!string.IsNullOrEmpty(flagEsperada) && string.Equals(flagInformada, flagEsperada, StringComparison.OrdinalIgnoreCase))
        {
            instancia.Resolvido = true;
            instancia.ResolvidoEm = DateTime.UtcNow;
            instancia.FlagSubmetida = flagInformada;
            await _context.SaveChangesAsync();

            // Gamificação: Credita XP do laboratório e desbloqueia badge
            var resultadoLab = await _gamificacaoService.CreditarXpAsync(user.Id, instancia.Laboratorio.Pontos, $"Laboratório: {instancia.Laboratorio.Titulo}");
            var resultadoBadge = await _gamificacaoService.DesbloquearConquistaAsync(user.Id, "PRIMEIRO_LAB_CONCLUIDO");

            if (resultadoBadge.ConquistaDesbloqueada != null)
            {
                TempData["ModuloConcluidoNome"] = "Desafio Hands-on Concluído!";
                TempData["BadgeTitulo"] = resultadoBadge.ConquistaDesbloqueada.Titulo;
                TempData["BadgeDescricao"] = resultadoBadge.ConquistaDesbloqueada.Descricao;
                TempData["BadgeIcone"] = resultadoBadge.ConquistaDesbloqueada.Icone;
                TempData["BadgeCor"] = resultadoBadge.ConquistaDesbloqueada.CorDestaque;
                TempData["BadgeXp"] = resultadoBadge.ConquistaDesbloqueada.XpRecompensa;
                TempData["XpTotal"] = resultadoLab.XpTotal;
                TempData["Nivel"] = resultadoLab.Nivel;
                TempData["TituloNivel"] = resultadoLab.TituloNivel;
                TempData["SubiuDeNivel"] = resultadoLab.SubiuDeNivel || resultadoBadge.SubiuDeNivel;
            }

            TempData["MensagemSucesso"] = $"🏆 EXCELENTE TRABALHO! Flag validada com sucesso! Você conquistou +{instancia.Laboratorio.Pontos} XP no laboratório!";
        }
        else
        {
            TempData["MensagemErro"] = "❌ Flag incorreta! Continue explorando o alvo e tente novamente.";
        }

        return RedirectToPage(new { id = id });
    }

    public async Task<IActionResult> OnPostEstenderTempoLabAsync(int id, int instanciaId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        var instancia = await _context.InstanciasLaboratorios.FindAsync(instanciaId);
        if (instancia != null && instancia.UsuarioId == user.Id && instancia.Status == "Executando")
        {
            instancia.ExpiraEm = instancia.ExpiraEm.AddMinutes(30);
            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = "Tempo do laboratório estendido em +30 minutos!";
        }

        return RedirectToPage(new { id = id });
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

            var gamif = await _gamificacaoService.ProcessarConclusaoAulaAsync(user.Id, id);

            if (gamif.ModuloConcluido && gamif.ConquistaDesbloqueada != null)
            {
                TempData["ModuloConcluidoNome"] = gamif.NomeModuloConcluido;
                TempData["BadgeTitulo"] = gamif.ConquistaDesbloqueada.Titulo;
                TempData["BadgeDescricao"] = gamif.ConquistaDesbloqueada.Descricao;
                TempData["BadgeIcone"] = gamif.ConquistaDesbloqueada.Icone;
                TempData["BadgeCor"] = gamif.ConquistaDesbloqueada.CorDestaque;
                TempData["BadgeXp"] = gamif.ConquistaDesbloqueada.XpRecompensa;
                TempData["XpTotal"] = gamif.XpTotal;
                TempData["Nivel"] = gamif.Nivel;
                TempData["TituloNivel"] = gamif.TituloNivel;
                TempData["SubiuDeNivel"] = gamif.SubiuDeNivel;
            }
            else
            {
                TempData["MensagemSucesso"] = $"Aula marcada como concluída! +{gamif.XpGanho} XP adicionado ao seu perfil.";
            }
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

            var gamif = await _gamificacaoService.ProcessarConclusaoAulaAsync(user.Id, id);

            return new JsonResult(new
            {
                success = true,
                moduloConcluido = gamif.ModuloConcluido,
                nomeModulo = gamif.NomeModuloConcluido,
                badge = gamif.ConquistaDesbloqueada != null ? new
                {
                    titulo = gamif.ConquistaDesbloqueada.Titulo,
                    descricao = gamif.ConquistaDesbloqueada.Descricao,
                    icone = gamif.ConquistaDesbloqueada.Icone,
                    cor = gamif.ConquistaDesbloqueada.CorDestaque,
                    xp = gamif.ConquistaDesbloqueada.XpRecompensa
                } : null,
                xpGanho = gamif.XpGanho,
                xpTotal = gamif.XpTotal,
                nivel = gamif.Nivel,
                tituloNivel = gamif.TituloNivel,
                subiuDeNivel = gamif.SubiuDeNivel
            });
        }

        return new JsonResult(new { success = true, jaConcluida = true });
    }

    public async Task<IActionResult> OnPostEnviarDuvidaAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Conta/Login");

        if (string.IsNullOrWhiteSpace(NovaDuvida.Pergunta))
        {
            TempData["MensagemErro"] = "Digite sua dúvida antes de enviar.";
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

    // Stream do vídeo com suporte a Range Requests (Seek sem travar)
    public IActionResult OnGetStream(string arquivo)
    {
        if (string.IsNullOrEmpty(arquivo)) return NotFound();

        string caminho;

        // Caso 1: path absoluto salvo no banco (ex: C:\Users\...\video.mp4)
        if (System.IO.Path.IsPathRooted(arquivo))
        {
            if (!System.IO.File.Exists(arquivo))
            {
                // Tenta fallback: apenas o nome do arquivo na pasta wwwroot/videos
                var nomeArquivo = System.IO.Path.GetFileName(arquivo);
                caminho = Path.Combine(_env.WebRootPath, "videos", nomeArquivo);
                if (!System.IO.File.Exists(caminho))
                {
                    return NotFound();
                }
            }
            else
            {
                caminho = arquivo;
            }
        }
        else
        {
            // Caso 2: apenas o nome do arquivo → serve de wwwroot/videos
            caminho = Path.Combine(_env.WebRootPath, "videos", arquivo);
            if (!System.IO.File.Exists(caminho)) return NotFound();
        }

        // Detecta MIME type pela extensão
        var ext = Path.GetExtension(caminho).ToLowerInvariant();
        var mimeType = ext switch
        {
            ".mp4"  => "video/mp4",
            ".webm" => "video/webm",
            ".ogg"  => "video/ogg",
            ".mov"  => "video/quicktime",
            _       => "video/mp4"
        };

        return new PhysicalFileResult(caminho, mimeType)
        {
            EnableRangeProcessing = true
        };
    }

    // Salva anotação pessoal via AJAX (sem recarregar a página)
    public async Task<IActionResult> OnPostSalvarAnotacaoAjaxAsync(int id, [FromBody] SalvarAnotacaoInput input)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return new StatusCodeResult(401);

        var texto = input?.Conteudo ?? string.Empty;
        if (texto.Length > 10000) texto = texto[..10000];

        var anotacao = await _context.AnotacoesAulas
            .FirstOrDefaultAsync(a => a.AulaId == id && a.UsuarioId == user.Id);

        if (anotacao == null)
        {
            anotacao = new AnotacaoAula
            {
                AulaId = id,
                UsuarioId = user.Id,
                Conteudo = texto,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };
            _context.AnotacoesAulas.Add(anotacao);
        }
        else
        {
            anotacao.Conteudo = texto;
            anotacao.AtualizadoEm = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new JsonResult(new
        {
            success = true,
            atualizadoEm = anotacao.AtualizadoEm.ToLocalTime().ToString("HH:mm:ss")
        });
    }

    public class SalvarAnotacaoInput
    {
        public string? Conteudo { get; set; }
    }
}

