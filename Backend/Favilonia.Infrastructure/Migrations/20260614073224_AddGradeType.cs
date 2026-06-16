using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favilonia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GradeType",
                table: "Grades",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GradeType",
                table: "Grades");
        }
    }
}
