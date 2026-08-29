using AvaAcademico.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AvaAcademico.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AvaContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureRoleAsync(roleManager, "Administrador");
        await EnsureRoleAsync(roleManager, "Aluno");

        var adminEmail = "admin@ava.local";
        var adminPassword = "Admin@12345!";

        var adminUser = await userManager.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                NomeCompleto = "Administrador do Sistema",
                CriadoEm = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Administrador"))
        {
            await userManager.AddToRoleAsync(adminUser, "Administrador");
        }

        // Cadastro do Aluno Eduardo Dahmer Correa
        var alunoEmail = "eduardodahmer99@gmail.com";
        var alunoPassword = "12345678";

        var alunoUser = await userManager.Users.FirstOrDefaultAsync(u => u.Email == alunoEmail);
        if (alunoUser == null)
        {
            alunoUser = new ApplicationUser
            {
                UserName = alunoEmail,
                Email = alunoEmail,
                EmailConfirmed = true,
                NomeCompleto = "Eduardo Dahmer Correa",
                CriadoEm = DateTime.UtcNow
            };

            var createAlunoResult = await userManager.CreateAsync(alunoUser, alunoPassword);
            if (!createAlunoResult.Succeeded)
            {
                // Se falhar por algum motivo, tenta resetar
            }
        }
        else
        {
            // Garante que o nome e senha estejam atualizados
            alunoUser.NomeCompleto = "Eduardo Dahmer Correa";
            await userManager.UpdateAsync(alunoUser);
            
            var token = await userManager.GeneratePasswordResetTokenAsync(alunoUser);
            await userManager.ResetPasswordAsync(alunoUser, token, alunoPassword);
        }

        if (alunoUser != null && !await userManager.IsInRoleAsync(alunoUser, "Aluno"))
        {
            await userManager.AddToRoleAsync(alunoUser, "Aluno");
        }

        // Garante a existência do Curso de Pentest
        var cursoPentest = await context.Cursos
            .Include(c => c.Modulos)
            .ThenInclude(m => m.Aulas)
            .FirstOrDefaultAsync(c => c.Titulo.ToLower().Contains("pentest"));

        if (cursoPentest == null)
        {
            cursoPentest = new Curso
            {
                Titulo = "Formação Completa em Pentest & Segurança Ofensiva",
                Descricao = "Aprenda técnicas profissionais de teste de invasão (Penetration Testing), reconhecimento, varredura de vulnerabilidades, exploração, pós-exploração e elaboração de relatórios técnicos.",
                Categoria = "Cibersegurança",
                CargaHorariaHoras = 40,
                CriadoEm = DateTime.UtcNow,
                Modulos = new List<Modulo>
                {
                    new Modulo
                    {
                        Titulo = "Fundamentos do Pentest e Metodologias",
                        Ordem = 1,
                        Aulas = new List<Aula>
                        {
                            new Aula
                            {
                                Titulo = "Introdução ao Pentest e Ética Hacker",
                                Ordem = 1,
                                NomeArquivoVideo = "exemplo.mp4"
                            },
                            new Aula
                            {
                                Titulo = "Preparando o Laboratório com Kali Linux",
                                Ordem = 2,
                                NomeArquivoVideo = "exemplo.mp4"
                            }
                        }
                    },
                    new Modulo
                    {
                        Titulo = "Reconhecimento, Scanning e Enumeração",
                        Ordem = 2,
                        Aulas = new List<Aula>
                        {
                            new Aula
                            {
                                Titulo = "Varredura Avançada com Nmap",
                                Ordem = 1,
                                NomeArquivoVideo = "exemplo.mp4"
                            },
                            new Aula
                            {
                                Titulo = "Enumeração de Serviços e Identificação de Falhas",
                                Ordem = 2,
                                NomeArquivoVideo = "exemplo.mp4"
                            }
                        }
                    }
                }
            };

            context.Cursos.Add(cursoPentest);
            await context.SaveChangesAsync();
        }

        // Matrícula ativa do aluno Eduardo no Curso de Pentest
        if (alunoUser != null && cursoPentest != null)
        {
            var jaMatriculado = await context.Matriculas
                .AnyAsync(m => m.UsuarioId == alunoUser.Id && m.CursoId == cursoPentest.Id);

            if (!jaMatriculado)
            {
                context.Matriculas.Add(new MatriculaCurso
                {
                    UsuarioId = alunoUser.Id,
                    CursoId = cursoPentest.Id,
                    DataMatricula = DateTime.UtcNow,
                    Status = "Ativa"
                });
                await context.SaveChangesAsync();
            }

            // Garante uma assinatura ativa para o aluno
            var planoAnual = await context.Planos.FirstOrDefaultAsync(p => p.IntervaloMeses == 12) 
                             ?? await context.Planos.FirstOrDefaultAsync();

            if (planoAnual != null)
            {
                var temAssinatura = await context.Assinaturas
                    .AnyAsync(a => a.UsuarioId == alunoUser.Id && a.Status == "Ativa");

                if (!temAssinatura)
                {
                    context.Assinaturas.Add(new Assinatura
                    {
                        UsuarioId = alunoUser.Id,
                        PlanoId = planoAnual.Id,
                        DataInicio = DateTime.UtcNow,
                        DataFim = DateTime.UtcNow.AddYears(1),
                        Status = "Ativa",
                        MetodoPagamento = "Pix",
                        ValorPago = planoAnual.Preco
                    });
                    await context.SaveChangesAsync();
                }
            }
        }

        // 1. Seed Planos Padrão
        if (!await context.Planos.AnyAsync())
        {
            context.Planos.AddRange(
                new Plano
                {
                    Nome = "Plano Mensal",
                    Descricao = "Acesso completo a todas as trilhas e materiais com renovação mês a mês.",
                    Preco = 49.90m,
                    IntervaloMeses = 1,
                    Destaque = false,
                    Beneficios = "Acesso a todos os cursos\nAcesso ao fórum e dúvidas com instrutores\nBiblioteca digital em PDF ilimitada\nEmissão de certificados digitais",
                    Ativo = true,
                    CriadoEm = DateTime.UtcNow
                },
                new Plano
                {
                    Nome = "Plano Pro Anual (Recomendado)",
                    Descricao = "O plano mais econômico e completo com suporte prioritário e certificados ilimitados.",
                    Preco = 399.00m,
                    IntervaloMeses = 12,
                    Destaque = true,
                    Beneficios = "Acesso a todos os cursos por 1 ano\nAcesso a novos cursos lançados\nAcesso prioritário às respostas no fórum\nBiblioteca digital completa com download\nCertificados com validação pública oficial\nEconomia de mais de 30% em relação ao mensal",
                    Ativo = true,
                    CriadoEm = DateTime.UtcNow
                },
                new Plano
                {
                    Nome = "Plano Trimestral",
                    Descricao = "Perfeito para quem deseja focar intensamente em uma formação específica.",
                    Preco = 129.90m,
                    IntervaloMeses = 3,
                    Destaque = false,
                    Beneficios = "Acesso total por 3 meses\nAcesso à biblioteca virtual\nEmissão de certificados com autenticidade\nSuporte a dúvidas por aula",
                    Ativo = true,
                    CriadoEm = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
        }

        // 2. Seed Configuração de Pagamento Padrão
        if (!await context.ConfiguracoesPagamento.AnyAsync())
        {
            context.ConfiguracoesPagamento.Add(new ConfiguracaoPagamento
            {
                ProvedorPadrao = "PixDireto",
                ChavePix = "contato@ejlacademy.com.br",
                TipoChavePix = "Email",
                NomeBeneficiarioPix = "EJL Academy Educacional",
                CidadeBeneficiarioPix = "Sao Paulo",
                ModoSandbox = true,
                InstrucoesPagamento = "Após realizar o pagamento via Pix utilizando o QR Code ou Copia e Cola, o seu acesso ao ambiente acadêmico é liberado instantaneamente.",
                AtualizadoEm = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // 3. Seed Livros da Biblioteca Virtual
        if (!await context.LivrosBiblioteca.AnyAsync())
        {
            context.LivrosBiblioteca.AddRange(
                new LivroBiblioteca
                {
                    Titulo = "Guia Prático de Redes e Protocolos",
                    Autor = "Equipe Pedagógica EJL",
                    Categoria = "Redes & Infraestrutura",
                    Descricao = "Fundamentos essenciais de redes TCP/IP, roteamento, VLANs e topologias corporativas modernas.",
                    NomeArquivoPdf = "guia-redes-protocolos.pdf",
                    NumeroPaginas = 148,
                    TamanhoBytes = 4500000,
                    CriadoEm = DateTime.UtcNow
                },
                new LivroBiblioteca
                {
                    Titulo = "Manual de Cibersegurança Defensiva",
                    Autor = "Prof. Eduardo & Especialistas",
                    Categoria = "Cibersegurança",
                    Descricao = "Boas práticas de defesa cibernética, análise de vulnerabilidades, hardening de servidores e gestão de incidentes.",
                    NomeArquivoPdf = "manual-ciberseguranca-defensiva.pdf",
                    NumeroPaginas = 230,
                    TamanhoBytes = 6800000,
                    CriadoEm = DateTime.UtcNow
                },
                new LivroBiblioteca
                {
                    Titulo = "Arquitetura e Desenvolvimento Web com .NET",
                    Autor = "Tech Academy",
                    Categoria = "Programação",
                    Descricao = "Padrões de projeto, Entity Framework Core, Razor Pages, APIs REST seguras e autenticação moderna com Identity.",
                    NomeArquivoPdf = "desenvolvimento-dotnet-moderno.pdf",
                    NumeroPaginas = 312,
                    TamanhoBytes = 8200000,
                    CriadoEm = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
