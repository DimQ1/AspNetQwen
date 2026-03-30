#nullable enable

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// In-process server implementation for Android hosting.
/// </summary>
internal sealed class AndroidInProcessServer : IServer
{
    private readonly IFeatureCollection _features;
    private readonly ILogger<AndroidInProcessServer> _logger;
    private readonly AndroidInProcessHostingOptions _options;
    private RequestDelegate? _requestDelegate;
    private bool _isStarted;
    private bool _disposed;

    public AndroidInProcessServer(
        ILogger<AndroidInProcessServer> logger,
        AndroidInProcessHostingOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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

        _requestDelegate = context => application.ProcessRequestAsync((TContext)context);
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

        var stopwatch = Stopwatch.StartNew();
        AndroidInProcessDiagnostics.RecordRequestStart();

        // Validate request body size
        if (request.Body != null && request.Body.Length > _options.MaxRequestBodySize)
        {
            AndroidInProcessDiagnostics.RecordRequestFailure(stopwatch.ElapsedMilliseconds);
            Log.RequestBodyTooLarge(_logger, request.Body.Length, _options.MaxRequestBodySize, request.Path);
            return CreateErrorResponse(413, "Payload Too Large", $"Request body size ({request.Body.Length} bytes) exceeds the maximum allowed size ({_options.MaxRequestBodySize} bytes).");
        }

        var httpContext = new DefaultHttpContext();
        var featureFactory = new AndroidRequestFeatureCollectionFactory(request);
        httpContext.Features = featureFactory.CreateFeatureCollection();
        httpContext.RequestServices = null; // Will be set by hosting infrastructure

        try
        {
            Log.RequestStarted(_logger, request.Method, request.Path);
            await _requestDelegate(httpContext);
            Log.RequestFinished(_logger, httpContext.Response.StatusCode, request.Path);
            
            var responseAdapter = new AndroidResponseAdapter(httpContext.Response, _options.MaxResponseBodySize);
            var response = await responseAdapter.CaptureResponseAsync(cancellationToken);

            // Validate response body size
            if (response.Body.Length > _options.MaxResponseBodySize)
            {
                AndroidInProcessDiagnostics.RecordRequestFailure(stopwatch.ElapsedMilliseconds);
                Log.ResponseBodyTooLarge(_logger, response.Body.Length, _options.MaxResponseBodySize, request.Path);
                return CreateErrorResponse(500, "Internal Server Error", $"Response body size ({response.Body.Length} bytes) exceeds the maximum allowed size ({_options.MaxResponseBodySize} bytes).");
            }

            AndroidInProcessDiagnostics.RecordRequestSuccess(stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AndroidInProcessDiagnostics.RecordRequestFailure(stopwatch.ElapsedMilliseconds);
            Log.RequestCancelled(_logger, request.Path);
            throw;
        }
        catch (Exception ex)
        {
            AndroidInProcessDiagnostics.RecordRequestFailure(stopwatch.ElapsedMilliseconds);
            Log.RequestFailed(_logger, ex.GetType().Name, ex.Message, request.Path);
            return CreateErrorResponse(500, "Internal Server Error", $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static AndroidInProcessResponse CreateErrorResponse(int statusCode, string reasonPhrase, string message)
    {
        return new AndroidInProcessResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "text/plain"
            },
            Body = System.Text.Encoding.UTF8.GetBytes($"{reasonPhrase}: {message}")
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

        private static readonly Action<ILogger, long, long, string?, Exception?> _requestBodyTooLarge = LoggerMessage.Define<long, long, string?>(
            LogLevel.Warning,
            new EventId(14, "RequestBodyTooLarge"),
            "Request body too large: {ActualSize} bytes (max: {MaxSize}) for {Path}");

        private static readonly Action<ILogger, long, long, string?, Exception?> _responseBodyTooLarge = LoggerMessage.Define<long, long, string?>(
            LogLevel.Error,
            new EventId(15, "ResponseBodyTooLarge"),
            "Response body too large: {ActualSize} bytes (max: {MaxSize}) for {Path}");

        public static void ServerStarting(ILogger logger) => _serverStarting(logger, null);
        public static void ServerStarted(ILogger logger) => _serverStarted(logger, null);
        public static void ServerStopping(ILogger logger) => _serverStopping(logger, null);
        public static void ServerStopped(ILogger logger) => _serverStopped(logger, null);
        public static void ServerDisposed(ILogger logger) => _serverDisposed(logger, null);
        public static void RequestStarted(ILogger logger, string? method, string? path) => _requestStarted(logger, method, path, null);
        public static void RequestFinished(ILogger logger, int statusCode, string? path) => _requestFinished(logger, statusCode, path, null);
        public static void RequestFailed(ILogger logger, string exceptionType, string message, string? path) => _requestFailed(logger, exceptionType, message, path, null);
        public static void RequestCancelled(ILogger logger, string? path) => _requestCancelled(logger, path, null);
        public static void RequestBodyTooLarge(ILogger logger, long actualSize, long maxSize, string? path) => _requestBodyTooLarge(logger, actualSize, maxSize, path, null);
        public static void ResponseBodyTooLarge(ILogger logger, long actualSize, long maxSize, string? path) => _responseBodyTooLarge(logger, actualSize, maxSize, path, null);
    }
}
