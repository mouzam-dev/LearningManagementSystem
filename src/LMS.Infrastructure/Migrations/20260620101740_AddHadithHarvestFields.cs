using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHadithHarvestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookNameAr",
                table: "Hadiths",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookNameEn",
                table: "Hadiths",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradeCategory",
                table: "Hadiths",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HadithCollections",
                columns: table => new
                {
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShortIntroEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HadithCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HadithCollections", x => x.Slug);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hadiths_Collection_GradeCategory",
                table: "Hadiths",
                columns: new[] { "Collection", "GradeCategory" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HadithCollections");

            migrationBuilder.DropIndex(
                name: "IX_Hadiths_Collection_GradeCategory",
                table: "Hadiths");

            migrationBuilder.DropColumn(
                name: "BookNameAr",
                table: "Hadiths");

            migrationBuilder.DropColumn(
                name: "BookNameEn",
                table: "Hadiths");

            migrationBuilder.DropColumn(
                name: "GradeCategory",
                table: "Hadiths");
        }
    }
}
