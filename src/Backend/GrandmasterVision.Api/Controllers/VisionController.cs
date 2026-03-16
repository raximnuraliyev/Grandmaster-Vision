using Microsoft.AspNetCore.Mvc;

namespace GrandmasterVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisionController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VisionController> _logger;

    public VisionController(
        IHttpClientFactory httpClientFactory,
        ILogger<VisionController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Recognize chess board from uploaded image.
    /// Proxies request to Python Vision microservice.
    /// </summary>
    [HttpPost("recognize")]
    public async Task<IActionResult> RecognizeBoard(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        try
        {
            var client = _httpClientFactory.CreateClient("VisionService");

            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);

            content.Add(streamContent, "file", file.FileName);

            var response = await client.PostAsync("/api/recognize", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync(cancellationToken);
                return Content(result, "application/json");
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return StatusCode((int)response.StatusCode, error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Vision service unavailable");
            return StatusCode(503, new { error = "Vision service unavailable" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image");
            return StatusCode(500, new { error = "Image processing failed" });
        }
    }
}
