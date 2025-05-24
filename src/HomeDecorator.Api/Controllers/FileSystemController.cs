using Microsoft.AspNetCore.Mvc;

namespace HomeDecorator.Api.Controllers;

[ApiController]
[Route("api/diagnostics")]
public class FileSystemController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileSystemController> _logger;

    public FileSystemController(
        IWebHostEnvironment env,
        ILogger<FileSystemController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpGet("file-exists")]
    public IActionResult CheckFileExists([FromQuery] string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return BadRequest("Path parameter is required");
        }

        // Remove leading slash for file path resolution
        if (path.StartsWith("/"))
        {
            path = path.Substring(1);
        }

        // Check if the file exists in wwwroot
        var fullPath = Path.Combine(_env.WebRootPath, path);
        bool exists = System.IO.File.Exists(fullPath);

        // List directory contents if file not found to help debugging
        var directoryContents = new List<string>();
        if (!exists)
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (Directory.Exists(directory))
            {
                directoryContents = Directory.GetFiles(directory)
                    .Select(f => Path.GetFileName(f) ?? string.Empty)
                    .Where(f => !string.IsNullOrEmpty(f))
                    .ToList();
            }
        }

        return Ok(new
        {
            exists,
            requestedPath = path,
            fullPath,
            webRootPath = _env.WebRootPath,
            directoryExists = Directory.Exists(Path.GetDirectoryName(fullPath) ?? string.Empty),
            directoryContents
        });
    }

    [HttpGet("list-directories")]
    public IActionResult ListDirectories()
    {
        var wwwrootPath = _env.WebRootPath;

        // Check if wwwroot/images exists
        var imagesPath = Path.Combine(wwwrootPath, "images");
        var imagesExists = Directory.Exists(imagesPath);

        var uploadedPath = Path.Combine(imagesPath, "uploaded");
        var uploadedExists = Directory.Exists(uploadedPath);

        var generatedPath = Path.Combine(imagesPath, "generated");
        var generatedExists = Directory.Exists(generatedPath);

        // Get file counts
        var uploadedFiles = uploadedExists ? Directory.GetFiles(uploadedPath).Length : 0;
        var generatedFiles = generatedExists ? Directory.GetFiles(generatedPath).Length : 0;

        return Ok(new
        {
            wwwrootExists = Directory.Exists(wwwrootPath),
            wwwrootPath,
            imagesExists,
            imagesPath,
            uploadedExists,
            uploadedPath,
            uploadedFiles,
            generatedExists,
            generatedPath,
            generatedFiles
        });
    }
}
