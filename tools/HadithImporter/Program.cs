using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;

// One-time importer: parse the Sunnah.com MySQL dump (.claude/HadithTable.sql)
// and bulk-load it into LmsDb.Hadiths (SQL Server). Idempotent — truncates first.
//
//   dotnet run --project tools/HadithImporter [dumpPath] [connectionString]
//
// Defaults assume it is run from the repo root.

var dumpPath = args.Length > 0 ? args[0] : ".claude/HadithTable.sql";
var connectionString = args.Length > 1
    ? args[1]
    : "Server=localhost;Database=LmsDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

if (!File.Exists(dumpPath))
{
    Console.Error.WriteLine($"Dump not found: {Path.GetFullPath(dumpPath)}");
    return 1;
}

Console.WriteLine($"Dump : {Path.GetFullPath(dumpPath)}");
Console.WriteLine($"Conn : {connectionString.Split(';')[0]} / {connectionString.Split(';')[1]}");

// Column order of the `HadithTable` dump (from CREATE TABLE in the dump).
const int Collection = 0, BookNumber = 1, BabId = 2, HadithNumber = 5, OurHadithNumber = 6,
          ArabicUrn = 7, ArabicBabName = 8, ArabicText = 9, ArabicGrade = 10, EnglishUrn = 11,
          EnglishBabName = 12, EnglishText = 13, EnglishGrade = 14;

await using var conn = new SqlConnection(connectionString);
await conn.OpenAsync();

// Idempotent: clear any prior import (resets identity too).
await using (var truncate = new SqlCommand("TRUNCATE TABLE [Hadiths];", conn))
    await truncate.ExecuteNonQueryAsync();
Console.WriteLine("Truncated Hadiths.");

var table = NewTable();
long total = 0;
const int BatchSize = 5000;

using var bulk = new SqlBulkCopy(conn) { DestinationTableName = "Hadiths", BulkCopyTimeout = 180 };
foreach (DataColumn col in table.Columns)
    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

using (var reader = new StreamReader(dumpPath, Encoding.UTF8))
{
    // mysqldump here writes "INSERT INTO `HadithTable` VALUES" then one tuple per
    // line, terminated by a line ending in ';'. Newlines inside data are escaped
    // (\n), so a trailing ';' reliably marks the end of a statement. Accumulate the
    // whole statement, then parse all its tuples.
    const string startMarker = "INSERT INTO `HadithTable`";
    var buf = new StringBuilder();
    bool inStmt = false;
    string? line;
    while ((line = await reader.ReadLineAsync()) is not null)
    {
        if (!inStmt)
        {
            int vi;
            if (line.StartsWith(startMarker, StringComparison.Ordinal) &&
                (vi = line.IndexOf("VALUES", StringComparison.Ordinal)) >= 0)
            {
                inStmt = true;
                buf.Clear();
                buf.Append(line, vi + 6, line.Length - (vi + 6)); // after "VALUES"
            }
            else continue;
        }
        else
        {
            buf.Append('\n').Append(line);
        }

        if (!line.TrimEnd().EndsWith(';')) continue; // statement not finished yet

        foreach (var row in ParseRows(buf.ToString()))
        {
            if (row.Count <= EnglishGrade) continue; // malformed / short

            var r = table.NewRow();
            r["Collection"] = Clip(row[Collection], 50) ?? "";
            r["BookNumber"] = Clip(row[BookNumber], 20) ?? "";
            r["ChapterId"] = ParseDecimal(row[BabId]);
            r["HadithNumber"] = Clip(row[HadithNumber], 50) ?? "";
            r["OurHadithNumber"] = ParseInt(row[OurHadithNumber]);
            r["ArabicUrn"] = ParseInt(row[ArabicUrn]);
            r["EnglishUrn"] = ParseInt(row[EnglishUrn]);
            r["ChapterEn"] = (object?)row[EnglishBabName] ?? DBNull.Value;
            r["ChapterAr"] = (object?)row[ArabicBabName] ?? DBNull.Value;
            r["BodyEn"] = (object?)row[EnglishText] ?? DBNull.Value;
            r["BodyAr"] = (object?)row[ArabicText] ?? DBNull.Value;
            r["GradeEn"] = (object?)Clip(row[EnglishGrade], 2000) ?? DBNull.Value;
            r["GradeAr"] = (object?)Clip(row[ArabicGrade], 2000) ?? DBNull.Value;
            table.Rows.Add(r);

            if (table.Rows.Count >= BatchSize)
            {
                await bulk.WriteToServerAsync(table);
                total += table.Rows.Count;
                Console.Write($"\rImported {total:N0} rows...");
                table.Clear();
            }
        }

        buf.Clear();
        inStmt = false;
    }
}

