using LMS.Application.Student.Dtos;
using LMS.Application.Student.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LMS.Infrastructure.Services.Certificates;

/// <summary>
/// QuestPDF implementation. Renders an A4-landscape diploma styled to match
/// the on-screen certificate (indigo/rose gradient border, large student name,
/// course meta, instructor signature, verify code + URL).
///
/// QuestPDF.Settings.License is configured at app startup in Program.cs.
/// </summary>
public class CertificatePdfService : ICertificatePdfService
{
    // Palette mirrors the Tailwind tokens the on-screen cert uses, so the
    // printed PDF reads as the same artifact rather than a generic export.
    private const string ColorInk        = "#0F172A";   // slate-900
    private const string ColorInkSoft    = "#334155";   // slate-700
    private const string ColorMuted      = "#64748B";   // slate-500
    private const string ColorIndigo     = "#6366F1";   // indigo-500
    private const string ColorViolet     = "#8B5CF6";   // violet-500
    private const string ColorRose       = "#F43F5E";   // rose-500
    private const string ColorAmber      = "#D97706";   // amber-600
    private const string ColorAmberLight = "#FBBF24";   // amber-400
    private const string ColorSurface    = "#FFFFFF";
    private const string ColorLineSoft   = "#E2E8F0";   // slate-200

    public byte[] Render(CertificateDto cert, string verifyUrl)
    {
        var studentName    = string.IsNullOrWhiteSpace(cert.StudentName) ? "Student" : cert.StudentName;
        var courseTitle    = cert.CourseTitle;
        var instructorName = string.IsNullOrWhiteSpace(cert.InstructorName) ? "Instructor" : cert.InstructorName;
        var completedDate  = (cert.CompletedAt ?? cert.IssuedAt).ToString("MMMM d, yyyy");
        var issuedDate     = cert.IssuedAt.ToString("MMMM d, yyyy");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(t => t.FontFamily(Fonts.Calibri).FontColor(ColorInk));

                page.Content().Element(body =>
                {
                    // Outer gradient frame — rendered as three nested borders
                    // so we keep the layered look without needing custom paint.
                    body
                        .Background(ColorIndigo)
                        .Padding(4)
                        .Background(ColorViolet)
                        .Padding(3)
                        .Background(ColorRose)
                        .Padding(3)
                        .Background(ColorSurface)
                        .Padding(36)
                        .Column(col =>
                        {
                            col.Spacing(0);

                            // Brand row + seal
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text("DUARE SHARIYE  ·  CERTIFICATE")
                                    .FontSize(10).Bold().FontColor(ColorIndigo).LetterSpacing(2.5f);

                                row.ConstantItem(64)
                                    .Background(ColorAmberLight)
                                    .Padding(8)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Text("★")
                                    .FontSize(28).FontColor(ColorSurface);
                            });

                            col.Item().PaddingTop(18).LineHorizontal(0.6f).LineColor(ColorLineSoft);

                            // Headline
                            col.Item().PaddingTop(30).AlignCenter()
                                .Text("This is to certify that")
                                .FontSize(12).FontColor(ColorMuted).Italic();

                            col.Item().PaddingTop(12).AlignCenter()
                                .Text(studentName)
                                .FontFamily(Fonts.TimesNewRoman)
                                .FontSize(46).Bold().FontColor(ColorInk);

                            col.Item().PaddingTop(6).AlignCenter()
                                .Width(200).LineHorizontal(0.5f).LineColor(ColorLineSoft);

                            col.Item().PaddingTop(14).AlignCenter()
                                .Text("has successfully completed the course")
                                .FontSize(12).FontColor(ColorMuted);

                            col.Item().PaddingTop(8).AlignCenter()
                                .Text(courseTitle)
                                .FontSize(22).SemiBold().FontColor(ColorInk);

                            if (!string.IsNullOrWhiteSpace(cert.CourseCategory))
                            {
                                col.Item().PaddingTop(4).AlignCenter()
                                    .Text(cert.CourseCategory!.ToUpperInvariant())
                                    .FontSize(9).Bold().FontColor(ColorIndigo).LetterSpacing(1.5f);
                            }

                            // Stats row
                            col.Item().PaddingTop(28).Row(row =>
                            {
                                row.RelativeItem().Element(c => StatBlock(c, "COMPLETED", completedDate));
                                row.RelativeItem().Element(c => StatBlock(c, "LESSONS", cert.TotalLessons.ToString()));
                                row.RelativeItem().Element(c => StatBlock(c, "INSTRUCTOR", instructorName));
                            });

                            // Footer: signature + verify
                            col.Item().PaddingTop(40).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text(instructorName)
                                        .FontFamily(Fonts.TimesNewRoman).FontSize(16).Italic().FontColor(ColorInk);
                                    c.Item().PaddingTop(2).Width(180)
                                        .LineHorizontal(0.6f).LineColor(ColorLineSoft);
                                    c.Item().PaddingTop(2).Text("INSTRUCTOR SIGNATURE")
                                        .FontSize(8).Bold().FontColor(ColorMuted).LetterSpacing(1.2f);
                                });

                                row.ConstantItem(260).AlignRight().Column(c =>
                                {
                                    c.Item().AlignRight().Text("VERIFY CODE")
                                        .FontSize(8).Bold().FontColor(ColorMuted).LetterSpacing(1.2f);
                                    c.Item().PaddingTop(2).AlignRight().Text(cert.VerifyCode)
                                        .FontFamily(Fonts.CourierNew).FontSize(13).SemiBold()
                                        .FontColor(ColorInk).LetterSpacing(2);
                                    c.Item().PaddingTop(2).AlignRight().Text(verifyUrl)
                                        .FontSize(8).FontColor(ColorMuted);
                                });
                            });
                        });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Issued ").FontSize(8).FontColor(ColorMuted);
                    t.Span(issuedDate).FontSize(8).Bold().FontColor(ColorInkSoft);
                    t.Span("   ·   Certificate ID ").FontSize(8).FontColor(ColorMuted);
                    t.Span(cert.CertificateId.ToString()).FontSize(8).FontColor(ColorInkSoft);
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void StatBlock(IContainer container, string label, string value)
    {
        container.AlignCenter().Column(c =>
        {
            c.Item().Text(label)
                .FontSize(8).Bold().FontColor(ColorMuted).LetterSpacing(1.4f);
            c.Item().PaddingTop(3).Text(value)
                .FontSize(11).SemiBold().FontColor(ColorInk);
        });
    }
}
