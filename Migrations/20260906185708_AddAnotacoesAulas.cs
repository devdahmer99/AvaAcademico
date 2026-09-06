using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AvaAcademico.Migrations
{
    /// <inheritdoc />
    public partial class AddAnotacoesAulas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvaAnotacoesAulas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AulaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaAnotacoesAulas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaAnotacoesAulas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvaAnotacoesAulas_Aulas_AulaId",
                        column: x => x.AulaId,
                        principalTable: "Aulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvaAnotacoesAulas_AulaId",
                table: "AvaAnotacoesAulas",
                column: "AulaId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaAnotacoesAulas_UsuarioId_AulaId",
                table: "AvaAnotacoesAulas",
                columns: new[] { "UsuarioId", "AulaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvaAnotacoesAulas");
        }
    }
}
