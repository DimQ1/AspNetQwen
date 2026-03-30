#nullable enable

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// Hosted service that manages the lifetime of the Android in-process server.
/// </summary>
[SupportedOSPlatform("android")]
internal sealed class AndroidHostingLifetimeService : IHostedService
{
    private readonly AndroidInProcessServer _server;
    private readonly AndroidInProcessHostingOptions _options;
    private readonly ILogger<AndroidHostingLifetimeService>? _logger;

    public AndroidHostingLifetimeService(
        AndroidInProcessServer server,
        AndroidInProcessHostingOptions options,
        ILogger<AndroidHostingLifetimeService>? logger = null)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting Android in-process hosting lifetime service");
        
        // The server is started automatically by ASP.NET Core hosting infrastructure
        // This service primarily coordinates lifecycle events
        
        _logger?.LogInformation("Android in-process hosting lifetime service started");
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Stopping Android in-process hosting lifetime service");
        
        using var timeoutCts = new CancellationTokenSource(_options.ShutdownTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        
        try
        {
            await _server.StopAsync(linkedCts.Token);
            _logger?.LogInformation("Android in-process server stopped gracefully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("Android in-process server stop was cancelled");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger?.LogWarning("Android in-process server stop timed out after {ShutdownTimeout}", _options.ShutdownTimeout);
        }
        
        _logger?.LogInformation("Android in-process hosting lifetime service stopped");
    }
}
