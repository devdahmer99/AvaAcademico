using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AvaAcademico.Migrations
{
    /// <inheritdoc />
    public partial class AddGamificacaoEBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PontosXp",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AvaConquistas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Icone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CorDestaque = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    XpRecompensa = table.Column<int>(type: "int", nullable: false),
                    ModuloId = table.Column<int>(type: "int", nullable: true),
                    CursoId = table.Column<int>(type: "int", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaConquistas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaConquistas_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AvaConquistas_Modulos_ModuloId",
                        column: x => x.ModuloId,
                        principalTable: "Modulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AvaConquistasUsuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConquistaId = table.Column<int>(type: "int", nullable: false),
                    DesbloqueadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaConquistasUsuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaConquistasUsuarios_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvaConquistasUsuarios_AvaConquistas_ConquistaId",
                        column: x => x.ConquistaId,
                        principalTable: "AvaConquistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvaConquistas_CursoId",
                table: "AvaConquistas",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaConquistas_ModuloId",
                table: "AvaConquistas",
                column: "ModuloId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaConquistasUsuarios_ConquistaId",
                table: "AvaConquistasUsuarios",
                column: "ConquistaId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaConquistasUsuarios_UsuarioId_ConquistaId",
                table: "AvaConquistasUsuarios",
                columns: new[] { "UsuarioId", "ConquistaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvaConquistasUsuarios");

            migrationBuilder.DropTable(
                name: "AvaConquistas");

            migrationBuilder.DropColumn(
                name: "PontosXp",
                table: "AspNetUsers");
        }
    }
}
