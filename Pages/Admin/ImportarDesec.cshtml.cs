using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.RegularExpressions;

namespace AvaAcademico.Pages.Admin;

[Authorize(Roles = "Administrador")]
public class ImportarDesecModel : PageModel
{
    private readonly AvaContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ImportarDesecModel(AvaContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public string CaminhoDiretorio { get; set; } = @"C:\Users\Eduar\Downloads\Cópia de Desec - 2020 - NOVO PENTEST PROFISSIONAL-20260906T151005Z-1-007\Desec - 2020 - NOVO PENTEST PROFISSIONAL";

    public bool DiretorioExiste { get; set; }
    public int TotalModulosDetectados { get; set; }
    public int TotalAulasDetectadas { get; set; }
    public double TamanhoTotalGB { get; set; }
    public Curso? CursoJaImportado { get; set; }

    public void OnGet()
    {
        VerificarDiretorio();
    }

    public async Task<IActionResult> OnPostImportarAsync()
    {
        VerificarDiretorio();

        if (!DiretorioExiste)
        {
            TempData["MensagemErro"] = "O diretório informado não foi encontrado no sistema.";
            return Page();
        }

        // 1. Cria ou recupera o Curso
        var curso = await _context.Cursos
            .Include(c => c.Modulos)
                .ThenInclude(m => m.Aulas)
            .FirstOrDefaultAsync(c => c.Titulo.Contains("Novo Pentest Profissional"));

        if (curso == null)
        {
            curso = new Curso
            {
                Titulo = "Formação Novo Pentest Profissional (Desec)",
                Descricao = "Metodologia completa de Penetration Testing: Terminal Linux, Redes TCP/IP, Scripting (Bash, C, Python), Reconhecimento, Scanning, Exploitation, Quebra de Senhas, Buffer Overflow Windows/Linux, Web Hacking e Pós-Exploração.",
                Categoria = "Cibersegurança",
                CargaHorariaHoras = 120,
                CriadoEm = DateTime.UtcNow
            };
            _context.Cursos.Add(curso);
            await _context.SaveChangesAsync();
        }

        var dirInfo = new DirectoryInfo(CaminhoDiretorio);
        var subPastas = dirInfo.GetDirectories().OrderBy(d => d.Name).ToList();

        int modulosCriados = 0;
        int aulasCriadas = 0;

        int ordemModuloFallback = 1;

        foreach (var pasta in subPastas)
        {
            // Extrai ordem e título da pasta (ex: "07. Dominando o terminal do Linux")
            int ordemModulo = ExtrairOrdem(pasta.Name, ordemModuloFallback);
            string tituloModulo = LimparTitulo(pasta.Name);

            var modulo = curso.Modulos.FirstOrDefault(m => m.Titulo == tituloModulo)
                ?? await _context.Modulos.FirstOrDefaultAsync(m => m.CursoId == curso.Id && m.Titulo == tituloModulo);

            if (modulo == null)
            {
                modulo = new Modulo
                {
                    CursoId = curso.Id,
                    Titulo = tituloModulo,
                    Ordem = ordemModulo
                };
                _context.Modulos.Add(modulo);
                await _context.SaveChangesAsync();
                modulosCriados++;
            }

            // Lê arquivos .mp4 da pasta
            var videos = pasta.GetFiles("*.mp4").OrderBy(f => f.Name).ToList();
            int ordemAulaFallback = 1;

            foreach (var video in videos)
            {
                int ordemAula = ExtrairOrdem(video.Name, ordemAulaFallback);
                string tituloAula = LimparTitulo(Path.GetFileNameWithoutExtension(video.Name));

                var aulaExistente = await _context.Aulas
                    .FirstOrDefaultAsync(a => a.ModuloId == modulo.Id && (a.Titulo == tituloAula || a.NomeArquivoVideo == video.FullName));

                if (aulaExistente == null)
                {
                    var novaAula = new Aula
                    {
                        ModuloId = modulo.Id,
                        Titulo = tituloAula,
                        Ordem = ordemAula,
                        NomeArquivoVideo = video.FullName, // Caminho absoluto para streaming direto sem duplicação de disco!
                        Concluida = false
                    };
                    _context.Aulas.Add(novaAula);
                    aulasCriadas++;
                }

                ordemAulaFallback++;
            }

            ordemModuloFallback++;
        }

        await _context.SaveChangesAsync();

        // 2. Cria Laboratórios Docker de Demonstração nas Trilhas Relevantes
        await CriarLaboratoriosPadraoAsync(curso.Id);

        // 3. Garante a matrícula ativa do aluno Eduardo Dahmer Correa
        var aluno = await _userManager.FindByEmailAsync("eduardodahmer99@gmail.com");
        if (aluno != null)
        {
            var jaMatriculado = await _context.Matriculas
                .AnyAsync(m => m.UsuarioId == aluno.Id && m.CursoId == curso.Id);

            if (!jaMatriculado)
            {
                _context.Matriculas.Add(new MatriculaCurso
                {
                    UsuarioId = aluno.Id,
                    CursoId = curso.Id,
                    DataMatricula = DateTime.UtcNow,
                    Status = "Ativa"
                });
                await _context.SaveChangesAsync();
            }
        }

        TempData["MensagemSucesso"] = $"Importação concluída com sucesso! {modulosCriados} novos módulos e {aulasCriadas} aulas foram adicionadas e o aluno Eduardo já está matriculado!";
        return RedirectToPage("/Index");
    }

    private void VerificarDiretorio()
    {
        DiretorioExiste = Directory.Exists(CaminhoDiretorio);
        if (DiretorioExiste)
        {
            var dirInfo = new DirectoryInfo(CaminhoDiretorio);
            var subPastas = dirInfo.GetDirectories();
            TotalModulosDetectados = subPastas.Length;

            long totalBytes = 0;
            int videosCount = 0;
            foreach (var pasta in subPastas)
            {
                var files = pasta.GetFiles("*.mp4");
                videosCount += files.Length;
                foreach (var f in files)
                {
                    totalBytes += f.Length;
                }
            }

            TotalAulasDetectadas = videosCount;
            TamanhoTotalGB = Math.Round((double)totalBytes / (1024 * 1024 * 1024), 2);

            CursoJaImportado = _context.Cursos
                .Include(c => c.Modulos)
                    .ThenInclude(m => m.Aulas)
                .FirstOrDefault(c => c.Titulo.Contains("Novo Pentest Profissional"));
        }
    }

    private async Task CriarLaboratoriosPadraoAsync(int cursoId)
    {
        var modulos = await _context.Modulos
            .Include(m => m.Aulas)
            .Where(m => m.CursoId == cursoId)
            .ToListAsync();

        // 1. Lab Terminal Linux (Módulo 07)
        var modTerminal = modulos.FirstOrDefault(m => m.Titulo.ToLower().Contains("terminal do linux"));
        if (modTerminal != null && !await _context.Laboratorios.AnyAsync(l => l.ModuloId == modTerminal.Id))
        {
            _context.Laboratorios.Add(new Laboratorio
            {
                ModuloId = modTerminal.Id,
                Titulo = "Laboratório Prático: Comandos Essenciais Linux",
                Descricao = "Explore o ambiente Linux Alpine via shell web ou terminal e localize o arquivo com a flag de conclusão.",
                ImagemDocker = "alpine:latest",
                PortaPadraoContainer = 80,
                Flag = "DESEC{terminal_linux_master}",
                Pontos = 100,
                TempoLimiteMinutos = 60,
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            });
        }

        // 2. Lab Nmap / Scanning (Módulo 21)
        var modScanning = modulos.FirstOrDefault(m => m.Titulo.ToLower().Contains("scanning"));
        if (modScanning != null && !await _context.Laboratorios.AnyAsync(l => l.ModuloId == modScanning.Id))
        {
            _context.Laboratorios.Add(new Laboratorio
            {
                ModuloId = modScanning.Id,
                Titulo = "Laboratório Prático: Reconhecimento de Portas e Serviços (Nmap)",
                Descricao = "Realize um port scan no alvo para identificar quais serviços TCP/UDP estão rodando e identifique o serviço que retorna a flag.",
                ImagemDocker = "vulnerables/web-dvwa",
                PortaPadraoContainer = 80,
                Flag = "DESEC{nmap_port_scan_success}",
                Pontos = 150,
                TempoLimiteMinutos = 60,
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            });
        }

        // 3. Lab Web Hacking (Módulo 38)
        var modWeb = modulos.FirstOrDefault(m => m.Titulo.ToLower().Contains("pentest web"));
        if (modWeb != null && !await _context.Laboratorios.AnyAsync(l => l.ModuloId == modWeb.Id))
        {
            _context.Laboratorios.Add(new Laboratorio
            {
                ModuloId = modWeb.Id,
                Titulo = "Laboratório Prático: Exploração Web OWASP (Juice Shop / DVWA)",
                Descricao = "Ambiente vulnerável completo com desafios de SQL Injection, Cross-Site Scripting (XSS), Broken Authentication e RCE.",
                ImagemDocker = "bkimminich/juice-shop",
                PortaPadraoContainer = 3000,
                Flag = "DESEC{owasp_web_hacking_flag}",
                Pontos = 250,
                TempoLimiteMinutos = 90,
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    private static int ExtrairOrdem(string nome, int fallback)
    {
        var match = Regex.Match(nome, @"^(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int result))
        {
            return result;
        }
        return fallback;
    }

    private static string LimparTitulo(string nome)
    {
        var limpo = Regex.Replace(nome, @"^\d+[\.\-\s_]+", "").Trim();
        limpo = limpo.Replace("_", " ");
        return string.IsNullOrWhiteSpace(limpo) ? nome : limpo;
    }
}

