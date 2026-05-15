namespace LMS.Application.Common;

/// <summary>
/// Persists uploaded files (lesson documents, and later course thumbnails)
/// and hands back the relative URL path they can be served from.
/// Implemented in the WebAPI host since the concrete store is wwwroot.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Saves <paramref name="content"/> under <paramref name="subfolder"/> with a
    /// generated unique name + the given <paramref name="extension"/>.
    /// Returns the relative public path, e.g. "/uploads/documents/{guid}.pdf".
    /// </summary>
    Task<string> SaveAsync(
        Stream content,
        string subfolder,
        string extension,
        CancellationToken cancellationToken = default);
}
