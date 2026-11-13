using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentGradeManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SubjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectCode);
                });

            migrationBuilder.Sql("""
                INSERT INTO "Subjects" ("SubjectCode", "SubjectName")
                SELECT DISTINCT ON ("SubjectCode") "SubjectCode", "SubjectName"
                FROM "StudentGrades"
                ORDER BY "SubjectCode", COALESCE("DateModified", "DatePosted") DESC, "GradeId" DESC;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subjects");
        }
    }
}
