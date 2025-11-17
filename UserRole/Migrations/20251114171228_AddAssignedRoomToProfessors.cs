using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserRole.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedRoomToProfessors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedRoom",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedRoom",
                table: "AspNetUsers");
        }
    }
}