if (table.Rows.Count > 0)
{
    await bulk.WriteToServerAsync(table);
    total += table.Rows.Count;
}
Console.WriteLine($"\rImported {total:N0} rows.        ");

// Summary: distinct collections + counts.
await using (var summary = new SqlCommand(
    "SELECT Collection, COUNT(*) FROM Hadiths GROUP BY Collection ORDER BY COUNT(*) DESC;", conn))
await using (var rd = await summary.ExecuteReaderAsync())
{
    Console.WriteLine("\nCollections:");
    while (await rd.ReadAsync())
        Console.WriteLine($"  {rd.GetString(0),-18} {rd.GetInt32(1),8:N0}");
}

Console.WriteLine("\nDone.");
return 0;

// ---- helpers ----

static DataTable NewTable()
{
    var t = new DataTable();
    t.Columns.Add("Collection", typeof(string));
    t.Columns.Add("BookNumber", typeof(string));
    t.Columns.Add("ChapterId", typeof(decimal));
    t.Columns.Add("HadithNumber", typeof(string));
    t.Columns.Add("OurHadithNumber", typeof(int));
    t.Columns.Add("ArabicUrn", typeof(int));
    t.Columns.Add("EnglishUrn", typeof(int));
    t.Columns.Add("ChapterEn", typeof(string));
    t.Columns.Add("ChapterAr", typeof(string));
    t.Columns.Add("BodyEn", typeof(string));
    t.Columns.Add("BodyAr", typeof(string));
    t.Columns.Add("GradeEn", typeof(string));
    t.Columns.Add("GradeAr", typeof(string));
    return t;
}

static string? Clip(string? s, int max) => s is null ? null : (s.Length <= max ? s : s[..max]);

static decimal ParseDecimal(string? s) =>
    decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

static int ParseInt(string? s) =>
    int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;

// Parse a mysqldump VALUES list: (v0,v1,...),(...),... ;  Handles MySQL string
// escaping. Returns each tuple as a list of nullable string fields (NULL -> null).
static IEnumerable<List<string?>> ParseRows(string s)
{
    int i = 0, n = s.Length;
    while (i < n)
    {
        while (i < n && s[i] != '(') i++;     // seek tuple start
        if (i >= n) yield break;
        i++;                                   // skip '('

        var row = new List<string?>();
        var sb = new StringBuilder();
        bool endTuple = false;

        while (i < n && !endTuple)
        {
            char c = s[i];
            if (c == '\'')                     // quoted string
            {
                sb.Clear();
                i++;
                while (i < n)
                {
                    char d = s[i];
                    if (d == '\\' && i + 1 < n)
                    {
                        char e = s[i + 1];
                        sb.Append(e switch
                        {
                            'n' => '\n', 'r' => '\r', 't' => '\t',
                            '0' => '\0', 'b' => '\b', 'Z' => '',
                            _ => e,            // \' \" \\ -> literal char
                        });
                        i += 2;
                    }
                    else if (d == '\'')
                    {
                        if (i + 1 < n && s[i + 1] == '\'') { sb.Append('\''); i += 2; } // '' escape
                        else { i++; break; }   // closing quote
                    }
                    else { sb.Append(d); i++; }
                }
                row.Add(sb.ToString());
                while (i < n && s[i] != ',' && s[i] != ')') i++; // skip to delimiter
                if (i < n && s[i] == ')') { i++; endTuple = true; }
                else if (i < n) i++;           // skip ','
            }
            else if (c == ')') { i++; endTuple = true; }
            else if (c == ',') { i++; }
            else                                // bare token: number / NULL
            {
                sb.Clear();
                while (i < n && s[i] != ',' && s[i] != ')') { sb.Append(s[i]); i++; }
                var tok = sb.ToString().Trim();
                row.Add(tok.Equals("NULL", StringComparison.OrdinalIgnoreCase) ? null : tok);
            }
        }
        yield return row;
    }
}
