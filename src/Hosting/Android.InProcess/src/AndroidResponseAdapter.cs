#nullable enable

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// Adapter for capturing ASP.NET Core response data.
/// </summary>
internal sealed class AndroidResponseAdapter
{
    private readonly HttpResponse _response;
    private readonly long _maxResponseBodySize;

    public AndroidResponseAdapter(HttpResponse response, long maxResponseBodySize)
    {
        _response = response ?? throw new ArgumentNullException(nameof(response));
        _maxResponseBodySize = maxResponseBodySize;
    }

    public async Task<AndroidInProcessResponse> CaptureResponseAsync(CancellationToken cancellationToken)
    {
        var result = new AndroidInProcessResponse
        {
            StatusCode = _response.StatusCode,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        };

        foreach (var header in _response.Headers)
        {
            if (!string.IsNullOrEmpty(header.Value))
            {
                result.Headers[header.Key] = header.Value.ToString()!;
            }
        }

        var bodyStream = _response.Body;
        if (bodyStream is MemoryStream ms && ms.CanSeek)
        {
            ms.Position = 0;
            
            // Check size before reading
            if (ms.Length > _maxResponseBodySize)
            {
                // Still read the body to allow caller to handle the error
                result.Body = await ReadAllBytesAsync(ms, cancellationToken);
            }
            else
            {
                result.Body = await ReadAllBytesAsync(ms, cancellationToken);
            }
        }
        else
        {
            result.Body = Array.Empty<byte>();
        }

        return result;
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }
}
