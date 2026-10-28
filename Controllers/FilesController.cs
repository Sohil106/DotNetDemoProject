using Microsoft.AspNetCore.Mvc;

namespace InterviewLab.Controllers;

[ApiController, Route("api/files")]
public sealed class FilesController(IWebHostEnvironment environment) : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".txt", ".pdf", ".png", ".jpg", ".jpeg" };

    [HttpPost, RequestSizeLimit(MaxFileSize)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        // .NET Core & Web API Q14, Q31, Q48: bounded multipart upload, asynchronous streaming, and filename sanitization.
        if (file is null || file.Length == 0) return BadRequest(new { error = "Choose a non-empty file." });
        if (file.Length > MaxFileSize) return StatusCode(StatusCodes.Status413PayloadTooLarge);
        var extension = Path.GetExtension(Path.GetFileName(file.FileName));
        if (!AllowedExtensions.Contains(extension)) return BadRequest(new { error = "Allowed file types: txt, pdf, png, jpg." });

        var directory = Path.Combine(environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(directory);
        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = Path.Combine(directory, storedName);
        await using (var destination = System.IO.File.Create(path))
            await file.CopyToAsync(destination, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id = storedName, bytes = file.Length, originalName = Path.GetFileName(file.FileName) });
    }
}
