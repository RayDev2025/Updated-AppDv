using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserRole.Migrations
{
    /// <inheritdoc />
    public partial class CreateProfessorSectionAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    AssignedRoom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_ProfessorSectionAssignments_ProfessorId",
                table: "ProfessorSectionAssignments",
                column: "ProfessorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfessorSectionAssignments");
        }
    }
}

