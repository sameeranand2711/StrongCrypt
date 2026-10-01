using System;
using System.Security.Cryptography;
using StrongCrypt.Decryption;
using StrongCrypt.Protocol;
using Xunit;

namespace StrongCrypt.Decryption.Tests;

public class AdversarialTests
{
    private static byte[] CreateValidEnvelope(byte[] key, byte[] plaintext, byte[]? keyId = null, byte[]? externalAAD = null)
    {
        Span<byte> nonce = stackalloc byte[12];
        RandomNumberGenerator.Fill(nonce);

        int keyIdLen = keyId?.Length ?? 0;
        int protocolHeaderLen = 22 + keyIdLen;
        int envelopeLen = protocolHeaderLen + plaintext.Length + 16;

        byte[] envelope = new byte[envelopeLen];
        Span<byte> span = envelope.AsSpan();

        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(span, V1Constants.Magic);
        span[2] = V1Constants.Version;
        span[3] = V1Constants.ProfileAes256Gcm;
        span[4] = V1Constants.ReservedFlags;
        span[5] = (byte)keyIdLen;
        if (keyIdLen > 0) keyId.CopyTo(span.Slice(6, keyIdLen));
        nonce.CopyTo(span.Slice(6 + keyIdLen, 12));
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span.Slice(18 + keyIdLen, 4), plaintext.Length);

        Span<byte> ciphertext = span.Slice(protocolHeaderLen, plaintext.Length);
        Span<byte> tag = span.Slice(protocolHeaderLen + plaintext.Length, 16);

