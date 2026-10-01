using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using StrongCrypt.Protocol;

namespace StrongCrypt.Encryption;

/// <summary>
/// Encrypts plaintext using AES-256-GCM and produces a canonical V1 envelope.
/// Nonce generation is internal using cryptographically secure RNG.
/// </summary>
public sealed class AesGcmEncryptor
{
    /// <summary>
    /// Encrypt plaintext with the specified key and optional metadata.
    /// </summary>
    /// <param name="plaintext">Data to encrypt (zero-copy span).</param>
    /// <param name="key">32-byte AES-256 key.</param>
    /// <param name="keyId">Key identifier (0-64 bytes, non-secret, application-defined).</param>
    /// <param name="associatedData">Optional AAD (authenticated but not encrypted).</param>
    /// <returns>Complete V1 envelope as byte array.</returns>
    /// <exception cref="ArgumentException">Invalid key size or keyId length.</exception>
    /// <exception cref="CryptographicException">Encryption failure.</exception>
    public static byte[] Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> keyId = default,
        ReadOnlySpan<byte> associatedData = default)
    {
        ValidateInputs(key, keyId, plaintext.Length);

        int envelopeSize = GetEnvelopeSize(plaintext.Length, keyId.Length);
        byte[] envelope = new byte[envelopeSize];

        bool success = TryEncryptCore(plaintext, envelope, key, keyId, associatedData, out int bytesWritten);
        if (!success || bytesWritten != envelopeSize)
        {
            CryptographicOperations.ZeroMemory(envelope);
            throw new CryptographicException("Encryption failed");
        }

        return envelope;
    }

    /// <summary>
    /// Encrypt plaintext into a caller-provided destination buffer.
    /// </summary>
    /// <param name="plaintext">Data to encrypt.</param>
    /// <param name="destination">Buffer to receive envelope; must have sufficient capacity.</param>
    /// <param name="key">32-byte AES-256 key.</param>
    /// <param name="keyId">Key identifier (0-64 bytes).</param>
    /// <param name="associatedData">Optional AAD.</param>
    /// <param name="bytesWritten">Number of envelope bytes written.</param>
    /// <returns>True if successful; false if destination too small.</returns>
    /// <exception cref="ArgumentException">Invalid key size or keyId length.</exception>
    /// <exception cref="CryptographicException">Encryption failure.</exception>
    public static bool TryEncrypt(
        ReadOnlySpan<byte> plaintext,
        Span<byte> destination,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> keyId,
        ReadOnlySpan<byte> associatedData,
        out int bytesWritten)
    {
        bytesWritten = 0;
        ValidateInputs(key, keyId, plaintext.Length);

        int requiredSize = GetEnvelopeSize(plaintext.Length, keyId.Length);
        if (destination.Length < requiredSize)
        {
            return false;
        }

        return TryEncryptCore(plaintext, destination, key, keyId, associatedData, out bytesWritten);
    }

    /// <summary>
    /// Calculate required destination buffer size for an envelope.
    /// </summary>
    /// <param name="plaintextLength">Plaintext byte count.</param>
    /// <param name="keyIdLength">Key identifier byte count (0-64).</param>
    /// <returns>Envelope size in bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Invalid lengths.</exception>
    public static int GetEnvelopeSize(int plaintextLength, int keyIdLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(plaintextLength);
        if (keyIdLength < 0 || keyIdLength > V1Constants.MaxKeyIdLength)
            throw new ArgumentOutOfRangeException(nameof(keyIdLength));

        long size = 22L + keyIdLength + plaintextLength + V1Constants.TagSize;
        if (size > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(plaintextLength), "Envelope size exceeds int.MaxValue");

        return (int)size;
    }

    private static void ValidateInputs(ReadOnlySpan<byte> key, ReadOnlySpan<byte> keyId, int plaintextLength)
    {
        if (key.Length != V1Constants.KeySize)
            throw new ArgumentException($"Key must be {V1Constants.KeySize} bytes", nameof(key));

        if (keyId.Length > V1Constants.MaxKeyIdLength)
            throw new ArgumentException($"KeyId must be at most {V1Constants.MaxKeyIdLength} bytes", nameof(keyId));

        if (plaintextLength > V1Constants.MaxCiphertextLength - 38 - keyId.Length)
            throw new ArgumentException("Plaintext too large for envelope format", nameof(plaintextLength));
    }

    private static bool TryEncryptCore(
        ReadOnlySpan<byte> plaintext,
        Span<byte> destination,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> keyId,
        ReadOnlySpan<byte> associatedData,
        out int bytesWritten)
    {
        bytesWritten = 0;

        Span<byte> nonce = stackalloc byte[V1Constants.NonceSize];
        RandomNumberGenerator.Fill(nonce);

        int headerSize = 22 + keyId.Length;
        WriteHeader(destination, keyId, nonce, plaintext.Length);

        Span<byte> ciphertextDest = destination.Slice(headerSize, plaintext.Length);
        Span<byte> tagDest = destination.Slice(headerSize + plaintext.Length, V1Constants.TagSize);

        Span<byte> aad = BuildAAD(destination.Slice(0, headerSize), associatedData);

        try
        {
            using var aesGcm = new AesGcm(key, V1Constants.TagSize);
            aesGcm.Encrypt(nonce, plaintext, ciphertextDest, tagDest, aad);

            bytesWritten = headerSize + plaintext.Length + V1Constants.TagSize;
            return true;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(destination.Slice(0, headerSize + plaintext.Length));
            throw;
        }
        finally
        {
            if (aad.Length > headerSize)
            {
                CryptographicOperations.ZeroMemory(aad.Slice(headerSize));
            }
        }
    }

    private static void WriteHeader(Span<byte> destination, ReadOnlySpan<byte> keyId, ReadOnlySpan<byte> nonce, int plaintextLength)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0, 2), V1Constants.Magic);
        destination[2] = V1Constants.Version;
        destination[3] = V1Constants.ProfileAes256Gcm;
        destination[4] = V1Constants.ReservedFlags;
        destination[5] = (byte)keyId.Length;
        keyId.CopyTo(destination.Slice(6, keyId.Length));
        nonce.CopyTo(destination.Slice(6 + keyId.Length, V1Constants.NonceSize));
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(18 + keyId.Length, 4), plaintextLength);
    }

    private static Span<byte> BuildAAD(Span<byte> protocolHeader, ReadOnlySpan<byte> externalAAD)
    {
        if (externalAAD.IsEmpty)
            return protocolHeader;

        Span<byte> combinedAAD = new byte[protocolHeader.Length + externalAAD.Length];
        protocolHeader.CopyTo(combinedAAD);
        externalAAD.CopyTo(combinedAAD.Slice(protocolHeader.Length));
        return combinedAAD;
    }
}
