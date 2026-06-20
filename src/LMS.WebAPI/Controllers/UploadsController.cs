using LMS.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Role-agnostic file upload endpoints. Authentication is required but the
/// authorization gate sits on the *consumer* of the returned URL — i.e. the
/// existing handlers that attach a URL to an Organization, User, or Course
/// already enforce who's allowed to do that. This controller just stores the
/// bytes and hands back a public URL.
/// </summary>
[ApiController]
[Route("api/uploads")]
[Authorize]
public class UploadsController : ControllerBase
{
    private readonly IFileStorage _fileStorage;

    public UploadsController(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public class ImageUploadResult
    {
        /// <summary>Absolute URL the saved image is served from.</summary>
        public string Url { get; set; } = string.Empty;
    }

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg",
    };

    // Buckets the image gets stored under. Keeps wwwroot organized and makes
    // it cheap to retention-policy a single bucket later (e.g. evict orphaned
    // avatars). Defaults to "misc" if the caller doesn't say.
    private static readonly HashSet<string> AllowedKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "logo", "avatar", "thumbnail", "misc",
    };

    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    public async Task<ActionResult<ImageUploadResult>> UploadImage(
        IFormFile? file,
        [FromQuery] string? kind,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["file"] = ["Choose an image to upload."] }));
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedImageExtensions.Contains(ext))
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["file"] = [$"Unsupported image type. Allowed: {string.Join(", ", AllowedImageExtensions)}"],
                }));
        }

        if (file.Length > MaxImageBytes)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["Image is too large — the limit is 5 MB."],
                }));
        }

        var bucket = !string.IsNullOrWhiteSpace(kind) && AllowedKinds.Contains(kind)
            ? kind.ToLowerInvariant()
            : "misc";

        await using var stream = file.OpenReadStream();
        var relativePath = await _fileStorage.SaveAsync(stream, $"uploads/images/{bucket}s", ext, ct);

        // Absolute URL so the stored value works from any origin.
        var absoluteUrl = $"{Request.Scheme}://{Request.Host}{relativePath}";
        return Ok(new ImageUploadResult { Url = absoluteUrl });
    }
}
