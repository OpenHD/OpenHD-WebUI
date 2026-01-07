using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocsController : ControllerBase
{
    private const string RemoteDocsUrl = "https://openhdfpv.org/introduction/";
    private const string LocalDocsPath = "/docs/introduction/index.html";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DocsController> _logger;

    public DocsController(IHttpClientFactory httpClientFactory, ILogger<DocsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("link")]
    public async Task<ActionResult<DocsLinkResponse>> GetDocsLink(CancellationToken cancellationToken)
    {
        if (await RemoteDocsReachable(cancellationToken))
        {
            return Ok(new DocsLinkResponse(RemoteDocsUrl));
        }

        return Ok(new DocsLinkResponse(LocalDocsPath));
    }

    private async Task<bool> RemoteDocsReachable(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            using var request = new HttpRequestMessage(HttpMethod.Head, RemoteDocsUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("OpenHdWebUi", "1.0"));

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to reach remote documentation at {RemoteDocsUrl}", RemoteDocsUrl);
            return false;
        }
    }

    public record DocsLinkResponse(string Url);
}
