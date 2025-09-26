using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocsController : ControllerBase
{
    private const string RemoteDocsUrl = "https://openhdfpv.org/introduction/";
    private const string DefaultDocsRequestPath = "/docs";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DocsController> _logger;
    private readonly DocumentationConfiguration _documentationConfiguration;
    private readonly IWebHostEnvironment _environment;

    public DocsController(
        IHttpClientFactory httpClientFactory,
        ILogger<DocsController> logger,
        IOptions<DocumentationConfiguration> documentationOptions,
        IWebHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _documentationConfiguration = documentationOptions.Value;
        _environment = environment;
    }

    [HttpGet("link")]
    public async Task<ActionResult<DocsLinkResponse>> GetDocsLink(CancellationToken cancellationToken)
    {
        if (TryGetLocalDocsUrl(out var localDocsUrl))
        {
            return Ok(new DocsLinkResponse(localDocsUrl));
        }

        if (await RemoteDocsReachable(cancellationToken))
        {
            return Ok(new DocsLinkResponse(RemoteDocsUrl));
        }

        if (TryGetBundledDocsUrl(out var bundledDocsUrl))
        {
            return Ok(new DocsLinkResponse(bundledDocsUrl));
        }

        return Ok(new DocsLinkResponse(RemoteDocsUrl));
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

    private bool TryGetLocalDocsUrl(out string url)
    {
        var introRelativePath = GetRelativeIntroPath();
        var configuredRoot = _documentationConfiguration.LocalDocsRoot;
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var absolutePath = Path.GetFullPath(configuredRoot);
            if (Directory.Exists(absolutePath))
            {
                if (!HasFullDocusaurusAssets(absolutePath))
                {
                    _logger.LogDebug("Local documentation at {DocsRoot} does not contain a full Docusaurus build.", absolutePath);
                    url = string.Empty;
                    return false;
                }

                var introductionFullPath = Path.Combine(absolutePath, introRelativePath);
                if (IsSafeChildPath(absolutePath, introductionFullPath) && System.IO.File.Exists(introductionFullPath))
                {
                    url = BuildDocsUrl(introRelativePath, _documentationConfiguration.RequestPath);
                    return true;
                }
            }
        }

        url = string.Empty;
        return false;
    }

    private static bool HasFullDocusaurusAssets(string docsRoot)
    {
        try
        {
            var assetsPath = Path.Combine(docsRoot, "assets");
            if (!Directory.Exists(assetsPath))
            {
                return false;
            }

            var jsAssetsPath = Path.Combine(assetsPath, "js");
            if (!Directory.Exists(jsAssetsPath))
            {
                return false;
            }

            return Directory.EnumerateFiles(jsAssetsPath, "*.js").Any();
        }
        catch
        {
            return false;
        }
    }

    private bool TryGetBundledDocsUrl(out string url)
    {
        var introRelativePath = GetRelativeIntroPath();
        if (!string.IsNullOrWhiteSpace(_environment.WebRootPath))
        {
            var bundledRoot = Path.Combine(_environment.WebRootPath, "docs");
            var introductionFullPath = Path.Combine(bundledRoot, introRelativePath);
            if (IsSafeChildPath(bundledRoot, introductionFullPath) && System.IO.File.Exists(introductionFullPath))
            {
                url = BuildDocsUrl(introRelativePath, DefaultDocsRequestPath);
                return true;
            }
        }

        url = string.Empty;
        return false;
    }

    private string GetRelativeIntroPath()
    {
        var introPath = _documentationConfiguration.LocalIntroPage;
        if (string.IsNullOrWhiteSpace(introPath))
        {
            introPath = "introduction/index.html";
        }

        introPath = introPath.Replace('\\', '/');
        return introPath.TrimStart('/');
    }

    private static string BuildDocsUrl(string introRelativePath, string? requestPath)
    {
        var safeRelativePath = introRelativePath.Replace('\\', '/');
        if (!safeRelativePath.StartsWith('/'))
        {
            safeRelativePath = "/" + safeRelativePath;
        }

        var safeRequestPath = string.IsNullOrWhiteSpace(requestPath) ? DefaultDocsRequestPath : requestPath;
        if (!safeRequestPath.StartsWith('/'))
        {
            safeRequestPath = "/" + safeRequestPath;
        }

        return (safeRequestPath + safeRelativePath).Replace("//", "/");
    }

    private static bool IsSafeChildPath(string root, string candidate)
    {
        var normalizedRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedCandidate = Path.GetFullPath(candidate);

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return normalizedCandidate.StartsWith(normalizedRoot, comparison);
    }

    public record DocsLinkResponse(string Url);
}
