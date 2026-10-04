using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrongCrypt.Decryption;

/// <summary>
/// Dependency injection extensions for StrongCrypt decryption services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers StrongCrypt decryption services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    /// <remarks>
    /// Registers DecryptionService as singleton. Caller must separately register
    /// IDecryptionKeyProvider with appropriate lifetime and security controls.
    /// </remarks>
    public static IServiceCollection AddStrongCryptDecryption(this IServiceCollection services)
    {
        services.TryAddSingleton<DecryptionService>();
        return services;
    }

    /// <summary>
    /// Registers StrongCrypt decryption services with a custom key provider.
    /// </summary>
    /// <typeparam name="TKeyProvider">Key provider implementation type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddStrongCryptDecryption<TKeyProvider>(this IServiceCollection services)
        where TKeyProvider : class, IDecryptionKeyProvider
    {
        services.TryAddSingleton<IDecryptionKeyProvider, TKeyProvider>();
        services.TryAddSingleton<DecryptionService>();
        return services;
    }
}
