using System;

namespace StrongCrypt.Encryption;

/// <summary>
/// Provides encryption keys for StrongCrypt operations.
/// Implementations must securely manage key lifecycle and disposal.
/// </summary>
public interface IEncryptionKeyProvider
{
    /// <summary>
    /// Retrieves the encryption key for the specified key identifier.
    /// </summary>
    /// <param name="keyId">Optional key identifier. If empty, returns the default key.</param>
    /// <returns>32-byte AES-256 key. Caller must zero memory after use.</returns>
    byte[] GetKey(ReadOnlySpan<byte> keyId = default);
}
