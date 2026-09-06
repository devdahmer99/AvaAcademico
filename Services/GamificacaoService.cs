using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Services;

public class GamificacaoService : IGamificacaoService
{
    private readonly AvaContext _context;
    private readonly ILogger<GamificacaoService> _logger;

    public GamificacaoService(AvaContext context, ILogger<GamificacaoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResultadoGamificacao> ProcessarConclusaoAulaAsync(string usuarioId, int aulaId)
    {
        var resultado = new ResultadoGamificacao { Sucesso = true };

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (user == null) return new ResultadoGamificacao { Sucesso = false };

        var aula = await _context.Aulas
            .Include(a => a.Modulo)
            .FirstOrDefaultAsync(a => a.Id == aulaId);

        if (aula == null || aula.Modulo == null) return new ResultadoGamificacao { Sucesso = false };

        int nivelAnterior = CalcularNivel(user.PontosXp).Nivel;

        // 1. Concede XP pela aula concluída (+15 XP)
        user.PontosXp += 15;
        resultado.XpGanho += 15;

        // 2. Verifica se todas as aulas do módulo foram concluídas pelo aluno
        var todasAulasModulo = await _context.Aulas
            .Where(a => a.ModuloId == aula.ModuloId)
            .Select(a => a.Id)
            .ToListAsync();

        var aulasConcluidasModulo = await _context.ProgressosAulas
            .Where(p => p.UsuarioId == usuarioId && todasAulasModulo.Contains(p.AulaId))
            .Select(p => p.AulaId)
            .Distinct()
            .ToListAsync();

        // Garante que a aula atual conte
        if (!aulasConcluidasModulo.Contains(aulaId))
        {
            aulasConcluidasModulo.Add(aulaId);
        }

        bool moduloCompleto = todasAulasModulo.Count > 0 && aulasConcluidasModulo.Count >= todasAulasModulo.Count;

        if (moduloCompleto)
        {
            // Busca ou gera a Conquista do Módulo
            string codigoBadge = $"MOD_{aula.Modulo.Id}_COMPLETED";
            var badge = await _context.Conquistas.FirstOrDefaultAsync(c => c.Codigo == codigoBadge);

            if (badge == null)
            {
                badge = CriarBadgeParaModulo(aula.Modulo);
                _context.Conquistas.Add(badge);
                await _context.SaveChangesAsync();
            }

            // Verifica se o aluno já possui esta conquista
            bool jaDesbloqueou = await _context.ConquistasUsuarios
                .AnyAsync(cu => cu.UsuarioId == usuarioId && cu.ConquistaId == badge.Id);

            if (!jaDesbloqueou)
            {
                var novaConquista = new ConquistaUsuario
                {
                    UsuarioId = usuarioId,
                    ConquistaId = badge.Id,
                    DesbloqueadoEm = DateTime.UtcNow
                };

                _context.ConquistasUsuarios.Add(novaConquista);

                // Concede bônus de XP do Módulo (+100 XP)
                user.PontosXp += badge.XpRecompensa;
                resultado.XpGanho += badge.XpRecompensa;
                resultado.ModuloConcluido = true;
                resultado.NomeModuloConcluido = aula.Modulo.Titulo;
                resultado.ConquistaDesbloqueada = badge;
            }
        }

        // 3. Verifica se todas as aulas do curso foram concluídas pelo aluno
        var todasAulasCurso = await _context.Aulas
            .Where(a => a.Modulo != null && a.Modulo.CursoId == aula.Modulo.CursoId)
            .Select(a => a.Id)
            .ToListAsync();

        var aulasConcluidasCurso = await _context.ProgressosAulas
            .Where(p => p.UsuarioId == usuarioId && todasAulasCurso.Contains(p.AulaId))
            .Select(p => p.AulaId)
            .Distinct()
            .ToListAsync();

        if (!aulasConcluidasCurso.Contains(aulaId))
        {
            aulasConcluidasCurso.Add(aulaId);
        }

        bool cursoCompleto = todasAulasCurso.Count > 0 && aulasConcluidasCurso.Count >= todasAulasCurso.Count;
        if (cursoCompleto)
        {
            string codigoCurso = $"CURSO_{aula.Modulo.CursoId}_COMPLETED";
            var badgeCurso = await _context.Conquistas.FirstOrDefaultAsync(c => c.Codigo == codigoCurso);
            if (badgeCurso != null)
            {
                bool jaDesbloqueouCurso = await _context.ConquistasUsuarios
                    .AnyAsync(cu => cu.UsuarioId == usuarioId && cu.ConquistaId == badgeCurso.Id);

                if (!jaDesbloqueouCurso)
                {
                    _context.ConquistasUsuarios.Add(new ConquistaUsuario
                    {
                        UsuarioId = usuarioId,
                        ConquistaId = badgeCurso.Id,
                        DesbloqueadoEm = DateTime.UtcNow
                    });
                    user.PontosXp += badgeCurso.XpRecompensa;
                    resultado.XpGanho += badgeCurso.XpRecompensa;
                    resultado.ConquistaDesbloqueada ??= badgeCurso;
                }
            }
        }

        await _context.SaveChangesAsync();

        var infoNivel = CalcularNivel(user.PontosXp);
        resultado.XpTotal = user.PontosXp;
        resultado.Nivel = infoNivel.Nivel;
        resultado.TituloNivel = infoNivel.Titulo;
        resultado.SubiuDeNivel = infoNivel.Nivel > nivelAnterior;

        return resultado;
    }

    public async Task<ResultadoGamificacao> CreditarXpAsync(string usuarioId, int xp, string motivo)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (user == null) return new ResultadoGamificacao { Sucesso = false };

        int nivelAnterior = CalcularNivel(user.PontosXp).Nivel;
        user.PontosXp += xp;
        await _context.SaveChangesAsync();

        var infoNivel = CalcularNivel(user.PontosXp);

        return new ResultadoGamificacao
        {
            Sucesso = true,
            XpGanho = xp,
            XpTotal = user.PontosXp,
            Nivel = infoNivel.Nivel,
            TituloNivel = infoNivel.Titulo,
            SubiuDeNivel = infoNivel.Nivel > nivelAnterior
        };
    }

