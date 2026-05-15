using LMS.Application.Common;

namespace LMS.WebAPI.Services;

/// <summary>
/// Saves uploaded files under the host's wwwroot so they're served as static
/// content. Returns a relative path (e.g. "/uploads/documents/{guid}.pdf");
/// the caller turns it into an absolute URL from the current request.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorage(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveAsync(
        Stream content,
        string subfolder,
        string extension,
        CancellationToken cancellationToken = default)
    {
        // WebRootPath can be null when wwwroot doesn't exist yet — fall back to
        // <contentroot>/wwwroot and create it.
        var webRoot = _env.WebRootPath
            ?? Path.Combine(_env.ContentRootPath, "wwwroot");

        // Normalise the subfolder to forward slashes for the public URL, but
        // use the OS separator on disk.
        var cleanSub = subfolder.Trim('/', '\\');
        var targetDir = Path.Combine(webRoot, cleanSub.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(targetDir);

        var ext = extension.StartsWith('.') ? extension : "." + extension;
        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(targetDir, fileName);

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
        {
            await content.CopyToAsync(fs, cancellationToken);
        }

        return $"/{cleanSub}/{fileName}";
    }
}
