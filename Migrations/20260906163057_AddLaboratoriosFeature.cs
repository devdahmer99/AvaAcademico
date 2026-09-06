using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AvaAcademico.Migrations
{
    /// <inheritdoc />
    public partial class AddLaboratoriosFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvaLaboratorios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ImagemDocker = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PortaPadraoContainer = table.Column<int>(type: "int", nullable: false),
                    Flag = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Pontos = table.Column<int>(type: "int", nullable: false),
                    TempoLimiteMinutos = table.Column<int>(type: "int", nullable: false),
                    ParametrosAmbiente = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AulaId = table.Column<int>(type: "int", nullable: true),
                    ModuloId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaLaboratorios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaLaboratorios_Aulas_AulaId",
                        column: x => x.AulaId,
                        principalTable: "Aulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AvaLaboratorios_Modulos_ModuloId",
                        column: x => x.ModuloId,
                        principalTable: "Modulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AvaInstanciasLaboratorios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LaboratorioId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ContainerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PortaHost = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IniciadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Resolvido = table.Column<bool>(type: "bit", nullable: false),
                    ResolvidoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FlagSubmetida = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaInstanciasLaboratorios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaInstanciasLaboratorios_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvaInstanciasLaboratorios_AvaLaboratorios_LaboratorioId",
                        column: x => x.LaboratorioId,
                        principalTable: "AvaLaboratorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvaInstanciasLaboratorios_LaboratorioId",
                table: "AvaInstanciasLaboratorios",
                column: "LaboratorioId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaInstanciasLaboratorios_UsuarioId",
                table: "AvaInstanciasLaboratorios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaLaboratorios_AulaId",
                table: "AvaLaboratorios",
                column: "AulaId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaLaboratorios_ModuloId",
                table: "AvaLaboratorios",
                column: "ModuloId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvaInstanciasLaboratorios");

            migrationBuilder.DropTable(
                name: "AvaLaboratorios");
        }
    }
}
