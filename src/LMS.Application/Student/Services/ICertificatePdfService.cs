using LMS.Application.Student.Dtos;

namespace LMS.Application.Student.Services;

/// <summary>
/// Renders a completed-course certificate as a downloadable PDF.
/// Implemented in Infrastructure with QuestPDF; lives behind an interface so
/// Application code (controllers, handlers) never references the PDF library.
/// </summary>
public interface ICertificatePdfService
{
    /// <param name="cert">Full certificate payload (student, course, instructor, dates).</param>
    /// <param name="verifyUrl">
    /// Absolute URL of the public verification page, e.g.
    /// <c>https://lms-demo.azurewebsites.net/verify/ABC-123</c>. Embedded on
    /// the certificate so anyone holding the PDF can confirm authenticity.
    /// </param>
    /// <returns>PDF file bytes, ready to stream to the client.</returns>
    byte[] Render(CertificateDto cert, string verifyUrl);
}
