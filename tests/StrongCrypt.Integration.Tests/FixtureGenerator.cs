using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using StrongCrypt.Protocol;

namespace StrongCrypt.Integration.Tests;

internal static class FixtureGenerator
{
    public static byte[] GenerateEnvelopeWithFixedNonce(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> keyId = default,
        ReadOnlySpan<byte> externalAAD = default)
    {
        if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", nameof(key));
        if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", nameof(nonce));
        if (keyId.Length > 64) throw new ArgumentException("KeyId cannot exceed 64 bytes", nameof(keyId));

        int keyIdLen = keyId.Length;
        int ciphertextLen = plaintext.Length;
        int protocolHeaderLen = 22 + keyIdLen;
        int envelopeLen = protocolHeaderLen + ciphertextLen + 16;

        byte[] envelope = new byte[envelopeLen];
        Span<byte> span = envelope.AsSpan();

        // V1 wire format header
        BinaryPrimitives.WriteUInt16LittleEndian(span, V1Constants.Magic);
        span[2] = V1Constants.Version;
        span[3] = V1Constants.ProfileAes256Gcm;
        span[4] = V1Constants.ReservedFlags;
        span[5] = (byte)keyIdLen;
        
        // KeyId comes before nonce
        if (keyIdLen > 0)
        {
            keyId.CopyTo(span.Slice(6, keyIdLen));
        }
        
        // Nonce follows keyId
        nonce.CopyTo(span.Slice(6 + keyIdLen, 12));
        
        // Ciphertext length
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(18 + keyIdLen, 4), ciphertextLen);

        Span<byte> ciphertext = span.Slice(protocolHeaderLen, ciphertextLen);
        Span<byte> tag = span.Slice(protocolHeaderLen + ciphertextLen, 16);

        // AAD = protocol header + external AAD
        Span<byte> aad = stackalloc byte[protocolHeaderLen + externalAAD.Length];
        span.Slice(0, protocolHeaderLen).CopyTo(aad);
        if (!externalAAD.IsEmpty)
        {
            externalAAD.CopyTo(aad.Slice(protocolHeaderLen));
        }

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);

        return envelope;
    }
}
