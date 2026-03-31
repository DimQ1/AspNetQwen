#nullable enable

using System.Net.Http;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// HttpClient message handler that intercepts requests to the configured base address
/// and dispatches them to the ASP.NET Core in-process pipeline.
/// </summary>
[SupportedOSPlatform("android")]
public sealed class AndroidHttpClientMessageHandler : HttpMessageHandler
{
    private readonly AndroidInProcessServer _server;
    private readonly Uri _baseAddress;
    private readonly HttpMessageInvoker? _innerHandlerInvoker;
    private readonly ILogger<AndroidHttpClientMessageHandler>? _logger;

    public AndroidHttpClientMessageHandler(
        AndroidInProcessServer server,
        Uri baseAddress,
        HttpMessageHandler? innerHandler = null,
        ILogger<AndroidHttpClientMessageHandler>? logger = null)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
        _innerHandlerInvoker = innerHandler is null ? null : new HttpMessageInvoker(innerHandler);
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var requestUri = request.RequestUri;

        // Only intercept requests targeting our configured base address
        if (requestUri == null || !requestUri.ToString().StartsWith(_baseAddress.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            if (_innerHandlerInvoker != null)
            {
                return await _innerHandlerInvoker.SendAsync(request, cancellationToken);
            }

            throw new InvalidOperationException($"Request URI {requestUri} does not match the configured base address {_baseAddress}. Use the configured base address for in-process requests or provide an inner handler for external requests.");
        }

        try
        {
            return await InterceptRequestAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Adapter error: Failed to intercept HttpClient request");
            return CreateErrorResponse(ex);
        }
    }

    private async Task<HttpResponseMessage> InterceptRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        var method = request.Method.Method;
        
        var inProcessRequest = new AndroidInProcessRequest
        {
            Method = method,
            Path = uri.PathAndQuery,
            QueryString = uri.Query ?? string.Empty,
            Headers = ConvertHeaders(request.Headers),
            CancellationToken = cancellationToken,
        };

        // Add content headers if present
        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                inProcessRequest.Headers[header.Key] = string.Join(", ", header.Value);
            }

            // Read request body
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            inProcessRequest.Body = contentBytes;
        }

        var response = await _server.DispatchRequestAsync(inProcessRequest, cancellationToken);

        var httpResponseMessage = new HttpResponseMessage((System.Net.HttpStatusCode)response.StatusCode)
        {
            RequestMessage = request,
        };

        foreach (var header in response.Headers)
        {
            httpResponseMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (response.Body.Length > 0)
        {
            httpResponseMessage.Content = new ByteArrayContent(response.Body);
        }
        else
        {
            httpResponseMessage.Content = new ByteArrayContent(Array.Empty<byte>());
        }

        return httpResponseMessage;
    }

    private static Dictionary<string, string> ConvertHeaders(System.Net.Http.Headers.HttpHeaders headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            result[header.Key] = string.Join(", ", header.Value);
        }
        return result;
    }

    private static HttpResponseMessage CreateErrorResponse(Exception exception)
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
        {
            Content = new StringContent($"Internal Server Error: {exception.Message}")
        };
        return response;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerHandlerInvoker?.Dispose();
        }
        base.Dispose(disposing);
    }
}
