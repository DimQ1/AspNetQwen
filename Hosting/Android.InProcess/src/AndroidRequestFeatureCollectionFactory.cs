#nullable enable

using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// Factory for creating ASP.NET Core feature collections from Android in-process requests.
/// </summary>
internal sealed class AndroidRequestFeatureCollectionFactory
{
    private readonly AndroidInProcessRequest _request;

    public AndroidRequestFeatureCollectionFactory(AndroidInProcessRequest request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public IFeatureCollection CreateFeatureCollection()
    {
        var features = new FeatureCollection();
        var requestBody = _request.Body is { Length: > 0 } ? new MemoryStream(_request.Body) : Stream.Null;
        var responseBody = new MemoryStream();

        var httpRequestFeature = new HttpRequestFeature
        {
            Method = _request.Method,
            Protocol = "HTTP/1.1",
            Scheme = "https",
            PathBase = string.Empty,
            Path = _request.Path,
            QueryString = _request.QueryString,
            RawTarget = string.Concat(_request.Path, _request.QueryString),
            Headers = CreateHeaders(_request.Headers),
            Body = requestBody,
        };

        var httpResponseFeature = new HttpResponseFeature
        {
            StatusCode = 200,
            Headers = new HeaderDictionary(),
            Body = responseBody,
        };

        features.Set<IHttpRequestFeature>(httpRequestFeature);
        features.Set<IHttpResponseFeature>(httpResponseFeature);
        features.Set<IHttpResponseBodyFeature>(new HttpResponseBodyFeature(responseBody));

        return features;
    }

    private static HeaderDictionary CreateHeaders(IDictionary<string, string> headers)
    {
        var result = new HeaderDictionary();

        foreach (var header in headers)
        {
            result[header.Key] = new StringValues(header.Value);
        }

        return result;
    }

    private sealed class HttpResponseBodyFeature : IHttpResponseBodyFeature
    {
        private readonly PipeWriter _writer;

        public HttpResponseBodyFeature(Stream stream)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));
        }

        public Stream Stream { get; }

        public PipeWriter Writer => _writer;

        public void DisableBuffering()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (offset > 0)
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
            }

            if (count is null)
            {
                await fileStream.CopyToAsync(Stream, cancellationToken);
                return;
            }

            var remaining = count.Value;
            var buffer = new byte[81920];

            while (remaining > 0)
            {
                var bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                await Stream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                remaining -= bytesRead;
            }
        }

        public Task CompleteAsync() => _writer.CompleteAsync().AsTask();
    }
}
