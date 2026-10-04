using System;

namespace StrongCrypt.Decryption;

/// <summary>
/// Injectable decryption service wrapper for AesGcmDecryptor.
/// </summary>
public sealed class DecryptionService
{
    private readonly IDecryptionKeyProvider _keyProvider;

    /// <summary>
    /// Initializes a new decryption service with the specified key provider.
    /// </summary>
    public DecryptionService(IDecryptionKeyProvider keyProvider)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    /// <summary>
    /// Decrypts a V1 envelope using a key retrieved from the configured provider.
    /// </summary>
    /// <param name="envelope">V1 envelope bytes.</param>
    /// <param name="keyId">Optional key identifier to retrieve specific key.</param>
    /// <param name="externalAAD">Optional external AAD (must match encryption).</param>
    /// <returns>Decrypted plaintext.</returns>
    public byte[] Decrypt(
        ReadOnlySpan<byte> envelope,
        ReadOnlySpan<byte> keyId = default,
        ReadOnlySpan<byte> externalAAD = default)
    {
        byte[] key = _keyProvider.GetKey(keyId);
        try
        {
            return AesGcmDecryptor.Decrypt(key, envelope, externalAAD);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>
    /// Attempts to decrypt a V1 envelope into a destination buffer.
    /// </summary>
    public bool TryDecrypt(
        ReadOnlySpan<byte> envelope,
        Span<byte> destination,
        out int bytesWritten,
        ReadOnlySpan<byte> keyId = default,
        ReadOnlySpan<byte> externalAAD = default)
    {
        byte[] key = _keyProvider.GetKey(keyId);
        try
        {
            return AesGcmDecryptor.TryDecrypt(key, envelope, destination, out bytesWritten, externalAAD);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
        }
    }
}
