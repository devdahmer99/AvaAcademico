using System.Security.Claims;
using AvaAcademico.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AvaAcademico.Pages.Conta;

public class ExternalLoginCallbackModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ExternalLoginCallbackModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> OnGetAsync(string returnUrl = "/")
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToPage("/Conta/Login");
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            return LocalRedirect(returnUrl);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? info.Principal.FindFirstValue("email");
        var nomeCompleto = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email ?? "Aluno";
        var fotoUrl = info.Principal.FindFirstValue("picture") ?? string.Empty;

        ApplicationUser? usuario = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            usuario = await _userManager.FindByEmailAsync(email);
        }

        if (usuario == null)
        {
            usuario = new ApplicationUser
            {
                UserName = email ?? info.ProviderKey,
                Email = email,
                EmailConfirmed = !string.IsNullOrWhiteSpace(email),
                NomeCompleto = nomeCompleto,
                GoogleId = info.ProviderKey,
                FotoUrl = fotoUrl,
                CriadoEm = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(usuario);
            if (!createResult.Succeeded)
            {
                return RedirectToPage("/Conta/Login");
            }
        }
        else
        {
            usuario.NomeCompleto = string.IsNullOrWhiteSpace(usuario.NomeCompleto) ? nomeCompleto : usuario.NomeCompleto;
            usuario.GoogleId = info.ProviderKey;
            usuario.FotoUrl = string.IsNullOrWhiteSpace(usuario.FotoUrl) ? fotoUrl : usuario.FotoUrl;
            await _userManager.UpdateAsync(usuario);
        }

        var loginInfo = new UserLoginInfo(info.LoginProvider, info.ProviderKey, info.LoginProvider);
        var existingLogins = await _userManager.GetLoginsAsync(usuario);
        if (!existingLogins.Any(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey))
        {
            await _userManager.AddLoginAsync(usuario, loginInfo);
        }

        if (!await _roleManager.RoleExistsAsync("Aluno"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Aluno"));
        }

        if (!await _userManager.IsInRoleAsync(usuario, "Aluno"))
        {
            await _userManager.AddToRoleAsync(usuario, "Aluno");
        }

        await _signInManager.SignInAsync(usuario, isPersistent: false);
        return LocalRedirect(returnUrl);
    }
}
