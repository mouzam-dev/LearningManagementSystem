using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArabicCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArabicUnits",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BookNumber = table.Column<int>(type: "integer", nullable: false),
                    UnitNumber = table.Column<int>(type: "integer", nullable: false),
                    TitleAr = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: false),
                    TitleUr = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArabicUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArabicLessons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UnitId = table.Column<string>(type: "character varying(10)", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    TitleAr = table.Column<string>(type: "text", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: false),
                    TitleUr = table.Column<string>(type: "text", nullable: false),
                    IntroAr = table.Column<string>(type: "text", nullable: false),
                    IntroEn = table.Column<string>(type: "text", nullable: false),
                    IntroUr = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArabicLessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArabicLessons_ArabicUnits_UnitId",
                        column: x => x.UnitId,
                        principalTable: "ArabicUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArabicProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<string>(type: "character varying(10)", nullable: false),
                    LessonNumber = table.Column<int>(type: "integer", nullable: false),
                    BestScore = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArabicProgress", x => x.Id);
                    table.CheckConstraint("CK_ArabicProgress_BestScore_Range", "\"BestScore\" BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_ArabicProgress_ArabicUnits_UnitId",
                        column: x => x.UnitId,
                        principalTable: "ArabicUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArabicProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArabicDialogueLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LessonId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Speaker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ar = table.Column<string>(type: "text", nullable: false),
                    Tr = table.Column<string>(type: "text", nullable: false),
                    En = table.Column<string>(type: "text", nullable: false),
                    Ur = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArabicDialogueLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArabicDialogueLines_ArabicLessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "ArabicLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArabicQuizQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LessonId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    QuestionAr = table.Column<string>(type: "text", nullable: false),
                    QuestionEn = table.Column<string>(type: "text", nullable: false),
                    QuestionUr = table.Column<string>(type: "text", nullable: false),
                    Options = table.Column<List<string>>(type: "text[]", nullable: false),
                    OptionsAr = table.Column<List<string>>(type: "text[]", nullable: true),
                    AnswerIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArabicQuizQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArabicQuizQuestions_ArabicLessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "ArabicLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArabicVocabItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LessonId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Ar = table.Column<string>(type: "text", nullable: false),
                    Tr = table.Column<string>(type: "text", nullable: false),
                    En = table.Column<string>(type: "text", nullable: false),
                    Ur = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArabicVocabItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArabicVocabItems_ArabicLessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "ArabicLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArabicDialogueLines_LessonId_Order",
                table: "ArabicDialogueLines",
                columns: new[] { "LessonId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ArabicLessons_UnitId_Number",
                table: "ArabicLessons",
                columns: new[] { "UnitId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArabicProgress_UnitId",
                table: "ArabicProgress",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ArabicProgress_UserId_UnitId_LessonNumber",
                table: "ArabicProgress",
                columns: new[] { "UserId", "UnitId", "LessonNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArabicQuizQuestions_LessonId_Order",
                table: "ArabicQuizQuestions",
                columns: new[] { "LessonId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ArabicUnits_BookNumber_UnitNumber",
                table: "ArabicUnits",
                columns: new[] { "BookNumber", "UnitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArabicVocabItems_LessonId_Order",
                table: "ArabicVocabItems",
                columns: new[] { "LessonId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArabicDialogueLines");

            migrationBuilder.DropTable(
                name: "ArabicProgress");

            migrationBuilder.DropTable(
                name: "ArabicQuizQuestions");

            migrationBuilder.DropTable(
                name: "ArabicVocabItems");

            migrationBuilder.DropTable(
                name: "ArabicLessons");

            migrationBuilder.DropTable(
                name: "ArabicUnits");
        }
    }
}
