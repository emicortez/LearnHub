using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnHub.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseVersionToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "courses",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "courses");
        }
    }
}
