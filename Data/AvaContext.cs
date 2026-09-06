using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AvaAcademico.Models;

namespace AvaAcademico.Data
{
    public class AvaContext : IdentityDbContext<ApplicationUser>
    {
        public AvaContext(DbContextOptions<AvaContext> options) : base(options)
        {
        }

        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Modulo> Modulos { get; set; }  
        public DbSet<Aula> Aulas { get; set; }
        public DbSet<Plano> Planos { get; set; }
        public DbSet<ConfiguracaoPagamento> ConfiguracoesPagamento { get; set; }
        public DbSet<Assinatura> Assinaturas { get; set; }
        public DbSet<MatriculaCurso> Matriculas { get; set; }
        public DbSet<ProgressoAula> ProgressosAulas { get; set; }
        public DbSet<Certificado> Certificados { get; set; }
        public DbSet<TopicoForum> TopicosForum { get; set; }
        public DbSet<RespostaForum> RespostasForum { get; set; }
        public DbSet<DuvidaAula> DuvidasAulas { get; set; }
        public DbSet<RespostaDuvidaAula> RespostasDuvidasAulas { get; set; }
        public DbSet<LivroBiblioteca> LivrosBiblioteca { get; set; }
        public DbSet<Laboratorio> Laboratorios { get; set; }
        public DbSet<InstanciaLaboratorio> InstanciasLaboratorios { get; set; }
        public DbSet<Conquista> Conquistas { get; set; }
        public DbSet<ConquistaUsuario> ConquistasUsuarios { get; set; }
        public DbSet<AnotacaoAula> AnotacoesAulas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FotoUrl).IsRequired(false);
                entity.Property(u => u.GoogleId).IsRequired(false);
                entity.Property(u => u.NomeCompleto).IsRequired(false);
                entity.Property(u => u.Bio).IsRequired(false);
                entity.Property(u => u.Telefone).IsRequired(false);
            });

            // Desabilita a exclusão em cascata do Módulo quando um Curso for apagado
            modelBuilder.Entity<Modulo>()
                .HasOne(m => m.Curso)
                .WithMany(c => c.Modulos)
                .HasForeignKey(m => m.CursoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Desabilita a exclusão em cascata da Aula quando um Módulo for apagado
            modelBuilder.Entity<Aula>()
                .HasOne(a => a.Modulo)
                .WithMany(m => m.Aulas)
                .HasForeignKey(a => a.ModuloId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mapeamento de tabelas para evitar conflito com tabelas existentes do ERP (ex: PLANOS)
            modelBuilder.Entity<Plano>().ToTable("AvaPlanos");
            modelBuilder.Entity<Assinatura>().ToTable("AvaAssinaturas");

            // Relacionamentos de Assinatura
            modelBuilder.Entity<Assinatura>()
                .HasOne(a => a.Usuario)
                .WithMany(u => u.Assinaturas)
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assinatura>()
                .HasOne(a => a.Plano)
                .WithMany(p => p.Assinaturas)
                .HasForeignKey(a => a.PlanoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacionamentos de Matrícula
            modelBuilder.Entity<MatriculaCurso>()
                .HasOne(m => m.Usuario)
                .WithMany(u => u.Matriculas)
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MatriculaCurso>()
                .HasOne(m => m.Curso)
                .WithMany(c => c.Matriculas)
                .HasForeignKey(m => m.CursoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacionamentos de Progresso individual
            modelBuilder.Entity<ProgressoAula>()
                .HasOne(p => p.Usuario)
                .WithMany(u => u.AulasConcluidas)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProgressoAula>()
                .HasOne(p => p.Aula)
                .WithMany(a => a.Progressos)
                .HasForeignKey(p => p.AulaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacionamentos de Certificados
            modelBuilder.Entity<Certificado>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Certificados)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Certificado>()
                .HasOne(c => c.Curso)
                .WithMany(cr => cr.Certificados)
                .HasForeignKey(c => c.CursoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Certificado>()
                .HasIndex(c => c.CodigoAutenticidade)
                .IsUnique();

            // Relacionamentos do Fórum
            modelBuilder.Entity<TopicoForum>()
                .HasOne(t => t.Curso)
                .WithMany(c => c.TopicosForum)
                .HasForeignKey(t => t.CursoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TopicoForum>()
                .HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RespostaForum>()
                .HasOne(r => r.TopicoForum)
                .WithMany(t => t.Respostas)
                .HasForeignKey(r => r.TopicoForumId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RespostaForum>()
                .HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacionamentos de Dúvidas na Aula
            modelBuilder.Entity<DuvidaAula>()
                .HasOne(d => d.Aula)
                .WithMany(a => a.Duvidas)
                .HasForeignKey(d => d.AulaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DuvidaAula>()
                .HasOne(d => d.Usuario)
                .WithMany()
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RespostaDuvidaAula>()
                .HasOne(r => r.DuvidaAula)
                .WithMany(d => d.Respostas)
                .HasForeignKey(r => r.DuvidaAulaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RespostaDuvidaAula>()
                .HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacionamentos e Mapeamento de Laboratórios
            modelBuilder.Entity<Laboratorio>().ToTable("AvaLaboratorios");
            modelBuilder.Entity<InstanciaLaboratorio>().ToTable("AvaInstanciasLaboratorios");

            modelBuilder.Entity<Laboratorio>()
                .HasOne(l => l.Aula)
                .WithMany()
                .HasForeignKey(l => l.AulaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Laboratorio>()
                .HasOne(l => l.Modulo)
                .WithMany()
                .HasForeignKey(l => l.ModuloId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InstanciaLaboratorio>()
                .HasOne(i => i.Laboratorio)
                .WithMany(l => l.Instancias)
                .HasForeignKey(i => i.LaboratorioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InstanciaLaboratorio>()
                .HasOne(i => i.Usuario)
                .WithMany()
                .HasForeignKey(i => i.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacionamentos e Mapeamento de Gamificação e Conquistas
            modelBuilder.Entity<Conquista>().ToTable("AvaConquistas");
            modelBuilder.Entity<ConquistaUsuario>().ToTable("AvaConquistasUsuarios");

            modelBuilder.Entity<ConquistaUsuario>()
                .HasIndex(cu => new { cu.UsuarioId, cu.ConquistaId })
                .IsUnique();

            modelBuilder.Entity<Conquista>()
                .HasOne(c => c.Modulo)
                .WithMany()
                .HasForeignKey(c => c.ModuloId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Conquista>()
                .HasOne(c => c.Curso)
                .WithMany()
                .HasForeignKey(c => c.CursoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ConquistaUsuario>()
                .HasOne(cu => cu.Usuario)
                .WithMany(u => u.Conquistas)
                .HasForeignKey(cu => cu.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConquistaUsuario>()
                .HasOne(cu => cu.Conquista)
                .WithMany(c => c.Usuarios)
                .HasForeignKey(cu => cu.ConquistaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacionamento e Mapeamento de Anotações de Aulas
            modelBuilder.Entity<AnotacaoAula>().ToTable("AvaAnotacoesAulas");

            modelBuilder.Entity<AnotacaoAula>()
                .HasIndex(a => new { a.UsuarioId, a.AulaId })
                .IsUnique();

            modelBuilder.Entity<AnotacaoAula>()
                .HasOne(a => a.Aula)
                .WithMany()
                .HasForeignKey(a => a.AulaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnotacaoAula>()
                .HasOne(a => a.Usuario)
                .WithMany()
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
