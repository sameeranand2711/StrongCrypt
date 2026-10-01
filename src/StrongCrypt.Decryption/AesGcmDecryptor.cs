using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using StrongCrypt.Protocol;

namespace StrongCrypt.Decryption;

/// <summary>
/// StrongCrypt V1 decryptor using AES-256-GCM.
/// Parses and authenticates V1 envelopes, then decrypts payload.
/// </summary>
public static class AesGcmDecryptor
{
    /// <summary>
    /// Decrypts a V1 envelope and returns plaintext as a new byte array.
    /// </summary>
    /// <param name="key">32-byte AES-256 key.</param>
    /// <param name="envelope">Complete V1 envelope bytes.</param>
    /// <param name="externalAAD">Optional external AAD (concatenated after protocol header in GCM AAD).</param>
    /// <returns>Plaintext bytes.</returns>
    /// <exception cref="ArgumentException">Invalid key size or envelope format.</exception>
    /// <exception cref="CryptographicException">Authentication or decryption failure.</exception>
    public static byte[] Decrypt(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> envelope,
        ReadOnlySpan<byte> externalAAD = default)
    {
        if (key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes", nameof(key));

        var parsed = ParseEnvelope(envelope);
        
        byte[] plaintext = new byte[parsed.Ciphertext.Length];
        
        using (var aes = new AesGcm(key, V1Constants.TagSize))
        {
            Span<byte> combinedAAD = BuildAAD(parsed.ProtocolHeader, externalAAD);
            
            try
            {
                aes.Decrypt(parsed.Nonce, parsed.Ciphertext, parsed.Tag, plaintext, combinedAAD);
            }
            catch (CryptographicException ex)
            {
                CryptographicOperations.ZeroMemory(plaintext);
                throw new CryptographicException("Decryption failed", ex);
            }
        }
        
        return plaintext;
    }

    /// <summary>
    /// Attempts to decrypt a V1 envelope into caller-provided destination buffer.
    /// </summary>
    /// <param name="key">32-byte AES-256 key.</param>
    /// <param name="envelope">Complete V1 envelope bytes.</param>
    /// <param name="destination">Destination buffer (must be >= plaintext length).</param>
    /// <param name="bytesWritten">Actual plaintext bytes written.</param>
    /// <param name="externalAAD">Optional external AAD.</param>
    /// <returns>True if successful; false if destination too small or decryption fails.</returns>
    public static bool TryDecrypt(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> envelope,
        Span<byte> destination,
        out int bytesWritten,
        ReadOnlySpan<byte> externalAAD = default)
    {
        bytesWritten = 0;
        
        if (key.Length != 32)
            return false;

        if (!TryParseEnvelope(envelope, out var parsed))
            return false;

        if (destination.Length < parsed.Ciphertext.Length)
            return false;

        Span<byte> plaintext = destination.Slice(0, parsed.Ciphertext.Length);
        
        using (var aes = new AesGcm(key, V1Constants.TagSize))
        {
            Span<byte> combinedAAD = BuildAAD(parsed.ProtocolHeader, externalAAD);
            
            try
            {
                aes.Decrypt(parsed.Nonce, parsed.Ciphertext, parsed.Tag, plaintext, combinedAAD);
                bytesWritten = parsed.Ciphertext.Length;
                return true;
            }
            catch (CryptographicException)
            {
                CryptographicOperations.ZeroMemory(plaintext);
                return false;
            }
        }
    }

    /// <summary>
    /// Calculates the plaintext length from a V1 envelope without decrypting.
    /// </summary>
    /// <param name="envelope">Complete V1 envelope bytes.</param>
    /// <returns>Plaintext length in bytes.</returns>
    /// <exception cref="ArgumentException">Invalid envelope format.</exception>
    public static int GetPlaintextLength(ReadOnlySpan<byte> envelope)
    {
        var parsed = ParseEnvelope(envelope);
        return parsed.Ciphertext.Length;
    }

    private readonly ref struct ParsedEnvelope
    {
        public ReadOnlySpan<byte> ProtocolHeader { get; }
        public ReadOnlySpan<byte> Nonce { get; }
        public ReadOnlySpan<byte> Tag { get; }
        public ReadOnlySpan<byte> Ciphertext { get; }

        public ParsedEnvelope(ReadOnlySpan<byte> protocolHeader, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> ciphertext)
        {
            ProtocolHeader = protocolHeader;
            Nonce = nonce;
            Tag = tag;
            Ciphertext = ciphertext;
        }
    }

    private static ParsedEnvelope ParseEnvelope(ReadOnlySpan<byte> envelope)
    {
        if (!TryParseEnvelope(envelope, out var parsed))
            throw new ArgumentException("Invalid V1 envelope format", nameof(envelope));
        return parsed;
    }

    private static bool TryParseEnvelope(ReadOnlySpan<byte> envelope, out ParsedEnvelope parsed)
    {
        parsed = default;

        // Minimum: magic(2) + version(1) + profile(1) + flags(1) + keyIdLen(1) + nonce(12) + ciphertextLen(4) + tag(16) + ciphertext(0+)
        if (envelope.Length < V1Constants.MinEnvelopeSize)
            return false;

        // Check magic (little-endian)
        if (BinaryPrimitives.ReadUInt16LittleEndian(envelope) != V1Constants.Magic)
            return false;

        // Check version
        if (envelope[2] != V1Constants.Version)
            return false;

        // Check profile
        if (envelope[3] != V1Constants.ProfileAes256Gcm)
            return false;

        // Check flags
        if (envelope[4] != V1Constants.ReservedFlags)
            return false;

        int keyIdLen = envelope[5];
        if (keyIdLen > V1Constants.MaxKeyIdLength)
            return false;

        int ciphertextLenOffset = 18 + keyIdLen;
        if (envelope.Length < ciphertextLenOffset + 4)
            return false;

        int ciphertextLen = BinaryPrimitives.ReadInt32LittleEndian(envelope.Slice(ciphertextLenOffset, 4));
        if (ciphertextLen < 0 || ciphertextLen > V1Constants.MaxCiphertextLength)
            return false;

        int expectedLength = 22 + keyIdLen + ciphertextLen + V1Constants.TagSize;
        if (envelope.Length != expectedLength)
            return false;

        int headerEnd = 6 + keyIdLen;
        int nonceOffset = headerEnd;
        int ciphertextOffset = 22 + keyIdLen;
        int tagOffset = ciphertextOffset + ciphertextLen;

        ReadOnlySpan<byte> protocolHeader = envelope.Slice(0, headerEnd);
        ReadOnlySpan<byte> nonce = envelope.Slice(nonceOffset, V1Constants.NonceSize);
        ReadOnlySpan<byte> tag = envelope.Slice(tagOffset, V1Constants.TagSize);
        ReadOnlySpan<byte> ciphertext = envelope.Slice(ciphertextOffset, ciphertextLen);

        parsed = new ParsedEnvelope(protocolHeader, nonce, tag, ciphertext);
        return true;
    }

    private static Span<byte> BuildAAD(ReadOnlySpan<byte> protocolHeader, ReadOnlySpan<byte> externalAAD)
    {
        if (externalAAD.IsEmpty)
            return protocolHeader.ToArray();

        Span<byte> combinedAAD = new byte[protocolHeader.Length + externalAAD.Length];
        protocolHeader.CopyTo(combinedAAD);
        externalAAD.CopyTo(combinedAAD.Slice(protocolHeader.Length));
        return combinedAAD;
    }
}
