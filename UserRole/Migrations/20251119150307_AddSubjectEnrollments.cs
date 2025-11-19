using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserRole.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedSubject",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProfessorSectionAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    GradeLevel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Section = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedRoom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StartTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    EndTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DayOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessorSectionAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfessorSectionAssignments_AspNetUsers_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubjectEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProfessorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EnrolledDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectEnrollments_AspNetUsers_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubjectEnrollments_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfessorSectionAssignments_ProfessorId",
                table: "ProfessorSectionAssignments",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectEnrollments_EnrollmentId",
                table: "SubjectEnrollments",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectEnrollments_ProfessorId",
                table: "SubjectEnrollments",
                column: "ProfessorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfessorSectionAssignments");

            migrationBuilder.DropTable(
                name: "SubjectEnrollments");

            migrationBuilder.DropColumn(
                name: "AssignedSubject",
                table: "AspNetUsers");
        }
    }
}
