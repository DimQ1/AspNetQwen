using Microsoft.AspNetCore.Hosting.Android.InProcess;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.Hosting;

namespace AndroidInProcessSample;

public class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        // Configure MAUI essentials
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add Android in-process hosting for ASP.NET Core
        builder.AddAndroidInProcessHosting(options =>
        {
            options.BaseAddress = new Uri("https://app.local");
            options.EnableWebViewAdapter = true;
            options.EnableHttpClientAdapter = true;
        });

        // Configure ASP.NET Core middleware and endpoints
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseAndroidInProcessHosting();
            webHostBuilder.Configure(app =>
            {
                // Static file serving simulation
                app.MapGet("/", async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>ASP.NET Core on Android</title>
    <style>
        body { font-family: sans-serif; padding: 20px; }
        h1 { color: #512bd4; }
        .info { background: #f0f0f0; padding: 15px; border-radius: 8px; margin-top: 20px; }
    </style>
</head>
<body>
    <h1>Hello from ASP.NET Core on Android!</h1>
    <p>This page is served by the in-process ASP.NET Core server.</p>
    <div class='info'>
        <h2>API Endpoints:</h2>
        <ul>
            <li><a href='/api/time'>GET /api/time</a> - Current server time</li>
            <li><a href='/api/data'>GET /api/data</a> - Sample JSON data</li>
        </ul>
    </div>
    <script>
        console.log('Page loaded from ASP.NET Core pipeline');
    </script>
</body>
</html>");
                });

                // API endpoint: current time
                app.MapGet("/api/time", () => new
                {
                    Timestamp = DateTime.UtcNow,
                    Message = "Current UTC time from ASP.NET Core"
                });

                // API endpoint: sample data
                app.MapGet("/api/data", () => new
                {
                    Items = new[]
                    {
                        new { Id = 1, Name = "Item 1" },
                        new { Id = 2, Name = "Item 2" },
                        new { Id = 3, Name = "Item 3" }
                    },
                    TotalCount = 3
                });

                // API endpoint: echo POST body
                app.MapPost("/api/echo", async (HttpRequest request) =>
                {
                    using var reader = new StreamReader(request.Body);
                    var body = await reader.ReadToEndAsync();
                    return Results.Json(new
                    {
                        Received = true,
                        BodyLength = body.Length,
                        Body = body
                    });
                });
            });
        });

#if DEBUG
        builder.Configuration["Logging:LogLevel:Default"] = "Debug";
        builder.Configuration["Logging:LogLevel:Microsoft.AspNetCore.Hosting.Android.InProcess"] = "Debug";
#endif

        return builder.Build();
    }
}

public class App : Application
{
    public App()
    {
        MainPage = new MainPage();
    }
}

public class MainPage : ContentPage
{
    private WebView _webView;
    private IServiceProvider? _serviceProvider;

    public MainPage()
    {
        Title = "ASP.NET Core Android Sample";

        _webView = new WebView
        {
            Source = "https://app.local/",
        };

        Content = _webView;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler?.MauiContext?.Services != null)
        {
            _serviceProvider = Handler.MauiContext.Services;
            
            // Set up WebView adapter to intercept requests
            var webViewPlatform = _webView.Handler?.PlatformView;
            if (webViewPlatform != null)
            {
                try
                {
                    var adapter = _serviceProvider.GetRequiredService<AndroidWebViewRequestAdapter>();
                    // Note: In a real implementation, you would set the adapter here
                    // This is a simplified example
                    System.Diagnostics.Debug.WriteLine("WebView adapter available");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting adapter: {ex}");
                }
            }
        }
    }
}
