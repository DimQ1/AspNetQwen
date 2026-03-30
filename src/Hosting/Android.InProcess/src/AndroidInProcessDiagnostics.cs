#nullable enable

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// Diagnostics and metrics for Android in-process hosting.
/// </summary>
internal sealed class AndroidInProcessDiagnostics
{
    private static readonly Meter _meter = new("aspnetcore.android");
    
    private static readonly Counter<long> _requestsTotal = _meter.CreateCounter<long>(
        "aspnetcore.android.requests.total",
        description: "Total number of dispatched requests");

    private static readonly Counter<long> _requestsFailed = _meter.CreateCounter<long>(
        "aspnetcore.android.requests.failed",
        description: "Number of requests that resulted in an unhandled exception");

    private static readonly Histogram<double> _requestDuration = _meter.CreateHistogram<double>(
        "aspnetcore.android.request.duration",
        unit: "ms",
        description: "Request duration in milliseconds");

    public static void RecordRequestStart()
    {
        _requestsTotal.Add(1);
    }

    public static void RecordRequestSuccess(double durationMs)
    {
        _requestDuration.Record(durationMs);
    }

    public static void RecordRequestFailure(double durationMs)
    {
        _requestsFailed.Add(1);
        _requestDuration.Record(durationMs);
    }

    public static void LogAdapterError(ILogger logger, string adapterType, string message)
    {
        logger.LogError("Adapter error ({AdapterType}): {Message}", adapterType, message);
    }
}
