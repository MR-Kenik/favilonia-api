using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favilonia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodsAndFinalGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PeriodId",
                table: "Grades",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Periods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Periods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Periods_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinalGrades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinalGrades_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinalGrades_Periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "Periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinalGrades_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinalGrades_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinalGrades_Users_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Grades_PeriodId",
                table: "Grades",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalGrades_OrganizationId",
                table: "FinalGrades",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalGrades_PeriodId",
                table: "FinalGrades",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalGrades_StudentId_SubjectId_PeriodId",
                table: "FinalGrades",
                columns: new[] { "StudentId", "SubjectId", "PeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinalGrades_SubjectId",
                table: "FinalGrades",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalGrades_TeacherId",
                table: "FinalGrades",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Periods_OrganizationId",
                table: "Periods",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Periods_PeriodId",
                table: "Grades",
                column: "PeriodId",
                principalTable: "Periods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Periods_PeriodId",
                table: "Grades");

            migrationBuilder.DropTable(
                name: "FinalGrades");

            migrationBuilder.DropTable(
                name: "Periods");

            migrationBuilder.DropIndex(
                name: "IX_Grades_PeriodId",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "PeriodId",
                table: "Grades");
        }
    }
}
