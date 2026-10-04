using System;

namespace StrongCrypt.Encryption;

/// <summary>
/// Injectable encryption service wrapper for AesGcmEncryptor.
/// </summary>
public sealed class EncryptionService
{
    private readonly IEncryptionKeyProvider _keyProvider;

    /// <summary>
    /// Initializes a new encryption service with the specified key provider.
    /// </summary>
    public EncryptionService(IEncryptionKeyProvider keyProvider)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    /// <summary>
    /// Encrypts plaintext using a key retrieved from the configured provider.
    /// </summary>
    /// <param name="plaintext">Data to encrypt.</param>
    /// <param name="keyId">Key identifier (0-64 bytes, used to retrieve key).</param>
    /// <param name="associatedData">Optional AAD.</param>
    /// <returns>Complete V1 envelope.</returns>
    public byte[] Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> keyId = default,
        ReadOnlySpan<byte> associatedData = default)
    {
        byte[] key = _keyProvider.GetKey(keyId);
        try
        {
            return AesGcmEncryptor.Encrypt(plaintext, key, keyId, associatedData);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>
    /// Encrypts plaintext into a destination buffer using a key from the provider.
    /// </summary>
    public bool TryEncrypt(
        ReadOnlySpan<byte> plaintext,
        Span<byte> destination,
        ReadOnlySpan<byte> keyId,
        ReadOnlySpan<byte> associatedData,
        out int bytesWritten)
    {
        byte[] key = _keyProvider.GetKey(keyId);
        try
        {
            return AesGcmEncryptor.TryEncrypt(plaintext, destination, key, keyId, associatedData, out bytesWritten);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
        }
    }
}
