using AvaAcademico.Data;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Pages.Admin.Laboratorios;

[Authorize(Roles = "Administrador")]
public class IndexModel : PageModel
{
    private readonly AvaContext _context;
    private readonly Services.IDockerService _dockerService;

    public IndexModel(AvaContext context, Services.IDockerService dockerService)
    {
        _context = context;
        _dockerService = dockerService;
    }

    public List<Laboratorio> Laboratorios { get; set; } = new();
    public List<InstanciaLaboratorio> InstanciasAtivas { get; set; } = new();
    public bool DockerAtivo { get; set; }

    public async Task OnGetAsync()
    {
        DockerAtivo = await _dockerService.IsDockerDisponivelAsync();

        Laboratorios = await _context.Laboratorios
            .Include(l => l.Aula)
            .Include(l => l.Modulo)
            .Include(l => l.Instancias)
            .OrderByDescending(l => l.CriadoEm)
            .ToListAsync();

        InstanciasAtivas = await _context.InstanciasLaboratorios
            .Include(i => i.Laboratorio)
            .Include(i => i.Usuario)
            .Where(i => i.Status == "Executando" && i.ExpiraEm > DateTime.UtcNow)
            .OrderByDescending(i => i.IniciadoEm)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var lab = await _context.Laboratorios.FindAsync(id);
        if (lab != null)
        {
            _context.Laboratorios.Remove(lab);
            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = "Laboratório excluído com sucesso!";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPararInstanciaAsync(int instanciaId)
    {
        var inst = await _context.InstanciasLaboratorios.FindAsync(instanciaId);
        if (inst != null)
        {
            await _dockerService.PararLaboratorioAsync(inst.ContainerId);
            TempData["MensagemSucesso"] = $"Container {inst.ContainerId} finalizado com sucesso!";
        }
        return RedirectToPage();
    }
}

