using System;
using Messager.EskizUz.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Messager.EskizUz;

/// <summary>DI helpers for registering <see cref="IMessagerAgent"/>.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMessagerAgent"/> as a singleton, binds
    /// <see cref="EskizOptions"/> from the supplied <paramref name="configuration"/>
    /// section (default name: "Eskiz") and wires a named <see cref="System.Net.Http.HttpClient"/>
    /// through <see cref="IHttpClientFactory"/>.
    /// </summary>
    public static IServiceCollection AddEskizMessenger(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Eskiz")
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.AddOptions<EskizOptions>()
                .Bind(configuration.GetSection(sectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.Email),
                          "EskizOptions.Email is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.SecretKey),
                          "EskizOptions.SecretKey is required.");

        services.AddHttpClient(MessagerAgent.HttpClientName);

        services.AddSingleton<IMessagerAgent>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EskizOptions>>().Value;
            var factory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
            return new MessagerAgent(opts, factory);
        });

        return services;
    }

    /// <summary>
    /// Registers <see cref="IMessagerAgent"/> with an inline options configurator.
    /// </summary>
    public static IServiceCollection AddEskizMessenger(
        this IServiceCollection services,
        Action<EskizOptions> configure)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        services.AddOptions<EskizOptions>()
                .Configure(configure)
                .Validate(o => !string.IsNullOrWhiteSpace(o.Email),
                          "EskizOptions.Email is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.SecretKey),
                          "EskizOptions.SecretKey is required.");

        services.AddHttpClient(MessagerAgent.HttpClientName);

        services.AddSingleton<IMessagerAgent>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EskizOptions>>().Value;
            var factory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
            return new MessagerAgent(opts, factory);
        });

        return services;
    }
}
