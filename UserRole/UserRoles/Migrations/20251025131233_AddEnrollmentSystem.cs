using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UserRoles.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChange",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndTime",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHistory",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GradeLevel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Section = table.Column<int>(type: "int", nullable: true),
                    BirthCertificate = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Form137 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ParentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enrollments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectionCapacities",
                columns: table => new
                {
                    GradeLevel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CurrentSection = table.Column<int>(type: "int", nullable: false),
                    StudentsInCurrentSection = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionCapacities", x => x.GradeLevel);
                });

            migrationBuilder.InsertData(
                table: "SectionCapacities",
                columns: new[] { "GradeLevel", "CurrentSection", "StudentsInCurrentSection" },
                values: new object[,]
                {
                    { "1", 1, 0 },
                    { "2", 1, 0 },
                    { "3", 1, 0 },
                    { "4", 1, 0 },
                    { "5", 1, 0 },
                    { "6", 1, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserId",
                table: "Enrollments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "SectionCapacities");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastPasswordChange",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LockoutEndTime",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordHistory",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "AspNetUsers");
        }
    }
}
