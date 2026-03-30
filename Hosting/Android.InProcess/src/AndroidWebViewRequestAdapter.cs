#nullable enable

using Android.Webkit;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// WebView request adapter that intercepts app-local URLs and dispatches them to the ASP.NET Core pipeline.
/// </summary>
[SupportedOSPlatform("android")]
public sealed class AndroidWebViewRequestAdapter : WebViewClient
{
    private readonly AndroidInProcessServer _server;
    private readonly Uri _baseAddress;
    private readonly ILogger<AndroidWebViewRequestAdapter>? _logger;

    public AndroidWebViewRequestAdapter(
        AndroidInProcessServer server,
        Uri baseAddress,
        ILogger<AndroidWebViewRequestAdapter>? logger = null)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
        _logger = logger;
    }

    public override WebResourceResponse? ShouldInterceptRequest(WebView? view, WebResourceRequest? request)
    {
        if (view == null || request == null || request.Url == null)
        {
            return base.ShouldInterceptRequest(view, request);
        }

        var uri = request.Url;
        
        // Only intercept requests targeting our configured base address
        if (!uri.ToString().StartsWith(_baseAddress.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return base.ShouldInterceptRequest(view, request);
        }

        try
        {
            return InterceptRequestAsync(request).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Adapter error: Failed to intercept WebView request");
            return CreateErrorResponse("Internal adapter error");
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "WebView adapter requires reflection for MIME type mapping")]
    private async Task<WebResourceResponse?> InterceptRequestAsync(WebResourceRequest request)
    {
        var uri = request.Url!;
        var method = request.Method ?? "GET";
        var headers = request.RequestHeaders;
        
        var inProcessRequest = new AndroidInProcessRequest
        {
            Method = method,
            Path = uri.PathAndQuery,
            QueryString = uri.Query ?? string.Empty,
            Headers = headers != null ? ConvertHeaders(headers) : new Dictionary<string, string>(),
            CancellationToken = CancellationToken.None,
        };

        // Read request body for POST/PUT/PATCH
        if (method is "POST" or "PUT" or "PATCH" && request.InputStream != null)
        {
            using var ms = new MemoryStream();
            await request.InputStream.CopyToAsync(ms);
            inProcessRequest.Body = ms.ToArray();
        }

        var response = await _server.DispatchRequestAsync(inProcessRequest, CancellationToken.None);

        var mimeType = GetMimeType(uri.Path);
        var encoding = "utf-8";
        var reasonPhrase = GetReasonPhrase(response.StatusCode);

        var responseHeaders = new Dictionary<string, string>();
        foreach (var header in response.Headers)
        {
            responseHeaders[header.Key] = header.Value;
        }

        var bodyStream = response.Body.Length > 0 ? new MemoryStream(response.Body) : Stream.Null;

        return new WebResourceResponse(mimeType, encoding, bodyStream)
        {
            StatusCode = response.StatusCode,
            ReasonPhrase = reasonPhrase,
            ResponseHeaders = responseHeaders,
        };
    }

    private static Dictionary<string, string> ConvertHeaders(IDictionary<string, string>? androidHeaders)
    {
        if (androidHeaders == null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, string>(androidHeaders.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var header in androidHeaders)
        {
            result[header.Key] = header.Value;
        }
        return result;
    }

    private static string GetMimeType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".xml" => "application/xml",
            ".txt" => "text/plain",
            _ => "application/octet-stream",
        };
    }

    private static string GetReasonPhrase(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            201 => "Created",
            204 => "No Content",
            301 => "Moved Permanently",
            302 => "Found",
            304 => "Not Modified",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            413 => "Payload Too Large",
            500 => "Internal Server Error",
            503 => "Service Unavailable",
            _ => "Unknown",
        };
    }

    private static WebResourceResponse CreateErrorResponse(string message)
    {
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes(message);
        return new WebResourceResponse("text/plain", "utf-8", new MemoryStream(bodyBytes))
        {
            StatusCode = 500,
            ReasonPhrase = "Internal Server Error",
        };
    }
}
