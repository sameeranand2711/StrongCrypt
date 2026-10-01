namespace StrongCrypt.Protocol;

/// <summary>
/// V1 protocol wire-format constants and limits.
/// These values are frozen for V1 compatibility and must not change.
/// </summary>
public static class V1Constants
{
    /// <summary>Magic bytes "SC" (0x5343) identifying a StrongCrypt envelope.</summary>
    public const ushort Magic = 0x5343;

    /// <summary>Protocol version 1.</summary>
    public const byte Version = 0x01;

    /// <summary>AES-256-GCM cryptographic profile identifier.</summary>
    public const byte ProfileAes256Gcm = 0x01;

    /// <summary>Reserved flags byte (must be 0x00 in V1).</summary>
    public const byte ReservedFlags = 0x00;

    /// <summary>AES-256 key size in bytes.</summary>
    public const int KeySize = 32;

    /// <summary>GCM nonce size in bytes (96 bits).</summary>
    public const int NonceSize = 12;

    /// <summary>GCM authentication tag size in bytes (128 bits).</summary>
    public const int TagSize = 16;

    /// <summary>Maximum key identifier length in bytes.</summary>
    public const int MaxKeyIdLength = 64;

    /// <summary>Maximum ciphertext length in bytes (2^31 - 1 for int.MaxValue allocation safety).</summary>
    public const int MaxCiphertextLength = int.MaxValue;

    /// <summary>Minimum valid envelope size in bytes (empty plaintext, no KeyId).</summary>
    public const int MinEnvelopeSize = 38;
}
