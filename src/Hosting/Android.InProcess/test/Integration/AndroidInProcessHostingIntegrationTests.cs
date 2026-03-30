#nullable enable

using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess.Tests.Integration;

public class AndroidInProcessHostingIntegrationTests
{
    [Fact]
    public async Task AddAndroidInProcessHosting_Registers_Server()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.AddAndroidInProcessHosting();
        var app = builder.Build();

        // Assert
        var server = app.Services.GetService<AndroidInProcessServer>();
        server.Should().NotBeNull();
        
        await app.StopAsync();
    }

    [Fact]
    public async Task AddAndroidInProcessHosting_With_Options_Registers_Configured_Server()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        var customBaseAddress = new Uri("https://custom.local");

        // Act
        builder.AddAndroidInProcessHosting(options =>
        {
            options.BaseAddress = customBaseAddress;
            options.EnableWebViewAdapter = false;
            options.EnableHttpClientAdapter = false;
        });
        var app = builder.Build();

        // Assert
        var options = app.Services.GetRequiredService<AndroidInProcessHostingOptions>();
        options.BaseAddress.Should().Be(customBaseAddress);
        options.EnableWebViewAdapter.Should().BeFalse();
        options.EnableHttpClientAdapter.Should().BeFalse();
        
        await app.StopAsync();
    }

    [Fact]
    public async Task Server_Can_Dispatch_Request_To_Middleware()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        var options = new AndroidInProcessHostingOptions();
        builder.Services.AddSingleton(options);
        builder.AddAndroidInProcessHosting();
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseAndroidInProcessHosting();
            webHostBuilder.Configure(app =>
            {
                app.Run(async context =>
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Hello from ASP.NET Core!");
                });
            });
        });

        var app = builder.Build();
        await app.StartAsync();

        var server = app.Services.GetRequiredService<AndroidInProcessServer>();
        var request = new AndroidInProcessRequest
        {
            Method = "GET",
            Path = "/",
        };

        // Act
        var response = await server.DispatchRequestAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Headers["Content-Type"].Should().Be("text/plain");
        var bodyText = System.Text.Encoding.UTF8.GetString(response.Body);
        bodyText.Should().Be("Hello from ASP.NET Core!");
        
        await app.StopAsync();
    }

    [Fact]
    public async Task Server_Handles_Exception_As_500()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        var options = new AndroidInProcessHostingOptions();
        builder.Services.AddSingleton(options);
        builder.AddAndroidInProcessHosting();
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseAndroidInProcessHosting();
            webHostBuilder.Configure(app =>
            {
                app.Run(context => throw new InvalidOperationException("Test exception"));
            });
        });

        var app = builder.Build();
        await app.StartAsync();

        var server = app.Services.GetRequiredService<AndroidInProcessServer>();
        var request = new AndroidInProcessRequest
        {
            Method = "GET",
            Path = "/error",
        };

        // Act
        var response = await server.DispatchRequestAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(500);
        
        await app.StopAsync();
    }

    [Fact]
    public async Task Server_Rejects_Oversized_Request_Body()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        var options = new AndroidInProcessHostingOptions
        {
            MaxRequestBodySize = 100 // 100 bytes limit for testing
        };
        builder.Services.AddSingleton(options);
        builder.AddAndroidInProcessHosting();
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseAndroidInProcessHosting();
            webHostBuilder.Configure(app =>
            {
                app.Run(async context =>
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("OK");
                });
            });
        });

        var app = builder.Build();
        await app.StartAsync();

        var server = app.Services.GetRequiredService<AndroidInProcessServer>();
        var largeBody = new byte[200]; // 200 bytes - exceeds limit
        var request = new AndroidInProcessRequest
        {
            Method = "POST",
            Path = "/api/large",
            Body = largeBody,
        };

        // Act
        var response = await server.DispatchRequestAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(413); // Payload Too Large
        
        await app.StopAsync();
    }

    [Fact]
    public async Task Server_Can_Process_Post_With_Body()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        var options = new AndroidInProcessHostingOptions();
        builder.Services.AddSingleton(options);
        builder.AddAndroidInProcessHosting();
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseAndroidInProcessHosting();
            webHostBuilder.Configure(app =>
            {
                app.Run(async context =>
                {
                    using var reader = new StreamReader(context.Request.Body);
                    var body = await reader.ReadToEndAsync();
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($"{{\"received\":\"{body}\"}}");
                });
            });
        });

        var app = builder.Build();
        await app.StartAsync();

        var server = app.Services.GetRequiredService<AndroidInProcessServer>();
        var requestBody = "{\"name\":\"test\"}";
        var request = new AndroidInProcessRequest
        {
            Method = "POST",
            Path = "/api/echo",
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
            },
            Body = System.Text.Encoding.UTF8.GetBytes(requestBody),
        };

        // Act
        var response = await server.DispatchRequestAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(200);
        var bodyText = System.Text.Encoding.UTF8.GetString(response.Body);
        bodyText.Should().Contain("{\"received\":\"{\\\"name\\\":\\\"test\\\"}\"}");
        
        await app.StopAsync();
    }
}
