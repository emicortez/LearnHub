using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnHub.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    price_currency = table.Column<string>(type: "char(3)", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'draft'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courses", x => x.id);

                    // Written by hand, here and in lessons: the model maps these as unique indexes, and the generated
                    // migration would create indexes. A constraint reports the same name and enforces the same rule.
                    table.UniqueConstraint("uq_courses_slug", x => x.slug);
                    table.CheckConstraint("ck_courses_currency", "price_currency::text ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("ck_courses_instructor_id_not_empty", "instructor_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_courses_price_range", "price_amount BETWEEN 0 AND 9999999999.99");
                    table.CheckConstraint("ck_courses_price_scale", "price_amount = round(price_amount, 2)");
                    table.CheckConstraint("ck_courses_slug_not_blank", "slug ~ '[^[:space:]]'");
                    table.CheckConstraint("ck_courses_status", "status IN ('draft', 'published', 'archived')");
                    table.CheckConstraint("ck_courses_title_length", "title ~ '[^[:space:]]' AND char_length(btrim(title)) <= 120");
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lessons", x => x.id);
                    table.UniqueConstraint("uq_lessons_course_position", x => new { x.course_id, x.position });
                    table.CheckConstraint("ck_lessons_duration_positive", "duration_seconds > 0");
                    table.CheckConstraint("ck_lessons_position_positive", "position > 0");
                    table.CheckConstraint("ck_lessons_title_not_blank", "title ~ '[^[:space:]]'");
                    table.ForeignKey(
                        name: "fk_lessons_course",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_courses_instructor_title_slug",
                table: "courses",
                columns: new[] { "instructor_id", "title", "slug" });

            migrationBuilder.CreateIndex(
                name: "ix_courses_published_title_slug",
                table: "courses",
                columns: new[] { "title", "slug" },
                filter: "status = 'published'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "courses");
        }
    }
}
