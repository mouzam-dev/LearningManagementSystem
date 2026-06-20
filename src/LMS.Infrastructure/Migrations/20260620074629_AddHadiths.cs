using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHadiths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hadiths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Collection = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BookNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChapterId = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    HadithNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OurHadithNumber = table.Column<int>(type: "int", nullable: false),
                    ArabicUrn = table.Column<int>(type: "int", nullable: false),
                    EnglishUrn = table.Column<int>(type: "int", nullable: false),
                    ChapterEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChapterAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BodyEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BodyAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GradeEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    GradeAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hadiths", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hadiths_Collection",
                table: "Hadiths",
                column: "Collection");

            migrationBuilder.CreateIndex(
                name: "IX_Hadiths_Collection_BookNumber_OurHadithNumber",
                table: "Hadiths",
                columns: new[] { "Collection", "BookNumber", "OurHadithNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hadiths");
        }
    }
}
