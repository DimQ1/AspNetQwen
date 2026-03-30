# AspNetQwen - ASP.NET Core Android In-Process Hosting

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![License](https://img.shields.io/badge/license-MIT-blue)]()
[![NuGet](https://img.shields.io/badge/nuget-preview-orange)]()

**Run ASP.NET Core on Android without Kestrel** - A dedicated NuGet package that enables in-process hosting of ASP.NET Core middleware and endpoints on Android devices.

## Overview

AspNetQwen provides a **no-Kestrel, in-process server architecture** for Android applications:

- ✅ Custom in-process server via `IServer` interface
- ✅ In-memory request execution without TCP listeners or port binding
- ✅ Full Android/MAUI lifecycle integration
- ✅ WebView and HttpClient adapters for seamless consumption
- ✅ Pure C# implementation - no Java/Kotlin required

## Quick Start

### Installation

Add the NuGet package to your Android or MAUI project:

```bash
dotnet add package Microsoft.AspNetCore.Hosting.Android.InProcess
```

### Basic Usage

#### 1. Configure ASP.NET Core Hosting

In your MAUI app (`MauiProgram.cs`) or Android activity:

```csharp
using Microsoft.AspNetCore.Hosting.Android.InProcess;

var builder = MauiApp.CreateBuilder();

// Add Android in-process hosting
builder.Services.AddAndroidInProcessHosting(options =>
{
    options.BaseAddress = new Uri("https://app.local");
    options.EnableWebViewAdapter = true;
    options.EnableHttpClientAdapter = true;
    options.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
    options.MaxResponseBodySize = 10 * 1024 * 1024; // 10 MB
});

// Add your ASP.NET Core services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure middleware pipeline
app.UseRouting();
app.MapGet("/api/hello", () => "Hello from ASP.NET Core on Android!");
app.MapPost("/api/data", (MyData data) => Results.Ok(new { Received = data }));

app.Run();
```

#### 2. Use with WebView

```csharp
var webView = new WebView(Context);
webView.LoadUrl("https://app.local/index.html");

// The WebView will automatically intercept local URLs
// and route them through the ASP.NET Core pipeline
```

#### 3. Use with HttpClient

```csharp
using var httpClient = new HttpClient(
    new AndroidHttpClientMessageHandler("https://app.local")
);

var response = await httpClient.GetAsync("https://app.local/api/hello");
var content = await response.Content.ReadAsStringAsync();
// content = "Hello from ASP.NET Core on Android!"
```

## Features

### MVP Scope (v3.1)

✅ **Core Functionality**
- In-process `IServer` implementation
- Request dispatch to ASP.NET Core middleware and endpoints
- Response support: status codes, headers, text/binary body
- Android lifecycle integration (start/stop/background/resume)
- Dependency injection and configuration support
- Logging and diagnostics

✅ **Adapters**
- WebView adapter for local URL interception
- HttpClient message handler for in-app requests
- Unified dispatch through the same pipeline

✅ **Supported ASP.NET Core Features**
- Middleware pipeline execution
- Endpoint routing (minimal APIs and controllers)
- Request/response headers and cookies
- Basic request body reading (buffered)
- Basic response writing (text and binary)
- `IHttpContextAccessor` support
- Cancellation token propagation

### Post-MVP Features (Planned)

- WebSockets support
- SignalR integration
- Server-Sent Events (SSE)
- HTTP/2 and HTTP/3 transport features
- Unbuffered large-body streaming
- External URL redirects from WebView

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Android Application                    │
├─────────────────────────────────────────────────────────┤
│  ┌──────────────┐         ┌──────────────────────────┐  │
│  │   WebView    │         │      HttpClient          │  │
│  └──────┬───────┘         └────────────┬─────────────┘  │
│         │                              │                │
│         └──────────────┬───────────────┘                │
│                        ▼                                │
│         ┌──────────────────────────────┐               │
│         │  AndroidInProcessServer      │               │
│         │  (IServer Implementation)    │               │
│         └──────────────┬───────────────┘               │
│                        ▼                                │
│         ┌──────────────────────────────┐               │
│         │  Request Dispatcher          │               │
│         └──────────────┬───────────────┘               │
│                        ▼                                │
│         ┌──────────────────────────────┐               │
│         │  ASP.NET Core Pipeline       │               │
│         │  - Middleware                │               │
│         │  - Routing                   │               │
│         │  - Endpoints                 │               │
│         │  - DI, Logging, Config       │               │
│         └──────────────────────────────┘               │
└─────────────────────────────────────────────────────────┘
```

## Configuration Options

```csharp
public class AndroidInProcessHostingOptions
{
    public Uri BaseAddress { get; set; } = new Uri("https://app.local");
    public bool EnableWebViewAdapter { get; set; } = true;
    public bool EnableHttpClientAdapter { get; set; } = true;
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public bool BufferRequestBody { get; set; } = true;
    public bool BufferResponseBody { get; set; } = true;
    public long MaxRequestBodySize { get; set; } = 10 * 1024 * 1024; // 10 MB
    public long MaxResponseBodySize { get; set; } = 10 * 1024 * 1024; // 10 MB
}
```

## Android Lifecycle Behavior

| Lifecycle Event | Server Behavior |
|----------------|-----------------|
| **Start** (OnCreate) | Server starts via `AndroidHostingLifetimeService.StartAsync` |
| **Background** | Server continues running, serves active WebView requests |
| **Resume** | No action required, server already running |
| **Stop** (OnDestroy) | Graceful shutdown with configured timeout, in-flight requests cancelled |
| **Recreation** | Single server instance per process lifetime (documented limitation) |

## Performance Goals

- **Startup overhead**: ≤ 200 ms on mid-range devices (Android 10+)
- **Request latency**: 95th percentile ≤ 50 ms (excluding application work)
- **Memory overhead**: ≤ 5 MB additional RSS on empty pipeline
- **Default payload limits**: 10 MB for request/response bodies (configurable)

## Testing

### Supported Android API Levels

- API 21 (Android 5.0) - Minimum supported
- API 34/35 (Latest stable)
- API 36+ (Latest available)

### Run Tests

```bash
# Unit tests
dotnet test src/Hosting/Android.InProcess/test/Microsoft.AspNetCore.Hosting.Android.InProcess.Tests.csproj

# Integration tests
dotnet test --filter "Category=Integration"

# Android smoke tests (requires emulator/device)
dotnet test --filter "Category=AndroidSmoke"
```

## Diagnostics and Observability

### Logging

The package provides structured logging with the following events:

- `Server started` / `Server stopped`
- `Request started` (request ID, method, path)
- `Request finished` (status code, elapsed duration)
- `Request failed` (exception type and message)
- `Adapter error` (source: WebView or HttpClient)

### Metrics

Available via `System.Diagnostics.Metrics`:

- `aspnetcore.android.requests.total` - Total dispatched requests
- `aspnetcore.android.requests.failed` - Failed requests count
- `aspnetcore.android.request.duration` - Request duration histogram

## Security Considerations

⚠️ **Important Security Boundaries:**

- Only URLs starting with `BaseAddress` are dispatched to the pipeline
- All other URLs are forwarded without modification
- Requests are assumed to originate from within the same app process
- External untrusted traffic is explicitly unsupported
- Do not grant WebView filesystem access through the adapter unless required
- Document which response headers are forwarded to WebView

## Known Limitations

1. **No network serving**: This package does not expose ASP.NET Core to external network requests
2. **Buffered bodies only**: All request/response bodies are fully buffered in MVP
3. **Single instance per process**: Server recreation on app rotation is not handled in MVP
4. **WebView edge cases**: Some WebView behaviors may differ across Android versions
5. **No WebSockets/SignalR**: Real-time communication features are deferred to post-MVP

See [PACKAGE.md](src/Hosting/Android.InProcess/PACKAGE.md) for detailed limitations.

## Project Structure

```
src/Hosting/Android.InProcess/
├── src/
│   ├── AndroidInProcessServer.cs
│   ├── AndroidInProcessRequestDispatcher.cs
│   ├── AndroidWebViewRequestAdapter.cs
│   ├── AndroidHttpClientMessageHandler.cs
│   ├── AndroidHostingLifetimeService.cs
│   ├── AndroidInProcessDiagnostics.cs
│   ├── AndroidInProcessHostingOptions.cs
│   ├── AndroidInProcessHostBuilderExtensions.cs
│   └── AndroidInProcessWebHostBuilderExtensions.cs
├── test/
│   ├── Unit/
│   ├── Integration/
│   └── AndroidSmoke/
├── samples/
│   └── AndroidInProcessSample/
└── PACKAGE.md
```

## Roadmap

- [x] Phase 0: Feasibility Spike
- [x] Phase 1: Package Skeleton
- [x] Phase 2: In-Process Server Core
- [x] Phase 3: Android Adapters
- [x] Phase 4: Hardening and Release Readiness
- [ ] Post-MVP: WebSockets Support
- [ ] Post-MVP: SignalR Integration
- [ ] Post-MVP: Streaming Support

## Contributing

Contributions are welcome! Please read our contributing guidelines before submitting PRs.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Related Projects

- [ASP.NET Core Repository](https://github.com/dotnet/aspnetcore)
- [.NET MAUI](https://github.com/dotnet/maui)
- [Android Developers](https://developer.android.com/)

---

**Version**: 3.1.0-preview  
**Target Framework**: net9.0-android and higher  
**Minimum .NET Version**: .NET 9  
**Platform**: Android (API 21+)
