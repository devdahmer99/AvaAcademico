using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AvaAcademico.Migrations
{
    /// <inheritdoc />
    public partial class AddDesafiosRealistasLaboratorios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CenarioBriefing",
                table: "AvaLaboratorios",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComandoCustomizado",
                table: "AvaLaboratorios",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dificuldade",
                table: "AvaLaboratorios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Pistas",
                table: "AvaLaboratorios",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VetorAtaque",
                table: "AvaLaboratorios",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Writeup",
                table: "AvaLaboratorios",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CenarioBriefing",
                table: "AvaLaboratorios");

            migrationBuilder.DropColumn(
                name: "ComandoCustomizado",
                table: "AvaLaboratorios");

            migrationBuilder.DropColumn(
                name: "Dificuldade",
                table: "AvaLaboratorios");

            migrationBuilder.DropColumn(
                name: "Pistas",
                table: "AvaLaboratorios");

            migrationBuilder.DropColumn(
                name: "VetorAtaque",
                table: "AvaLaboratorios");

            migrationBuilder.DropColumn(
                name: "Writeup",
                table: "AvaLaboratorios");
        }
    }
}
