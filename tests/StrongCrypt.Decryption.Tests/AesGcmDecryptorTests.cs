using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using StrongCrypt.Decryption;
using StrongCrypt.Protocol;

namespace StrongCrypt.Decryption.Tests;

public class AesGcmDecryptorTests
{
    private static readonly byte[] TestKey = new byte[32];

    static AesGcmDecryptorTests()
    {
        RandomNumberGenerator.Fill(TestKey);
    }

    [Fact]
    public void Decrypt_ValidEnvelope_ReturnsPlaintext()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("test message");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());

        byte[] decrypted = AesGcmDecryptor.Decrypt(TestKey, envelope);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Decrypt_WithExternalAAD_ReturnsPlaintext()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("test message");
        byte[] externalAAD = Encoding.UTF8.GetBytes("context");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, externalAAD);

        byte[] decrypted = AesGcmDecryptor.Decrypt(TestKey, envelope, externalAAD);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Decrypt_WrongKey_ThrowsCryptographicException()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("test message");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());
        byte[] wrongKey = new byte[32];
        wrongKey[0] = 1;

        Assert.Throws<CryptographicException>(() =>
            AesGcmDecryptor.Decrypt(wrongKey, envelope));
    }

    [Fact]
    public void Decrypt_CorruptedTag_ThrowsCryptographicException()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("test message");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());

        envelope[^1] ^= 1;

        Assert.Throws<CryptographicException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, envelope));
    }

    [Fact]
    public void Decrypt_CorruptedCiphertext_ThrowsCryptographicException()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("test message");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());

        envelope[22] ^= 1;

        Assert.Throws<CryptographicException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, envelope));
    }

    [Fact]
    public void Decrypt_WrongAAD_ThrowsCryptographicException()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("test message");
        byte[] externalAAD = Encoding.UTF8.GetBytes("original");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, externalAAD);

        byte[] wrongAAD = Encoding.UTF8.GetBytes("modified");

        Assert.Throws<CryptographicException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, envelope, wrongAAD));
    }

    [Fact]
    public void Decrypt_InvalidKeyLength_ThrowsArgumentException()
    {
        byte[] plaintext = new byte[16];
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());

        Assert.Throws<ArgumentException>(() =>
            AesGcmDecryptor.Decrypt(new byte[16], envelope));
    }

    [Fact]
    public void Decrypt_InvalidMagic_ThrowsArgumentException()
    {
        byte[] plaintext = new byte[16];
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());
        envelope[0] = 0xFF;

        Assert.Throws<ArgumentException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, envelope));
    }

    [Fact]
    public void Decrypt_InvalidVersion_ThrowsArgumentException()
    {
        byte[] plaintext = new byte[16];
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());
        envelope[2] = 99;

        Assert.Throws<ArgumentException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, envelope));
    }

    [Fact]
    public void Decrypt_InvalidProfile_ThrowsArgumentException()
    {
        byte[] plaintext = new byte[16];
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());
        envelope[3] = 99;

        Assert.Throws<ArgumentException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, envelope));
    }

    [Fact]
    public void Decrypt_TooShort_ThrowsArgumentException()
    {
        byte[] tooShort = new byte[37];

        Assert.Throws<ArgumentException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, tooShort));
    }

    [Fact]
    public void TryDecrypt_ValidEnvelope_ReturnsTrue()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("test message");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());
        byte[] destination = new byte[plaintext.Length];

        bool result = AesGcmDecryptor.TryDecrypt(TestKey, envelope, destination, out int bytesWritten);

        Assert.True(result);
        Assert.Equal(plaintext.Length, bytesWritten);
        Assert.Equal(plaintext, destination);
    }

    [Fact]
    public void TryDecrypt_DestinationTooSmall_ReturnsFalse()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("test message");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());
        byte[] destination = new byte[plaintext.Length - 1];

        bool result = AesGcmDecryptor.TryDecrypt(TestKey, envelope, destination, out int bytesWritten);

        Assert.False(result);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void TryDecrypt_WrongKey_ReturnsFalse()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("test message");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());
        byte[] wrongKey = new byte[32];
        wrongKey[0] = 1;
        byte[] destination = new byte[plaintext.Length];

        bool result = AesGcmDecryptor.TryDecrypt(wrongKey, envelope, destination, out int bytesWritten);

        Assert.False(result);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void GetPlaintextSize_ValidEnvelope_ReturnsCorrectSize()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("test message");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());

        int size = AesGcmDecryptor.GetPlaintextSize(envelope);

        Assert.Equal(plaintext.Length, size);
    }

    [Fact]
    public void GetPlaintextSize_InvalidEnvelope_ThrowsArgumentException()
    {
        byte[] invalid = new byte[10];

        Assert.Throws<ArgumentException>(() =>
            AesGcmDecryptor.GetPlaintextSize(invalid));
    }

    [Fact]
    public void Decrypt_EmptyPlaintext_Succeeds()
    {
        byte[] plaintext = Array.Empty<byte>();
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());

        byte[] decrypted = AesGcmDecryptor.Decrypt(TestKey, envelope);

        Assert.Empty(decrypted);
    }

    private static byte[] CreateValidEnvelope(byte[] key, byte[] plaintext, byte[] externalAAD)
    {
        byte[] keyId = Array.Empty<byte>();
        int keyIdLen = keyId.Length;

        // magic(2) + version(1) + profile(1) + flags(1) + keyIdLen(1) + keyId(N) + nonce(12) + ciphertextLen(4) + ciphertext(M) + tag(16)
        int envelopeSize = 2 + 1 + 1 + 1 + 1 + keyIdLen + 12 + 4 + plaintext.Length + 16;
        byte[] envelope = new byte[envelopeSize];

        int pos = 0;

        // Magic (little-endian 0x5343)
        envelope[pos++] = 0x43;
        envelope[pos++] = 0x53;
        // Version
        envelope[pos++] = 1;
        // Profile
        envelope[pos++] = 1;
        // Flags
        envelope[pos++] = 0;
        // KeyIdLen
        envelope[pos++] = (byte)keyIdLen;

        // KeyId (if any)
        if (keyIdLen > 0)
        {
            keyId.CopyTo(envelope, pos);
        }
        pos += keyIdLen;

        // Nonce
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        nonce.CopyTo(envelope, pos);
        pos += 12;

        // CiphertextLen
        BinaryPrimitives.WriteInt32LittleEndian(envelope.AsSpan(pos, 4), plaintext.Length);
        pos += 4;

        // Protocol header ends here: 22 + keyIdLen
        int headerEnd = 22 + keyIdLen;

        Span<byte> ciphertextSpan = envelope.AsSpan(pos, plaintext.Length);
        pos += plaintext.Length;

        Span<byte> tagSpan = envelope.AsSpan(pos, 16);

        // Construct AAD: protocolHeader || externalAAD
        ReadOnlySpan<byte> protocolHeader = envelope.AsSpan(0, headerEnd);
        Span<byte> aad = externalAAD.Length == 0
            ? protocolHeader.ToArray()
            : CombineAAD(protocolHeader, externalAAD);

        using (var aes = new AesGcm(key, V1Constants.TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertextSpan, tagSpan, aad);
        }

        return envelope;
    }

    private static byte[] CombineAAD(ReadOnlySpan<byte> header, byte[] external)
    {
        byte[] combined = new byte[header.Length + external.Length];
        header.CopyTo(combined);
        external.CopyTo(combined, header.Length);
        return combined;
    }
}
