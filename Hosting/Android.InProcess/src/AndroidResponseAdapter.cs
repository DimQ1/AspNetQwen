#nullable enable

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// Adapter for capturing ASP.NET Core response data.
/// </summary>
internal sealed class AndroidResponseAdapter
{
    private readonly HttpResponse _response;

    public AndroidResponseAdapter(HttpResponse response)
    {
        _response = response ?? throw new ArgumentNullException(nameof(response));
    }

    public async Task<AndroidInProcessResponse> CaptureResponseAsync()
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
            result.Body = await ReadAllBytesAsync(ms);
        }
        else
        {
            result.Body = Array.Empty<byte>();
        }

        return result;
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}
