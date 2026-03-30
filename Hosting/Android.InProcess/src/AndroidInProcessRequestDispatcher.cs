#nullable enable

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// Represents an incoming request to the Android in-process server.
/// </summary>
internal sealed class AndroidInProcessRequest
{
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = "/";
    public string QueryString { get; set; } = string.Empty;
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public byte[]? Body { get; set; }
    public CancellationToken CancellationToken { get; set; }
}

/// <summary>
/// Represents a response from the Android in-process server.
/// </summary>
internal sealed class AndroidInProcessResponse
{
    public int StatusCode { get; set; } = 200;
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public byte[] Body { get; set; } = Array.Empty<byte>();
}
