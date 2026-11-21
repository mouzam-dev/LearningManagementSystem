using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ArabicContentV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ArabicDialogueLines",
                type: "text",
                nullable: true);

            // New course text (original dialogues): drop the old lessons (dialogue, vocab and quiz rows
            // cascade) so ArabicContentSeeder re-imports them on the next start. Units - and with them
            // learners' ArabicProgress - are kept.
            migrationBuilder.Sql("DELETE FROM \"ArabicLessons\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "ArabicDialogueLines");
        }
    }
}