        int aadLen = protocolHeaderLen + (externalAAD?.Length ?? 0);
        Span<byte> aad = stackalloc byte[aadLen];
        span.Slice(0, protocolHeaderLen).CopyTo(aad);
        if (externalAAD != null) externalAAD.CopyTo(aad.Slice(protocolHeaderLen));

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);

        return envelope;
    }

    [Fact]
    public void Truncation_EmptyInput_Rejects()
    {
        byte[] key = new byte[32];
        Assert.Throws<ArgumentException>(() => AesGcmDecryptor.Decrypt(key, []));
    }

    [Fact]
    public void Truncation_OnlyMagic_Rejects()
    {
        byte[] key = new byte[32];
        byte[] envelope = [0x53, 0x43];
        Assert.Throws<ArgumentException>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Fact]
    public void Truncation_HeaderWithoutCiphertext_Rejects()
    {
        byte[] key = new byte[32];
        byte[] envelope = new byte[22];
        envelope[0] = 0x53;
        envelope[1] = 0x43;
        envelope[2] = V1Constants.Version;
        envelope[3] = V1Constants.ProfileAes256Gcm;
        envelope[4] = V1Constants.ReservedFlags;
        envelope[5] = 0;
        Assert.Throws<ArgumentException>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Fact]
    public void Truncation_MissingTag_Rejects()
    {
        byte[] key = new byte[32];
        byte[] plaintext = "test"u8.ToArray();
        byte[] validEnvelope = CreateValidEnvelope(key, plaintext);
        byte[] truncated = validEnvelope[..(validEnvelope.Length - 1)];
        Assert.Throws<ArgumentException>(() => AesGcmDecryptor.Decrypt(key, truncated));
    }

    [Fact]
    public void Mutation_Magic_Rejects()
    {
        byte[] key = new byte[32];
        byte[] plaintext = "test"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext);
        envelope[0] ^= 0xFF;
        Assert.Throws<ArgumentException>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Fact]
    public void Mutation_Version_Rejects()
    {
        byte[] key = new byte[32];
        byte[] plaintext = "test"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext);
        envelope[2] = 0x02;
        Assert.Throws<ArgumentException>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Fact]
    public void Mutation_Profile_Rejects()
    {
        byte[] key = new byte[32];
        byte[] plaintext = "test"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext);
        envelope[3] = 0xFF;
        Assert.Throws<ArgumentException>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Fact]
    public void Mutation_ReservedFlags_Rejects()
    {
        byte[] key = new byte[32];
        byte[] plaintext = "test"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext);
        envelope[4] = 0x01;
        Assert.Throws<ArgumentException>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Fact]
    public void Mutation_Nonce_FailsAuthentication()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        byte[] plaintext = "test"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext);
        envelope[6] ^= 0x01;
        Assert.Throws<CryptographicException>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Fact]
    public void Mutation_Ciphertext_FailsAuthentication()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        byte[] plaintext = "test"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext);
        envelope[22] ^= 0x01;
        Assert.Throws<CryptographicException>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Fact]
    public void Mutation_Tag_FailsAuthentication()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        byte[] plaintext = "test"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext);
        envelope[^1] ^= 0x01;
        Assert.Throws<CryptographicException>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Fact]
    public void Mutation_KeyIdLength_CausesStructuralRejectionOrAuthFailure()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        byte[] plaintext = "test"u8.ToArray();
        byte[] keyId = "key1"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext, keyId);
        envelope[5] = 99;
        Assert.ThrowsAny<Exception>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Fact]
    public void Mutation_CiphertextLength_CausesStructuralRejectionOrAuthFailure()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        byte[] plaintext = "test"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext);
        envelope[18] = 0xFF;
        Assert.ThrowsAny<Exception>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void SystematicTruncation_AllLengths_Reject(int validLength)
    {
        if (validLength < 22) return;

        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        byte[] plaintext = new byte[validLength - 22 - 16];
        RandomNumberGenerator.Fill(plaintext);
        byte[] envelope = CreateValidEnvelope(key, plaintext);

        for (int truncLen = 0; truncLen < envelope.Length; truncLen++)
        {
            byte[] truncated = envelope[..truncLen];
            Assert.ThrowsAny<Exception>(() => AesGcmDecryptor.Decrypt(key, truncated));
        }
    }

    [Fact]
    public void TrailingData_Rejects()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        byte[] plaintext = "test"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext);
        byte[] withTrailing = new byte[envelope.Length + 10];
        envelope.CopyTo(withTrailing, 0);
        Assert.Throws<ArgumentException>(() => AesGcmDecryptor.Decrypt(key, withTrailing));
    }

    [Fact]
    public void WrongKey_FailsAuthentication()
    {
        byte[] key1 = new byte[32];
        byte[] key2 = new byte[32];
        RandomNumberGenerator.Fill(key1);
        RandomNumberGenerator.Fill(key2);
        byte[] plaintext = "test"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key1, plaintext);
        Assert.Throws<CryptographicException>(() => AesGcmDecryptor.Decrypt(key2, envelope));
    }

    [Fact]
    public void WrongExternalAAD_FailsAuthentication()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        byte[] plaintext = "test"u8.ToArray();
        byte[] aad1 = "context1"u8.ToArray();
        byte[] aad2 = "context2"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext, externalAAD: aad1);
        Assert.Throws<CryptographicException>(() => AesGcmDecryptor.Decrypt(key, envelope, externalAAD: aad2));
    }

    [Fact]
    public void MissingExpectedExternalAAD_FailsAuthentication()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        byte[] plaintext = "test"u8.ToArray();
        byte[] aad = "context"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext, externalAAD: aad);
        Assert.Throws<CryptographicException>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }

    [Fact]
    public void UnexpectedExternalAAD_FailsAuthentication()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        byte[] plaintext = "test"u8.ToArray();
        byte[] envelope = CreateValidEnvelope(key, plaintext);
        byte[] aad = "context"u8.ToArray();
        Assert.Throws<CryptographicException>(() => AesGcmDecryptor.Decrypt(key, envelope, externalAAD: aad));
    }


    private static byte[] CreateValidEnvelope(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> keyId = default)
    {
        if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes");

        int keyIdLen = keyId.Length;
        int ciphertextLen = plaintext.Length;
        int protocolHeaderLen = 22 + keyIdLen;
        int envelopeLen = protocolHeaderLen + ciphertextLen + 16;

        byte[] envelope = new byte[envelopeLen];
        Span<byte> span = envelope.AsSpan();

        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(span, V1Constants.Magic);
        span[2] = V1Constants.Version;
        span[3] = V1Constants.ProfileAes256Gcm;
        span[4] = V1Constants.ReservedFlags;
        span[5] = (byte)keyIdLen;

        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        nonce.CopyTo(span.Slice(6, 12));

        if (keyIdLen > 0)
            keyId.CopyTo(span.Slice(18, keyIdLen));

        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span.Slice(18 + keyIdLen, 4), ciphertextLen);

        Span<byte> ciphertext = span.Slice(protocolHeaderLen, ciphertextLen);
        Span<byte> tag = span.Slice(protocolHeaderLen + ciphertextLen, 16);

        Span<byte> aad = span.Slice(0, protocolHeaderLen);

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);

        return envelope;
    }
}
