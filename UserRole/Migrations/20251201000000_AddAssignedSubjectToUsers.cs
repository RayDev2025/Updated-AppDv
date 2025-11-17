using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserRole.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedSubjectToUsers : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedSubject",
                table: "AspNetUsers");
        }
    }
}

