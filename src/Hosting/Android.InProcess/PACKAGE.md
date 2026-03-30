# Microsoft.AspNetCore.Hosting.Android.InProcess

In-process ASP.NET Core hosting for Android without Kestrel. This package enables running ASP.NET Core middleware and endpoints on Android devices via WebView and in-app HttpClient adapters.

## Features

- **No Kestrel required**: Runs entirely in-process without TCP sockets or port binding
- **WebView integration**: Intercept app-local URLs and serve content from ASP.NET Core pipeline
- **HttpClient adapter**: Use standard `HttpClient` to communicate with your ASP.NET Core endpoints
- **Full middleware support**: Run the complete ASP.NET Core middleware pipeline including routing, DI, logging, and configuration
- **Android lifecycle aware**: Graceful startup and shutdown integrated with Android application lifecycle

## Installation

```bash
dotnet add package Microsoft.AspNetCore.Hosting.Android.InProcess
```

## Requirements

- .NET 9.0 or higher
- Android API level 21 (Android 5.0) or higher
- MAUI Android or native Android project

## Quick Start

### 1. Configure Hosting

In your MAUI `MauiProgram.cs` or Android application entry point:

```csharp
using Microsoft.AspNetCore.Hosting.Android.InProcess;

var builder = MauiApp.CreateBuilder();

// Add Android in-process hosting
builder.AddAndroidInProcessHosting(options =>
{
    options.BaseAddress = new Uri("https://app.local");
    options.EnableWebViewAdapter = true;
    options.EnableHttpClientAdapter = true;
});

// Configure ASP.NET Core services and middleware
builder.ConfigureWebHost(webHostBuilder =>
{
    webHostBuilder.UseAndroidInProcessHosting();
    webHostBuilder.Configure(app =>
    {
        app.MapGet("/", () => "Hello from ASP.NET Core on Android!");
        app.MapGet("/api/data", () => new { Message = "Data from API" });
    });
});

// Continue with MAUI setup...
```

### 2. Use with WebView

Set up your WebView to use the adapter:

```csharp
using Microsoft.AspNetCore.Hosting.Android.InProcess;

// In your Android Activity or MAUI page
var webView = new WebView(this);
var adapter = serviceProvider.GetRequiredService<AndroidWebViewRequestAdapter>();
webView.Client = adapter;

// Load content from your ASP.NET Core pipeline
webView.LoadUrl("https://app.local/index.html");
```

### 3. Use with HttpClient

Make requests to your in-process endpoints:

```csharp
using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Android.InProcess;

var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
// Or directly use the handler:
var handler = serviceProvider.GetRequiredService<AndroidHttpClientMessageHandler>();
var httpClient = new HttpClient(handler);

// Request will be handled by your ASP.NET Core pipeline
var response = await httpClient.GetAsync("https://app.local/api/data");
var data = await response.Content.ReadAsStringAsync();
```

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `BaseAddress` | `https://app.local` | Base URI for intercepting requests |
| `EnableWebViewAdapter` | `true` | Enable WebView request interception |
| `EnableHttpClientAdapter` | `true` | Enable HttpClient message handler |
| `ShutdownTimeout` | `00:00:05` | Timeout for graceful shutdown |
| `BufferRequestBody` | `true` | Buffer request body before dispatch |
| `BufferResponseBody` | `true` | Buffer response body before return |
| `MaxRequestBodySize` | `10485760` (10MB) | Maximum request body size in bytes |
| `MaxResponseBodySize` | `10485760` (10MB) | Maximum response body size in bytes |

## Supported Features (MVP)

- ✅ Middleware pipeline execution
- ✅ Endpoint routing (minimal APIs, MVC controllers)
- ✅ Dependency injection
- ✅ Logging and configuration
- ✅ Request/response headers and cookies
- ✅ Basic request body reading (buffered)
- ✅ Basic response writing (text and binary)
- ✅ `IHttpContextAccessor`
- ✅ Cancellation token propagation

## Not Supported (Post-MVP)

- ❌ Public network serving (Kestrel)
- ❌ HTTP/2 / HTTP/3 transport features
- ❌ WebSockets
- ❌ SignalR
- ❌ Server-Sent Events (SSE)
- ❌ Unbuffered large-body streaming

## Android Lifecycle Behavior

- **Start**: Server starts when `AndroidHostingLifetimeService.StartAsync` is called (typically in `OnCreate`)
- **Background**: Server continues running when app moves to background
- **Resume**: No additional action required
- **Stop**: Graceful shutdown on application destruction with configured timeout

## Diagnostics

The package emits the following telemetry:

### Log Events
- `Server started` / `Server stopped`
- `Request started` (request ID, method, path)
- `Request finished` (status code, elapsed duration)
- `Request failed` (exception type and message)
- `Adapter error` (source: WebView or HttpClient, description)

### Metrics (System.Diagnostics.Metrics)
- `aspnetcore.android.requests.total` — Total dispatched request count
- `aspnetcore.android.requests.failed` — Count of failed requests
- `aspnetcore.android.request.duration` — Request duration histogram

## Security Considerations

This server is designed for **local-only, in-process** communication:

- Only URLs matching the configured `BaseAddress` are intercepted
- All requests are assumed to originate from within the same app process
- Do not use this server to process traffic from untrusted external sources
- Response headers are forwarded to WebView with standard security restrictions

## Troubleshooting

### WebView requests not being intercepted
- Ensure the WebView URL starts with the configured `BaseAddress`
- Verify the `AndroidWebViewRequestAdapter` is set as the `WebView.Client`
- Check that `EnableWebViewAdapter` is `true` in options

### HttpClient requests failing
- Ensure the request URI matches the configured `BaseAddress`
- If you need external requests, provide an inner handler for fallback
- Check that `EnableHttpClientAdapter` is `true` in options

### Server not starting
- Verify `AddAndroidInProcessHosting` is called before `Build()`
- Check application logs for startup errors
- Ensure the Android platform version is API 21 or higher

## License

MIT License - See LICENSE file for details.
