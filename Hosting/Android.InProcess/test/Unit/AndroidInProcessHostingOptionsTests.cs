#nullable enable

using Xunit;
using FluentAssertions;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess.Tests.Unit;

public class AndroidInProcessHostingOptionsTests
{
    [Fact]
    public void Default_BaseAddress_Is_AppLocal()
    {
        // Arrange & Act
        var options = new AndroidInProcessHostingOptions();

        // Assert
        options.BaseAddress.Should().Be("https://app.local/");
    }

    [Fact]
    public void Default_EnableWebViewAdapter_Is_True()
    {
        // Arrange & Act
        var options = new AndroidInProcessHostingOptions();

        // Assert
        options.EnableWebViewAdapter.Should().BeTrue();
    }

    [Fact]
    public void Default_EnableHttpClientAdapter_Is_True()
    {
        // Arrange & Act
        var options = new AndroidInProcessHostingOptions();

        // Assert
        options.EnableHttpClientAdapter.Should().BeTrue();
    }

    [Fact]
    public void Default_ShutdownTimeout_Is_5_Seconds()
    {
        // Arrange & Act
        var options = new AndroidInProcessHostingOptions();

        // Assert
        options.ShutdownTimeout.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Default_BufferRequestBody_Is_True()
    {
        // Arrange & Act
        var options = new AndroidInProcessHostingOptions();

        // Assert
        options.BufferRequestBody.Should().BeTrue();
    }

    [Fact]
    public void Default_BufferResponseBody_Is_True()
    {
        // Arrange & Act
        var options = new AndroidInProcessHostingOptions();

        // Assert
        options.BufferResponseBody.Should().BeTrue();
    }

    [Fact]
    public void Default_MaxRequestBodySize_Is_10MB()
    {
        // Arrange & Act
        var options = new AndroidInProcessHostingOptions();

        // Assert
        options.MaxRequestBodySize.Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public void Default_MaxResponseBodySize_Is_10MB()
    {
        // Arrange & Act
        var options = new AndroidInProcessHostingOptions();

        // Assert
        options.MaxResponseBodySize.Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public void Can_Customize_Options()
    {
        // Arrange
        var customBaseAddress = new Uri("https://custom.local");
        var customShutdownTimeout = TimeSpan.FromSeconds(10);

        // Act
        var options = new AndroidInProcessHostingOptions
        {
            BaseAddress = customBaseAddress,
            EnableWebViewAdapter = false,
            EnableHttpClientAdapter = false,
            ShutdownTimeout = customShutdownTimeout,
            BufferRequestBody = false,
            BufferResponseBody = false,
            MaxRequestBodySize = 5 * 1024 * 1024,
            MaxResponseBodySize = 5 * 1024 * 1024,
        };

        // Assert
        options.BaseAddress.Should().Be(customBaseAddress);
        options.EnableWebViewAdapter.Should().BeFalse();
        options.EnableHttpClientAdapter.Should().BeFalse();
        options.ShutdownTimeout.Should().Be(customShutdownTimeout);
        options.BufferRequestBody.Should().BeFalse();
        options.BufferResponseBody.Should().BeFalse();
        options.MaxRequestBodySize.Should().Be(5 * 1024 * 1024);
        options.MaxResponseBodySize.Should().Be(5 * 1024 * 1024);
    }
}
