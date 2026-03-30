#nullable enable

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// Options for configuring Android in-process hosting.
/// </summary>
public class AndroidInProcessHostingOptions
{
    /// <summary>
    /// The base address for the in-process server.
    /// Default is https://app.local.
    /// </summary>
    public Uri BaseAddress { get; set; } = new Uri("https://app.local");

    /// <summary>
    /// Enables the WebView request adapter.
    /// Default is true.
    /// </summary>
    public bool EnableWebViewAdapter { get; set; } = true;

    /// <summary>
    /// Enables the HttpClient message handler adapter.
    /// Default is true.
    /// </summary>
    public bool EnableHttpClientAdapter { get; set; } = true;

    /// <summary>
    /// Timeout for graceful shutdown.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether to buffer the request body before dispatching.
    /// Default is true.
    /// </summary>
    public bool BufferRequestBody { get; set; } = true;

    /// <summary>
    /// Whether to buffer the response body before returning.
    /// Default is true.
    /// </summary>
    public bool BufferResponseBody { get; set; } = true;

    /// <summary>
    /// Maximum allowed request body size in bytes.
    /// Default is 10 MB.
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Maximum allowed response body size in bytes.
    /// Default is 10 MB.
    /// </summary>
    public long MaxResponseBodySize { get; set; } = 10 * 1024 * 1024;
}
