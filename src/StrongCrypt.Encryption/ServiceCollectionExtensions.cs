using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrongCrypt.Encryption;

/// <summary>
/// Dependency injection extensions for StrongCrypt encryption services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers StrongCrypt encryption services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    /// <remarks>
    /// Registers EncryptionService as singleton. Caller must separately register
    /// IEncryptionKeyProvider with appropriate lifetime and security controls.
    /// </remarks>
    public static IServiceCollection AddStrongCryptEncryption(this IServiceCollection services)
    {
        services.TryAddSingleton<EncryptionService>();
        return services;
    }

    /// <summary>
    /// Registers StrongCrypt encryption services with a custom key provider.
    /// </summary>
    /// <typeparam name="TKeyProvider">Key provider implementation type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddStrongCryptEncryption<TKeyProvider>(this IServiceCollection services)
        where TKeyProvider : class, IEncryptionKeyProvider
    {
        services.TryAddSingleton<IEncryptionKeyProvider, TKeyProvider>();
        services.TryAddSingleton<EncryptionService>();
        return services;
    }
}
