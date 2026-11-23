using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLibrarySearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiQueryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PassageCount = table.Column<int>(type: "integer", nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiQueryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuranSurahs",
                columns: table => new
                {
                    Number = table.Column<int>(type: "integer", nullable: false),
                    NameAr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameTransliterated = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RevelationPlace = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VerseCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuranSurahs", x => x.Number);
                });

            migrationBuilder.CreateTable(
                name: "QuranVerses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SurahNumber = table.Column<int>(type: "integer", nullable: false),
                    VerseNumber = table.Column<int>(type: "integer", nullable: false),
                    TextAr = table.Column<string>(type: "text", nullable: false),
                    TextEn = table.Column<string>(type: "text", nullable: false),
                    TextUr = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuranVerses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuranVerses_QuranSurahs_SurahNumber",
                        column: x => x.SurahNumber,
                        principalTable: "QuranSurahs",
                        principalColumn: "Number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiQueryLogs_UserId_CreatedAt",
                table: "AiQueryLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuranVerses_SurahNumber_VerseNumber",
                table: "QuranVerses",
                columns: new[] { "SurahNumber", "VerseNumber" },
                unique: true);

            // Full-text search (library search). lib_norm_ar folds Arabic/Urdu spelling variants
            // (harakat, tatweel, alif/ya/ta-marbuta/kaf/heh forms) so queries match with or without
            // diacritics; the search service applies the same folding to queries.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION lib_norm_ar(t text) RETURNS text
                LANGUAGE sql IMMUTABLE PARALLEL SAFE AS $$
                  SELECT translate(regexp_replace(replace(coalesce(t, ''), 'وٰ', 'ا'), '[\u064B-\u065F\u0670\u06D6-\u06ED\u0640]', '', 'g'),
                                   'أإآٱىةیکہۃ', 'اااايهيكهه')
                $$;
                -- (Uthmani الصلوٰة/الزكوٰة spell the alif as waw + dagger alif: folded to alif first.)
                -- English transliteration accents (Ramaḍān, zakāh, Kaʿbah) folded to plain letters.
                CREATE OR REPLACE FUNCTION lib_fold_en(t text) RETURNS text
                LANGUAGE sql IMMUTABLE PARALLEL SAFE AS $$
                  SELECT translate(coalesce(t, ''), 'āĀḍḥḤīṣṢṭṬūẓẒäãüʿʾʻʼ', 'aAdhHisStTuzZaau')
                $$;
                """);
            migrationBuilder.Sql("""
                ALTER TABLE "QuranVerses" ADD COLUMN "SearchVector" tsvector GENERATED ALWAYS AS (
                    setweight(to_tsvector('english', lib_fold_en("TextEn")), 'B')
                 || setweight(to_tsvector('arabic', lib_norm_ar("TextAr")), 'B')
                 || setweight(to_tsvector('simple', lib_norm_ar("TextUr")), 'C')) STORED;
                CREATE INDEX "IX_QuranVerses_SearchVector" ON "QuranVerses" USING gin ("SearchVector");
                """);
            // Hadith: chapter/book titles weigh more than the narration; inline [narrator]/[matn] tags are stripped.
            migrationBuilder.Sql("""
                ALTER TABLE "Hadiths" ADD COLUMN "SearchVector" tsvector GENERATED ALWAYS AS (
                    setweight(to_tsvector('english', lib_fold_en(coalesce("ChapterEn", '') || ' ' || coalesce("BookNameEn", ''))), 'A')
                 || setweight(to_tsvector('english', lib_fold_en(regexp_replace(coalesce("BodyEn", ''), '\[/?[a-zA-Z][^\]]*\]|<[^>]+>', ' ', 'g'))), 'C')
                 || setweight(to_tsvector('arabic', lib_norm_ar(coalesce("ChapterAr", '') || ' ' || regexp_replace(coalesce("BodyAr", ''), '\[/?[a-zA-Z][^\]]*\]|<[^>]+>', ' ', 'g'))), 'C')) STORED;
                CREATE INDEX "IX_Hadiths_SearchVector" ON "Hadiths" USING gin ("SearchVector");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_Hadiths_SearchVector";
                ALTER TABLE "Hadiths" DROP COLUMN IF EXISTS "SearchVector";
                """);

            migrationBuilder.DropTable(
                name: "AiQueryLogs");

            migrationBuilder.DropTable(
                name: "QuranVerses");

            migrationBuilder.DropTable(
                name: "QuranSurahs");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS lib_norm_ar(text); DROP FUNCTION IF EXISTS lib_fold_en(text);");
        }
    }
}
