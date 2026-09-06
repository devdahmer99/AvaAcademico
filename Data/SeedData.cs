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

        // 1. Garante que todas as migrações pendentes sejam aplicadas automaticamente
        await context.Database.MigrateAsync();

        // 2. Perfis de Acesso (Roles)
        await EnsureRoleAsync(roleManager, "Administrador");
        await EnsureRoleAsync(roleManager, "Aluno");

        // 3. Usuário Administrador (para gestão de cursos, módulos, etc.)
        var adminEmail = "admin@ava.local";
        var adminPassword = "Admin@12345!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail) ?? await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                NomeCompleto = "Administrador do Sistema",
                CriadoEm = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrador");
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(adminUser, "Administrador"))
            {
                await userManager.AddToRoleAsync(adminUser, "Administrador");
            }
        }

        // 4. Usuário Aluno: Eduardo Dahmer Correa
        var alunoEmail = "eduardodahmer99@gmail.com";
        var alunoPassword = "12345678";

        var alunoUser = await userManager.FindByEmailAsync(alunoEmail) ?? await userManager.FindByNameAsync(alunoEmail);
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

            var result = await userManager.CreateAsync(alunoUser, alunoPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(alunoUser, "Aluno");
            }
        }
        else
        {
            alunoUser.NomeCompleto = "Eduardo Dahmer Correa";
            await userManager.UpdateAsync(alunoUser);

            if (!await userManager.IsInRoleAsync(alunoUser, "Aluno"))
            {
                await userManager.AddToRoleAsync(alunoUser, "Aluno");
            }
        }

        // 5. Inicialização automática de Badges e Conquistas de Módulos
        var gamificacaoService = scope.ServiceProvider.GetRequiredService<AvaAcademico.Services.IGamificacaoService>();
        var cursos = await context.Cursos.Select(c => c.Id).ToListAsync();
        foreach (var cId in cursos)
        {
            await gamificacaoService.GerarConquistasPadraoModulosAsync(cId);
        }

        // 6. Inicialização de Laboratórios Ofensivos com Cenários Reais e Desafiadores
        await InicializarLaboratoriosOfensivosAsync(context);
    }

    private static async Task InicializarLaboratoriosOfensivosAsync(AvaContext context)
    {
        var modulos = await context.Modulos.ToListAsync();
        if (modulos.Count == 0) return;

        // 1. Módulo 07: Dominando o terminal do Linux (PrivEsc SUID)
        var mod7 = modulos.FirstOrDefault(m => m.Ordem == 7 || m.Titulo.ToLower().Contains("terminal do linux"));
        if (mod7 != null)
        {
            var lab = await context.Laboratorios.FirstOrDefaultAsync(l => l.ModuloId == mod7.Id);
            if (lab == null)
            {
                lab = new Laboratorio { ModuloId = mod7.Id, CriadoEm = DateTime.UtcNow };
                context.Laboratorios.Add(lab);
            }

            lab.Titulo = "Operação RootBreak: Escalação de Privilégios Linux (SUID)";
            lab.Dificuldade = "Intermediário";
            lab.VetorAtaque = "Privilege Escalation (SUID)";
            lab.ImagemDocker = "tsl0922/ttyd:alpine";
            lab.PortaPadraoContainer = 7681;
            lab.Flag = "DESEC{linux_privesc_suid_gtfobins_master}";
            lab.Pontos = 150;
            lab.TempoLimiteMinutos = 60;
            lab.Ativo = true;
            lab.CenarioBriefing = "🎯 CONTEXTO DA MISSÃO:\nVocê obteve acesso inicial ao servidor interno da corporação AlfaSec através de um vetor de engenharia social. Suas credenciais atuais são de baixo privilégio ('pentester').\n\n🛡️ REGRAS DE ENGAJAMENTO:\n• O servidor não permite autenticação direta como root.\n• Seu objetivo de teste de intrusão é demonstrar impacto crítico obtendo controle total (Root) do sistema operacional.\n• A flag confidencial está armazenada em /root/flag.txt (com permissões estritas 0400 root:root).\n\n⚡ VETOR DE ATAQUE SUGERIDO:\nEnumere permissões procurando binários com bit SUID (Set User ID) ativado, que executam com privilégios do proprietário root.";
            lab.Pistas = "Enumere todos os binários do sistema que possuem o bit SUID ativado usando o comando: find / -perm -4000 2>/dev/null ||| Observe se há algum utilitário incomum na pasta /usr/local/bin ou com nome suspeito como 'backup-tool' ||| O binário encontrado é uma cópia funcional de um utilitário do sistema. Você pode utilizá-lo para executar comandos ou invocar um shell interativo herdando os privilégios de root, consultando o repositório GTFOBins!";
            lab.Writeup = "ANÁLISE TÉCNICA DA EXPLORAÇÃO:\n1. Enumeração: O comando 'find / -perm -4000 2>/dev/null' revelou o binário /usr/local/bin/backup-tool com permissões 4755 (SUID root).\n2. Exploração: Como o binário permitia execução de comandos, foi possível ler diretamente a flag com '/usr/local/bin/backup-tool cat /root/flag.txt' ou invocar um subshell com '/usr/local/bin/backup-tool sh'.\n\nREMEDIAÇÃO DEFENSIVA (HARDENING):\n• Nunca atribua o bit SUID a interpretadores de comandos ou utilitários que permitam ler arquivos arbitrários.\n• Audite periodicamente binários SUID com ferramentas como Lynis ou auditd.\n• Remova o bit SUID com: chmod u-s /usr/local/bin/backup-tool";
            lab.ComandoCustomizado = "sh -c \"adduser -D -s /bin/bash pentester && echo 'DESEC{linux_privesc_suid_gtfobins_master}' > /root/flag.txt && chmod 400 /root/flag.txt && cp /bin/busybox /usr/local/bin/backup-tool && chmod 4755 /usr/local/bin/backup-tool && echo -e '=== DESAFIO OFENSIVO: OPERAÇÃO ROOTBREAK ===\\nVocê acessou como usuário pentester (baixo privilégio).\\nA flag está protegida em /root/flag.txt.\\nEncontre o binário com bit SUID ativado no sistema para elevar privilégios para root e ler a flag!\\n' > /home/pentester/README.txt && su - pentester -c 'ttyd -W -p 7681 bash'\"";
        }

        // 2. Módulo 10: Análise de Logs (Forense de Intrusão Web)
        var mod10 = modulos.FirstOrDefault(m => m.Ordem == 10 || m.Titulo.ToLower().Contains("análise de logs"));
        if (mod10 != null)
        {
            var lab = await context.Laboratorios.FirstOrDefaultAsync(l => l.ModuloId == mod10.Id);
            if (lab == null)
            {
                lab = new Laboratorio { ModuloId = mod10.Id, CriadoEm = DateTime.UtcNow };
                context.Laboratorios.Add(lab);
            }

            lab.Titulo = "Operação CyberTrace: Análise Forense de Intrusão Web";
            lab.Dificuldade = "Intermediário";
            lab.VetorAtaque = "Forense de Logs & Threat Hunting";
            lab.ImagemDocker = "tsl0922/ttyd:alpine";
            lab.PortaPadraoContainer = 7681;
            lab.Flag = "DESEC{analyse_logs_forensics_master}";
            lab.Pontos = 150;
            lab.TempoLimiteMinutos = 60;
            lab.Ativo = true;
            lab.CenarioBriefing = "🎯 CONTEXTO DA MISSÃO:\nO SOC detectou uma anomalia em um servidor de produção Apache. Um atacante não identificado explorou uma vulnerabilidade web, injetou um webshell e exfiltrou uma credencial secreta.\n\n🛡️ REGRAS DE ENGAJAMENTO:\n• Os logs de acesso estão armazenados em /var/log/apache2/access.log.\n• Há milhares de requisições de tráfego legítimo misturadas com as ações do invasor.\n• Seu objetivo é investigar o rastro do invasor, isolar o IP suspeito, descobrir qual payload ele executou e reconstruir a flag secreta.\n\n⚡ VETOR DE INVESTIGAÇÃO:\nUtilize comandos Linux como grep, awk, cut, sort, uniq e decodificadores base64.";
            lab.Pistas = "Filtre as requisições em /var/log/apache2/access.log procurando por códigos HTTP 200 combinados com requisições POST suspeitas ou caminhos como /uploads/ ||| Observe os parâmetros passados na URL do arquivo web shell, especialmente parâmetros como ?cmd= ||| O invasor executou um comando echo passando uma string em Base64 e enviando para o comando base64 -d. Copie a string e decodifique-a usando: echo 'STRING' | base64 -d";
            lab.Writeup = "ANÁLISE FORENSE PASSO A PASSO:\n1. Investigação do IP atacante: 'cat /var/log/apache2/access.log | awk \"{print \\$1}\" | sort | uniq -c | sort -nr' apontou o IP 192.168.1.137 realizando varredura agressiva.\n2. Identificação do Web Shell: O atacante enviou um arquivo shell.php para /uploads/ e passou comandos com ?cmd=.\n3. Decodificação: O comando 'echo REVTRUN7YW5hbHlzZV9sb2dzX2ZvcmVuc2ljc19tYXN0ZXJ9 | base64 -d' revelou a flag exfiltrada.\n\nREMEDIAÇÃO DEFENSIVA:\n• Desabilitar execução de scripts PHP no diretório de uploads via configuração do Apache/Nginx.\n• Implementar WAF (Web Application Firewall) para barrar injeções de comandos.\n• Centralizar logs em tempo real para um SIEM.";
            lab.ComandoCustomizado = "sh -c \"mkdir -p /var/log/apache2 && adduser -D -s /bin/bash analista && for i in $(seq 1 40); do echo \\\"10.0.0.$((i % 15 + 1)) - - [06/Sep/2026:14:$((i/2)):10 +0000] \\\\\\\"GET /assets/style.css HTTP/1.1\\\\\\\" 200 4520\\\" >> /var/log/apache2/access.log; echo \\\"10.0.0.$((i % 15 + 1)) - - [06/Sep/2026:14:$((i/2)):15 +0000] \\\\\\\"GET /index.php HTTP/1.1\\\\\\\" 200 12840\\\" >> /var/log/apache2/access.log; done && echo '192.168.1.137 - - [06/Sep/2026:14:22:01 +0000] \\\"POST /login.php HTTP/1.1\\\" 401 180' >> /var/log/apache2/access.log && echo '192.168.1.137 - - [06/Sep/2026:14:23:45 +0000] \\\"POST /login.php HTTP/1.1\\\" 200 3200' >> /var/log/apache2/access.log && echo '192.168.1.137 - - [06/Sep/2026:14:25:12 +0000] \\\"POST /upload.php HTTP/1.1\\\" 200 840' >> /var/log/apache2/access.log && echo '192.168.1.137 - - [06/Sep/2026:14:26:00 +0000] \\\"GET /uploads/shell.php?cmd=whoami HTTP/1.1\\\" 200 24' >> /var/log/apache2/access.log && echo '192.168.1.137 - - [06/Sep/2026:14:27:30 +0000] \\\"GET /uploads/shell.php?cmd=echo%20REVTRUN7YW5hbHlzZV9sb2dzX2ZvcmVuc2ljc19tYXN0ZXJ9%20|%20base64%20-d HTTP/1.1\\\" 200 45' >> /var/log/apache2/access.log && echo -e '=== DESAFIO OFENSIVO: OPERAÇÃO CYBERTRACE ===\\nO servidor web foi comprometido por um invasor.\\nAnalise o arquivo /var/log/apache2/access.log com grep, awk e decodificadores para encontrar a flag exfiltrada!\\n' > /home/analista/README.txt && su - analista -c 'ttyd -W -p 7681 bash'\"";
        }

        // 3. Módulo 20: Information Gathering - WEB (Cloud Pentest - AWS S3)
        var mod20 = modulos.FirstOrDefault(m => m.Ordem == 20 || m.Titulo.ToLower().Contains("information gathering - web"));
        if (mod20 != null)
        {
            var lab = await context.Laboratorios.FirstOrDefaultAsync(l => l.ModuloId == mod20.Id);
            if (lab == null)
            {
                lab = new Laboratorio { ModuloId = mod20.Id, CriadoEm = DateTime.UtcNow };
                context.Laboratorios.Add(lab);
            }

            lab.Titulo = "Operação CloudLeak: Enumeração e Invasão de Buckets AWS S3";
            lab.Dificuldade = "Intermediário";
            lab.VetorAtaque = "Cloud Pentest (AWS S3 Leakage)";
            lab.ImagemDocker = "floci/floci:latest";
            lab.PortaPadraoContainer = 4566;
            lab.Flag = "DESEC{s3_bucket_public_leak_captured}";
            lab.Pontos = 150;
            lab.TempoLimiteMinutos = 60;
            lab.Ativo = true;
            lab.CenarioBriefing = "🎯 CONTEXTO DA MISSÃO:\nA corporação fictícia NexusCloud utiliza infraestrutura AWS para seus serviços digitais. Durante a fase de Information Gathering WEB, você deve identificar buckets S3 mal configurados com permissões públicas de leitura que estejam expondo dados confidenciais.\n\n🛡️ REGRAS DE ENGAJAMENTO:\n• O endpoint AWS emulado está acessível em http://localhost:[PORTA].\n• Enumere os buckets existentes e inspecione os arquivos armazenados.\n• Localize o arquivo confidencial de backup de credenciais que contém a flag de intrusão.\n\n⚡ VETOR DE ATAQUE:\nUtilize a AWS CLI, cURL ou scripts em Python/Boto3 apontando para o endpoint local:\naws --endpoint-url http://localhost:[PORTA] s3 ls\naws --endpoint-url http://localhost:[PORTA] s3 cp s3://nexus-corp-backup/credentials_dump.json .";
            lab.Pistas = "Liste todos os buckets disponíveis no endpoint local com o comando: aws --endpoint-url http://localhost:[PORTA] s3 ls (ou acesse a URL no navegador/cURL) ||| Inspecione o conteúdo do bucket corporativo: aws --endpoint-url http://localhost:[PORTA] s3 ls s3://nexus-corp-backup ||| Faça o download do arquivo de credenciais: aws --endpoint-url http://localhost:[PORTA] s3 cp s3://nexus-corp-backup/credentials_dump.json . e abra-o para capturar a flag!";
            lab.Writeup = "ANÁLISE TÉCNICA:\n1. A política do bucket S3 permitia leitura anônima sem autenticação.\n2. Através da AWS CLI apontando para o endpoint local, o atacante listou os objetos e baixou o arquivo sensível com credenciais de banco e a flag.\n\nREMEDIAÇÃO DEFENSIVA (AWS S3 HARDENING):\n• Ativar o 'Block Public Access' (S3 BPA) no nível da conta e do bucket.\n• Implementar políticas de bucket que exijam autenticação e concedam apenas privilégio mínimo (Least Privilege).\n• Ativar AWS Macie ou GuardDuty para monitorar buckets com permissão pública.";
        }

        // 4. Módulo 21: Scanning (Nmap)
        var mod21 = modulos.FirstOrDefault(m => m.Ordem == 21 || m.Titulo.ToLower().Contains("scanning"));
        if (mod21 != null)
        {
            var lab = await context.Laboratorios.FirstOrDefaultAsync(l => l.ModuloId == mod21.Id);
            if (lab == null)
            {
                lab = new Laboratorio { ModuloId = mod21.Id, CriadoEm = DateTime.UtcNow };
                context.Laboratorios.Add(lab);
            }

            lab.Titulo = "Operação GhostScan: Reconhecimento & Evasão de Filtros (Nmap)";
            lab.Dificuldade = "Iniciante";
            lab.VetorAtaque = "Network Scanning (Nmap)";
            lab.ImagemDocker = "vulnerables/web-dvwa";
            lab.PortaPadraoContainer = 80;
            lab.Flag = "DESEC{nmap_port_scan_success}";
            lab.Pontos = 150;
            lab.TempoLimiteMinutos = 60;
            lab.Ativo = true;
            lab.CenarioBriefing = "🎯 CONTEXTO DA MISSÃO:\nDurante a fase de reconhecimento externo de um pentest em rede, você deve mapear os serviços em execução no host corporativo de homologação.\n\n🛡️ REGRAS DE ENGAJAMENTO:\n• O alvo está acessível via localhost no endereço de porta indicado.\n• Execute um port scan com Nmap detalhando serviços, versões e cabeçalhos HTTP.\n• Identifique o serviço e inspecione as respostas HTTP para localizar a flag de reconhecimento.\n\n⚡ VETOR DE ATAQUE SUGERIDO:\nUtilize o Nmap com detecção de versão (-sV), scripts padrão (-sC) e inspecione os cabeçalhos HTTP com cURL ou scripts NSE.";
            lab.Pistas = "Execute um scan completo no IP/porta do alvo: nmap -sV -sC -p [PORTA] localhost ||| Inspecione a resposta HTTP completa do servidor incluindo os cabeçalhos: curl -I http://localhost:[PORTA] ||| Observe a resposta de boas-vindas do serviço na porta indicada para confirmar o sucesso do mapeamento.";
            lab.Writeup = "ANÁLISE TÉCNICA:\n1. O Nmap identificou o serviço Apache rodando na porta alocada.\n2. Uma requisição com curl -I revelou os cabeçalhos da aplicação e confirmou os serviços operantes.\n\nREMEDIAÇÃO DEFENSIVA:\n• Pratique hardening de cabeçalhos HTTP (ServerTokens Prod e ServerSignature Off).\n• Bloqueie varreduras com regras de rate limiting no firewall iptables/UFW.";
        }

        // 4. Módulo 27: Hashes e Senhas Linux
        var mod27 = modulos.FirstOrDefault(m => m.Ordem == 27 || m.Titulo.ToLower().Contains("hashes e senhas - linux"));
        if (mod27 != null)
        {
            var lab = await context.Laboratorios.FirstOrDefaultAsync(l => l.ModuloId == mod27.Id);
            if (lab == null)
            {
                lab = new Laboratorio { ModuloId = mod27.Id, CriadoEm = DateTime.UtcNow };
                context.Laboratorios.Add(lab);
            }

            lab.Titulo = "Operação HashCracker: Quebra de Hashes /etc/shadow com Dicionário";
            lab.Dificuldade = "Intermediário";
            lab.VetorAtaque = "Password Cracking & Hashes";
            lab.ImagemDocker = "tsl0922/ttyd:alpine";
            lab.PortaPadraoContainer = 7681;
            lab.Flag = "DESEC{shadow_hashcat_john_cracked}";
            lab.Pontos = 150;
            lab.TempoLimiteMinutos = 60;
            lab.Ativo = true;
            lab.CenarioBriefing = "🎯 CONTEXTO DA MISSÃO:\nVocê obteve uma cópia não autorizada do arquivo /etc/shadow de um servidor legado durante um teste interno. Seu objetivo é identificar o algoritmo do hash e recuperar a senha do administrador.\n\n🛡️ REGRAS DE ENGAJAMENTO:\n• O arquivo de hashes está disponível no ambiente em /home/pentester/shadow_dump.txt.\n• A wordlist de senhas comuns está em /home/pentester/wordlist.txt.\n• Quebre o hash e encontre a senha para validar o desafio.\n\n⚡ VETOR DE ATAQUE:\nIdentifique a cifra do hash ($6$ = SHA-512 crypt, $1$ = MD5) e execute o ataque de dicionário.";
            lab.Pistas = "Abra o arquivo /home/pentester/shadow_dump.txt para inspecionar os hashes armazenados ||| Observe a wordlist em /home/pentester/wordlist.txt ||| A senha do usuário root que corresponde ao hash quebrado é: desec12345. A flag de validação é DESEC{shadow_hashcat_john_cracked}!";
            lab.Writeup = "ANÁLISE TÉCNICA:\n1. O hash no shadow dump usava o salt $6$ (SHA-512 crypt).\n2. O ataque de dicionário identificou a senha fraca 'desec12345'.\n\nREMEDIAÇÃO DEFENSIVA:\n• Impor política de senhas fortes (mínimo de 14 caracteres, complexidade).\n• Adotar autenticação multifator (MFA/2FA).\n• Proteger arquivos /etc/shadow com permissão estrita 0640 ou 0600.";
            lab.ComandoCustomizado = "sh -c \"adduser -D -s /bin/bash pentester && echo 'root:\\$6\\$saltkey\\$L03sJmF01jFkZJq98Q6a2P4E2nL8N2Y0G9H7I6J5K4L3M2N1O0P9Q8R7S6T5U4V3W2X1Y0Z9A8B7C6D5E4F3:19200:0:99999:7:::' > /home/pentester/shadow_dump.txt && echo -e 'admin\\n123456\\npassword\\nsecret\\ndesec12345\\nqwerty\\nletmein' > /home/pentester/wordlist.txt && echo -e '=== DESAFIO: OPERAÇÃO HASHCRACKER ===\\nVocê obteve um dump de hash do arquivo /etc/shadow em shadow_dump.txt.\\nUse ferramentas ou scripts com a wordlist.txt fornecida para quebrar a senha do root!\\nA flag tem o formato: DESEC{shadow_hashcat_john_cracked}\\n' > /home/pentester/README.txt && su - pentester -c 'ttyd -W -p 7681 bash'\"";
        }

        // 5. Módulo 38: Pentest Web (OWASP Juice Shop / DVWA)
        var mod38 = modulos.FirstOrDefault(m => m.Ordem == 38 || m.Titulo.ToLower().Contains("pentest web"));
        if (mod38 != null)
        {
            var lab = await context.Laboratorios.FirstOrDefaultAsync(l => l.ModuloId == mod38.Id);
            if (lab == null)
            {
                lab = new Laboratorio { ModuloId = mod38.Id, CriadoEm = DateTime.UtcNow };
                context.Laboratorios.Add(lab);
            }

            lab.Titulo = "Operação WebStrike: OWASP Top 10 Exploitation (Juice Shop)";
            lab.Dificuldade = "Avançado";
            lab.VetorAtaque = "Web Hacking (SQLi, XSS, Broken Auth)";
            lab.ImagemDocker = "bkimminich/juice-shop";
            lab.PortaPadraoContainer = 3000;
            lab.Flag = "DESEC{owasp_web_hacking_flag}";
            lab.Pontos = 200;
            lab.TempoLimiteMinutos = 90;
            lab.Ativo = true;
            lab.CenarioBriefing = "🎯 CONTEXTO DA MISSÃO:\nA plataforma de e-commerce Juice Shop está sendo submetida a um pentest Web completo. Você deve encontrar falhas da OWASP Top 10 para obter acesso não autorizado como administrador e recuperar o token secreto.\n\n🛡️ REGRAS DE ENGAJAMENTO:\n• Alvo exposto na porta HTTP indicada.\n• Intercepte o tráfego com Burp Suite ou OWASP ZAP.\n• Explore a tela de login utilizando SQL Injection (' or 1=1--) para fazer login como admin@juice-sh.op sem senha, ou capture o token de pontuação no Score Board.\n\n⚡ VETOR DE ATAQUE:\nSQL Injection em formulários de autenticação, inspeção de requisições de API REST e manipulação de tokens.";
            lab.Pistas = "No formulário de Login, tente injetar payloads clássicos de SQL Injection no campo de e-mail, como: ' or 1=1-- ||| A senha pode ser qualquer valor aleatório quando o SQLi comenta o restante da query com '--' ||| Verifique a rota oculta /#/score-board inspecionando o código-fonte JavaScript da aplicação ou resolvendo o desafio no painel de administração!";
            lab.Writeup = "ANÁLISE TÉCNICA:\n1. Vulnerabilidade: A query de autenticação no backend concatenava strings sem parametrização.\n2. Impacto: Qualquer usuário pode assumir a identidade do administrador sem conhecer a senha.\n\nREMEDIAÇÃO DEFENSIVA:\n• Uso obrigatório de Prepared Statements (Consultas Parametrizadas) ou ORMs seguros.\n• Implementação de validação de entrada e sanitização de dados no lado servidor.";
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

