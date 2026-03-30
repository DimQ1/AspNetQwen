#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

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

        var httpRequestFeature = new HttpRequestFeature
        {
            Method = _request.Method,
            Scheme = "https",
            PathBase = string.Empty,
            Path = _request.Path,
            QueryString = _request.QueryString,
            Headers = new HeaderDictionary(_request.Headers),
        };

        if (_request.Body != null && _request.Body.Length > 0)
        {
            httpRequestFeature.Body = new MemoryStream(_request.Body);
        }
        else
        {
            httpRequestFeature.Body = Stream.Null;
        }

        features.Set<IHttpRequestFeature>(httpRequestFeature);

        var httpResponseFeature = new HttpResponseFeature
        {
            StatusCode = 200,
            Headers = new HeaderDictionary(),
            Body = new MemoryStream(),
        };

        features.Set<IHttpResponseFeature>(httpResponseFeature);

        var responseBodyFeature = new HttpResponseBodyFeature
        {
            Body = httpResponseFeature.Body,
        };

        features.Set<IHttpResponseBodyFeature>(responseBodyFeature);

        return features;
    }

    private sealed class HttpRequestFeature : IHttpRequestFeature
    {
        public string Method { get; set; } = "GET";
        public string Scheme { get; set; } = "https";
        public bool IsHttps { get; set; }
        public IDictionary<string, StringValues> Query { get; set; } = new Dictionary<string, StringValues>();
        public string PathBase { get; set; } = string.Empty;
        public string Path { get; set; } = "/";
        public string QueryString { get; set; } = string.Empty;
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
    }

    private sealed class HttpResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
    }

    private sealed class HttpResponseBodyFeature : IHttpResponseBodyFeature
    {
        public Stream Body { get; set; } = Stream.Null;

        public Task CompleteAsync() => Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void DisableBuffering() { }
    }
}
