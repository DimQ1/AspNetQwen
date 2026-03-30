#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// Extension methods for configuring Android in-process hosting with IHostApplicationBuilder.
/// </summary>
public static class AndroidInProcessHostBuilderExtensions
{
    /// <summary>
    /// Adds Android in-process hosting services to the application.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The host application builder.</returns>
    public static IHostApplicationBuilder AddAndroidInProcessHosting(this IHostApplicationBuilder builder)
    {
        return AddAndroidInProcessHosting(builder, _ => { });
    }

    /// <summary>
    /// Adds Android in-process hosting services to the application with configuration options.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Action to configure Android in-process hosting options.</param>
    /// <returns>The host application builder.</returns>
    public static IHostApplicationBuilder AddAndroidInProcessHosting(
        this IHostApplicationBuilder builder,
        Action<AndroidInProcessHostingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AndroidInProcessHostingOptions();
        configure(options);

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<AndroidInProcessServer>();
        builder.Services.AddHostedService(sp =>
        {
            var server = sp.GetRequiredService<AndroidInProcessServer>();
            var options = sp.GetRequiredService<AndroidInProcessHostingOptions>();
            var logger = sp.GetService<ILogger<AndroidHostingLifetimeService>>();
            return new AndroidHostingLifetimeService(server, options, logger);
        });

        if (options.EnableWebViewAdapter)
        {
            builder.Services.AddTransient(sp =>
            {
                var server = sp.GetRequiredService<AndroidInProcessServer>();
                var logger = sp.GetService<ILogger<AndroidWebViewRequestAdapter>>();
                return new AndroidWebViewRequestAdapter(server, options.BaseAddress, logger);
            });
        }

        if (options.EnableHttpClientAdapter)
        {
            builder.Services.AddTransient(sp =>
            {
                var server = sp.GetRequiredService<AndroidInProcessServer>();
                var logger = sp.GetService<ILogger<AndroidHttpClientMessageHandler>>();
                return new AndroidHttpClientMessageHandler(server, options.BaseAddress, innerHandler: null, logger);
            });
        }

        return builder;
    }
}
