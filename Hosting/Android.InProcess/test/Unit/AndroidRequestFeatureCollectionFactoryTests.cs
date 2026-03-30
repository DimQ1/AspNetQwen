#nullable enable

using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess.Tests.Unit;

public class AndroidRequestFeatureCollectionFactoryTests
{
    [Fact]
    public void CreateFeatureCollection_Maps_Request_Method()
    {
        // Arrange
        var request = new AndroidInProcessRequest
        {
            Method = "POST",
            Path = "/api/test",
            QueryString = "?id=123",
        };
        var factory = new AndroidRequestFeatureCollectionFactory(request);

        // Act
        var features = factory.CreateFeatureCollection();
        var httpRequestFeature = features.Get<IHttpRequestFeature>();

        // Assert
        httpRequestFeature.Should().NotBeNull();
        httpRequestFeature!.Method.Should().Be("POST");
    }

    [Fact]
    public void CreateFeatureCollection_Maps_Path_And_QueryString()
    {
        // Arrange
        var request = new AndroidInProcessRequest
        {
            Method = "GET",
            Path = "/api/users",
            QueryString = "?page=1&size=10",
        };
        var factory = new AndroidRequestFeatureCollectionFactory(request);

        // Act
        var features = factory.CreateFeatureCollection();
        var httpRequestFeature = features.Get<IHttpRequestFeature>();

        // Assert
        httpRequestFeature.Should().NotBeNull();
        httpRequestFeature!.Path.Should().Be("/api/users");
        httpRequestFeature.QueryString.Should().Be("?page=1&size=10");
    }

    [Fact]
    public void CreateFeatureCollection_Maps_Headers()
    {
        // Arrange
        var request = new AndroidInProcessRequest
        {
            Method = "GET",
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "application/json",
                ["Authorization"] = "Bearer token123",
            },
        };
        var factory = new AndroidRequestFeatureCollectionFactory(request);

        // Act
        var features = factory.CreateFeatureCollection();
        var httpRequestFeature = features.Get<IHttpRequestFeature>();

        // Assert
        httpRequestFeature.Should().NotBeNull();
        httpRequestFeature!.Headers["Content-Type"].Should().Be("application/json");
        httpRequestFeature.Headers["Authorization"].Should().Be("Bearer token123");
    }

    [Fact]
    public void CreateFeatureCollection_Maps_Body_When_Provided()
    {
        // Arrange
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes("{\"name\":\"test\"}");
        var request = new AndroidInProcessRequest
        {
            Method = "POST",
            Body = bodyBytes,
        };
        var factory = new AndroidRequestFeatureCollectionFactory(request);

        // Act
        var features = factory.CreateFeatureCollection();
        var httpRequestFeature = features.Get<IHttpRequestFeature>();

        // Assert
        httpRequestFeature.Should().NotBeNull();
        httpRequestFeature!.Body.Should().NotBeNull();
        httpRequestFeature.Body.CanRead.Should().BeTrue();
    }

    [Fact]
    public void CreateFeatureCollection_Sets_Null_Body_When_Not_Provided()
    {
        // Arrange
        var request = new AndroidInProcessRequest
        {
            Method = "GET",
            Body = null,
        };
        var factory = new AndroidRequestFeatureCollectionFactory(request);

        // Act
        var features = factory.CreateFeatureCollection();
        var httpRequestFeature = features.Get<IHttpRequestFeature>();

        // Assert
        httpRequestFeature.Should().NotBeNull();
        httpRequestFeature!.Body.Should().NotBeNull();
    }

    [Fact]
    public void CreateFeatureCollection_Includes_Response_Feature()
    {
        // Arrange
        var request = new AndroidInProcessRequest();
        var factory = new AndroidRequestFeatureCollectionFactory(request);

        // Act
        var features = factory.CreateFeatureCollection();
        var httpResponseFeature = features.Get<IHttpResponseFeature>();

        // Assert
        httpResponseFeature.Should().NotBeNull();
        httpResponseFeature!.StatusCode.Should().Be(200);
        httpResponseFeature.Body.Should().NotBeNull();
    }
}
