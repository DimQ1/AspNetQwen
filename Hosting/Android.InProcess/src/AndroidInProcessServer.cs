#nullable enable

using System.Text;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// In-process server implementation for Android hosting.
/// </summary>
public sealed class AndroidInProcessServer : IServer
{
    private readonly IFeatureCollection _features;
    private readonly ILogger<AndroidInProcessServer> _logger;
    private RequestDelegate? _requestDelegate;
    private bool _isStarted;
    private bool _disposed;

    public AndroidInProcessServer(ILogger<AndroidInProcessServer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _features = new FeatureCollection();
        
        Log.ServerStarting(_logger);
    }

    public IFeatureCollection Features => _features;

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AndroidInProcessServer));
        }

        if (_isStarted)
        {
            throw new InvalidOperationException("Server has already been started.");
        }

        _requestDelegate = async context =>
        {
            var appContext = application.CreateContext(context.Features);
            Exception? dispatchException = null;

            try
            {
                await application.ProcessRequestAsync(appContext);
            }
            catch (Exception ex)
            {
                dispatchException = ex;
                throw;
            }
            finally
            {
                application.DisposeContext(appContext, dispatchException);
            }
        };

        _isStarted = true;

        Log.ServerStarted(_logger);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AndroidInProcessServer));
        }

        Log.ServerStopping(_logger);
        
        _requestDelegate = null;
        _isStarted = false;
        
        await Task.CompletedTask;
        
        Log.ServerStopped(_logger);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Log.ServerDisposed(_logger);
    }

    internal async Task<AndroidInProcessResponse> DispatchRequestAsync(AndroidInProcessRequest request, CancellationToken cancellationToken)
    {
        if (!_isStarted || _requestDelegate == null)
        {
            throw new InvalidOperationException("Server is not started. Call StartAsync first.");
        }

        var featureFactory = new AndroidRequestFeatureCollectionFactory(request);
        var httpContext = new DefaultHttpContext(featureFactory.CreateFeatureCollection());

        try
        {
            Log.RequestStarted(_logger, request.Method, request.Path);
            await _requestDelegate(httpContext);
            Log.RequestFinished(_logger, httpContext.Response.StatusCode, request.Path);

            var responseAdapter = new AndroidResponseAdapter(httpContext.Response);
            return await responseAdapter.CaptureResponseAsync();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Log.RequestCancelled(_logger, request.Path);
            throw;
        }
        catch (Exception ex)
        {
            Log.RequestFailed(_logger, ex.GetType().Name, ex.Message, request.Path);
            return CreateErrorResponse(ex);
        }
    }

    private static AndroidInProcessResponse CreateErrorResponse(Exception exception)
    {
        return new AndroidInProcessResponse
        {
            StatusCode = 500,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            Body = Encoding.UTF8.GetBytes($"Internal Server Error: {exception.Message}")
        };
    }

    private static class Log
    {
        private static readonly Action<ILogger, Exception?> _serverStarting = LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, "ServerStarting"),
            "Android in-process server starting");

        private static readonly Action<ILogger, Exception?> _serverStarted = LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2, "ServerStarted"),
            "Android in-process server started");

        private static readonly Action<ILogger, Exception?> _serverStopping = LoggerMessage.Define(
            LogLevel.Information,
            new EventId(3, "ServerStopping"),
            "Android in-process server stopping");

        private static readonly Action<ILogger, Exception?> _serverStopped = LoggerMessage.Define(
            LogLevel.Information,
            new EventId(4, "ServerStopped"),
            "Android in-process server stopped");

        private static readonly Action<ILogger, Exception?> _serverDisposed = LoggerMessage.Define(
            LogLevel.Information,
            new EventId(5, "ServerDisposed"),
            "Android in-process server disposed");

        private static readonly Action<ILogger, string?, string?, Exception?> _requestStarted = LoggerMessage.Define<string?, string?>(
            LogLevel.Information,
            new EventId(10, "RequestStarted"),
            "Request started: {Method} {Path}");

        private static readonly Action<ILogger, int, string?, Exception?> _requestFinished = LoggerMessage.Define<int, string?>(
            LogLevel.Information,
            new EventId(11, "RequestFinished"),
            "Request finished: {StatusCode} {Path}");

        private static readonly Action<ILogger, string?, string?, string?, Exception?> _requestFailed = LoggerMessage.Define<string?, string?, string?>(
            LogLevel.Error,
            new EventId(12, "RequestFailed"),
            "Request failed: {ExceptionType} {Message} {Path}");

        private static readonly Action<ILogger, string?, Exception?> _requestCancelled = LoggerMessage.Define<string?>(
            LogLevel.Warning,
            new EventId(13, "RequestCancelled"),
            "Request cancelled: {Path}");

        public static void ServerStarting(ILogger logger) => _serverStarting(logger, null);
        public static void ServerStarted(ILogger logger) => _serverStarted(logger, null);
        public static void ServerStopping(ILogger logger) => _serverStopping(logger, null);
        public static void ServerStopped(ILogger logger) => _serverStopped(logger, null);
        public static void ServerDisposed(ILogger logger) => _serverDisposed(logger, null);
        public static void RequestStarted(ILogger logger, string? method, string? path) => _requestStarted(logger, method, path, null);
        public static void RequestFinished(ILogger logger, int statusCode, string? path) => _requestFinished(logger, statusCode, path, null);
        public static void RequestFailed(ILogger logger, string exceptionType, string message, string? path) => _requestFailed(logger, exceptionType, message, path, null);
        public static void RequestCancelled(ILogger logger, string? path) => _requestCancelled(logger, path, null);
    }
}