    public async Task<ResultadoGamificacao> DesbloquearConquistaAsync(string usuarioId, string codigoConquista)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == usuarioId);
        var conquista = await _context.Conquistas.FirstOrDefaultAsync(c => c.Codigo == codigoConquista);

        if (user == null || conquista == null) return new ResultadoGamificacao { Sucesso = false };

        bool jaPossui = await _context.ConquistasUsuarios
            .AnyAsync(cu => cu.UsuarioId == usuarioId && cu.ConquistaId == conquista.Id);

        if (jaPossui)
        {
            var infoNivelAtual = CalcularNivel(user.PontosXp);
            return new ResultadoGamificacao
            {
                Sucesso = true,
                XpTotal = user.PontosXp,
                Nivel = infoNivelAtual.Nivel,
                TituloNivel = infoNivelAtual.Titulo
            };
        }

        int nivelAnterior = CalcularNivel(user.PontosXp).Nivel;

        _context.ConquistasUsuarios.Add(new ConquistaUsuario
        {
            UsuarioId = usuarioId,
            ConquistaId = conquista.Id,
            DesbloqueadoEm = DateTime.UtcNow
        });

        user.PontosXp += conquista.XpRecompensa;
        await _context.SaveChangesAsync();

        var infoNivel = CalcularNivel(user.PontosXp);

        return new ResultadoGamificacao
        {
            Sucesso = true,
            XpGanho = conquista.XpRecompensa,
            XpTotal = user.PontosXp,
            Nivel = infoNivel.Nivel,
            TituloNivel = infoNivel.Titulo,
            SubiuDeNivel = infoNivel.Nivel > nivelAnterior,
            ConquistaDesbloqueada = conquista
        };
    }

    public async Task<PerfilGamificacaoDto> ObterPerfilGamificacaoAsync(string usuarioId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == usuarioId);
        int xpTotal = user?.PontosXp ?? 0;

        var infoNivel = CalcularNivel(xpTotal);

        var todasConquistas = await _context.Conquistas
            .OrderBy(c => c.Id)
            .ToListAsync();

        var minhasConquistas = await _context.ConquistasUsuarios
            .Where(cu => cu.UsuarioId == usuarioId)
            .ToDictionaryAsync(cu => cu.ConquistaId, cu => cu.DesbloqueadoEm);

        var listaDetalhada = todasConquistas.Select(c => new ConquistaDetalhadaDto
        {
            Id = c.Id,
            Codigo = c.Codigo,
            Titulo = c.Titulo,
            Descricao = c.Descricao,
            Icone = c.Icone,
            CorDestaque = c.CorDestaque,
            Categoria = c.Categoria,
            XpRecompensa = c.XpRecompensa,
            Desbloqueada = minhasConquistas.ContainsKey(c.Id),
            DesbloqueadoEm = minhasConquistas.TryGetValue(c.Id, out var data) ? data : null
        }).ToList();

        int deltaNivel = Math.Max(1, infoNivel.XpProximo - infoNivel.XpMinimo);
        int progressoNivel = Math.Clamp(xpTotal - infoNivel.XpMinimo, 0, deltaNivel);
        int percentual = (int)Math.Round((double)progressoNivel / deltaNivel * 100);

        return new PerfilGamificacaoDto
        {
            PontosXp = xpTotal,
            Nivel = infoNivel.Nivel,
            TituloNivel = infoNivel.Titulo,
            XpMinimoNivelAtual = infoNivel.XpMinimo,
            XpProximoNivel = infoNivel.XpProximo,
            PercentualProgressoNivel = percentual,
            TotalConquistasDesbloqueadas = minhasConquistas.Count,
            TotalConquistasDisponiveis = todasConquistas.Count,
            Conquistas = listaDetalhada
        };
    }

    public async Task GerarConquistasPadraoModulosAsync(int cursoId)
    {
        var modulos = await _context.Modulos
            .Where(m => m.CursoId == cursoId)
            .OrderBy(m => m.Ordem)
            .ToListAsync();

        foreach (var mod in modulos)
        {
            string codigo = $"MOD_{mod.Id}_COMPLETED";
            if (!await _context.Conquistas.AnyAsync(c => c.Codigo == codigo))
            {
                var badge = CriarBadgeParaModulo(mod);
                _context.Conquistas.Add(badge);
            }
        }

        // Conquista de primeiro laboratório
        if (!await _context.Conquistas.AnyAsync(c => c.Codigo == "PRIMEIRO_LAB_CONCLUIDO"))
        {
            _context.Conquistas.Add(new Conquista
            {
                Codigo = "PRIMEIRO_LAB_CONCLUIDO",
                Titulo = "Invasor de Primeira Viagem",
                Descricao = "Conquistou e capturou a primeira flag em um Laboratório Prático Docker.",
                Icone = "bi-flag-fill",
                CorDestaque = "#ef4444",
                Categoria = "Laboratorio",
                XpRecompensa = 150,
                CriadoEm = DateTime.UtcNow
            });
        }

        // Conquista Mestre do Curso
        if (!await _context.Conquistas.AnyAsync(c => c.Codigo == $"CURSO_{cursoId}_COMPLETED"))
        {
            _context.Conquistas.Add(new Conquista
            {
                Codigo = $"CURSO_{cursoId}_COMPLETED",
                Titulo = "Master Pentester Formado",
                Descricao = "Completou 100% da formação e todas as aulas do curso.",
                Icone = "bi-trophy-fill",
                CorDestaque = "#f59e0b",
                Categoria = "Curso",
                XpRecompensa = 1000,
                CursoId = cursoId,
                CriadoEm = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    private static Conquista CriarBadgeParaModulo(Modulo modulo)
    {
        string tit = modulo.Titulo.ToLower();
        string icone = "bi-award-fill";
        string cor = "#3b82f6";
        string badgeNome = $"Especialista: {modulo.Titulo}";

        if (tit.Contains("linux") || tit.Contains("terminal"))
        {
            icone = "bi-terminal-fill";
            cor = "#10b981";
            badgeNome = "Mestre do Terminal Linux";
        }
        else if (tit.Contains("tcp") || tit.Contains("redes") || tit.Contains("protocolo"))
        {
            icone = "bi-diagram-3-fill";
            cor = "#06b6d4";
            badgeNome = "Dominador de Redes & TCP/IP";
        }
        else if (tit.Contains("bash") || tit.Contains("shell"))
        {
            icone = "bi-code-square";
            cor = "#14b8a6";
            badgeNome = "Automatizador Bash Scripting";
        }
        else if (tit.Contains("python"))
        {
            icone = "bi-filetype-py";
            cor = "#3b82f6";
            badgeNome = "Desenvolvedor Python Ofensivo";
        }
        else if (tit.Contains("gathering") || tit.Contains("reconhecimento") || tit.Contains("enumera"))
        {
            icone = "bi-search";
            cor = "#8b5cf6";
            badgeNome = "Mestre do Reconhecimento (OSINT)";
        }
        else if (tit.Contains("scanning") || tit.Contains("nmap"))
        {
            icone = "bi-radar";
            cor = "#f59e0b";
            badgeNome = "Especialista em Scanning & Nmap";
        }
        else if (tit.Contains("metasploit") || tit.Contains("exploit"))
        {
            icone = "bi-lightning-charge-fill";
            cor = "#ef4444";
            badgeNome = "Operador de Exploits & Metasploit";
        }
        else if (tit.Contains("hash") || tit.Contains("senha") || tit.Contains("brute"))
        {
            icone = "bi-key-fill";
            cor = "#ec4899";
            badgeNome = "Quebrador de Hashes & Senhas";
        }
        else if (tit.Contains("buffer") || tit.Contains("assembly"))
        {
            icone = "bi-cpu-fill";
            cor = "#dc2626";
            badgeNome = "Binary Exploitation & Buffer Overflow";
        }
        else if (tit.Contains("web"))
        {
            icone = "bi-globe2";
            cor = "#6366f1";
            badgeNome = "Web Hacker Profissional (OWASP)";
        }

        return new Conquista
        {
            Codigo = $"MOD_{modulo.Id}_COMPLETED",
            Titulo = badgeNome,
            Descricao = $"Concluiu 100% das aulas do módulo '{modulo.Titulo}'.",
            Icone = icone,
            CorDestaque = cor,
            Categoria = "Modulo",
            XpRecompensa = 100,
            ModuloId = modulo.Id,
            CriadoEm = DateTime.UtcNow
        };
    }

    public static (int Nivel, string Titulo, int XpMinimo, int XpProximo) CalcularNivel(int xp)
    {
        if (xp < 300)   return (1, "Script Ingressante", 0, 300);
        if (xp < 800)   return (2, "Iniciante em Cibersegurança", 300, 800);
        if (xp < 1500)  return (3, "Operador de Reconhecimento", 800, 1500);
        if (xp < 2500)  return (4, "Especialista em Scanning & Enumeração", 1500, 2500);
        if (xp < 4000)  return (5, "Pentester Júnior", 2500, 4000);
        if (xp < 6000)  return (6, "Exploit Developer", 4000, 6000);
        if (xp < 8500)  return (7, "Web Hacker Avançado", 6000, 8500);
        if (xp < 11500) return (8, "Red Team Operator", 8500, 11500);
        if (xp < 15000) return (9, "Active Directory Infiltrator", 11500, 15000);

        return (10, "Master Cyber Guardian", 15000, 20000);
    }
}
