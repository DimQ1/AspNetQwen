#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Android.InProcess;

/// <summary>
/// Extension methods for configuring Android in-process hosting with IWebHostBuilder.
/// </summary>
public static class AndroidInProcessWebHostBuilderExtensions
{
    /// <summary>
    /// Configures the web host to use Android in-process hosting.
    /// </summary>
    /// <param name="builder">The web host builder.</param>
    /// <returns>The web host builder.</returns>
    public static IWebHostBuilder UseAndroidInProcessHosting(this IWebHostBuilder builder)
    {
        return UseAndroidInProcessHosting(builder, _ => { });
    }

    /// <summary>
    /// Configures the web host to use Android in-process hosting with configuration options.
    /// </summary>
    /// <param name="builder">The web host builder.</param>
    /// <param name="configure">Action to configure Android in-process hosting options.</param>
    /// <returns>The web host builder.</returns>
    public static IWebHostBuilder UseAndroidInProcessHosting(
        this IWebHostBuilder builder,
        Action<AndroidInProcessHostingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AndroidInProcessHostingOptions();
        configure(options);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<AndroidInProcessServer>();
            services.AddHostedService(sp =>
            {
                var server = sp.GetRequiredService<AndroidInProcessServer>();
                var options = sp.GetRequiredService<AndroidInProcessHostingOptions>();
                var logger = sp.GetService<ILogger<AndroidHostingLifetimeService>>();
                return new AndroidHostingLifetimeService(server, options, logger);
            });

            if (options.EnableWebViewAdapter)
            {
                services.AddTransient(sp =>
                {
                    var server = sp.GetRequiredService<AndroidInProcessServer>();
                    var logger = sp.GetService<ILogger<AndroidWebViewRequestAdapter>>();
                    return new AndroidWebViewRequestAdapter(server, options.BaseAddress, logger);
                });
            }

            if (options.EnableHttpClientAdapter)
            {
                services.AddTransient(sp =>
                {
                    var server = sp.GetRequiredService<AndroidInProcessServer>();
                    var logger = sp.GetService<ILogger<AndroidHttpClientMessageHandler>>();
                    return new AndroidHttpClientMessageHandler(server, options.BaseAddress, innerHandler: null, logger);
                });
            }
        });

        return builder;
    }
}
