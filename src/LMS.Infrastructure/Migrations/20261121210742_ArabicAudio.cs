using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ArabicAudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Audio",
                table: "ArabicVocabItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Audio",
                table: "ArabicDialogueLines",
                type: "text",
                nullable: true);

            // Re-import the lessons so dialogue lines and words pick up their audio clip names.
            // Units, and so learners' ArabicProgress, are kept (see ArabicContentSeeder).
            migrationBuilder.Sql("DELETE FROM \"ArabicLessons\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Audio",
                table: "ArabicVocabItems");

            migrationBuilder.DropColumn(
                name: "Audio",
                table: "ArabicDialogueLines");
        }
    }
}
