using System.ComponentModel.DataAnnotations;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AvaAcademico.Pages.Conta;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    [Required(ErrorMessage = "Informe seu e-mail ou usuario.")]
    public string Usuario { get; set; } = string.Empty;

    [BindProperty]
    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Informe a sua senha.")]
    public string Senha { get; set; } = string.Empty;

    [BindProperty]
    public bool LembrarMe { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usuario = await _userManager.FindByEmailAsync(Usuario) ?? await _userManager.FindByNameAsync(Usuario);
        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha invalidos.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(usuario, Senha, LembrarMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return RedirectToPage("/Index");
        }

        ModelState.AddModelError(string.Empty, "E-mail ou senha invalidos.");
        return Page();
    }
}
