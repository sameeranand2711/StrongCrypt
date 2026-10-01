using System;
using System.Text;
using System.Security.Cryptography;
using System.Buffers.Binary;
using StrongCrypt.Decryption;
using StrongCrypt.Protocol;
using Xunit;

namespace StrongCrypt.Decryption.Tests;

public class AesGcmDecryptorTests
{
    private static readonly byte[] TestKey = new byte[32];

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
        byte[] plaintext = Encoding.UTF8.GetBytes("authenticated data");
        byte[] externalAAD = Encoding.UTF8.GetBytes("context");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, externalAAD);

        byte[] decrypted = AesGcmDecryptor.Decrypt(TestKey, envelope, externalAAD);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Decrypt_WrongKey_ThrowsCryptographicException()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("secret");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());
        byte[] wrongKey = new byte[32];
        wrongKey[0] = 1;

        Assert.Throws<CryptographicException>(() =>
            AesGcmDecryptor.Decrypt(wrongKey, envelope));
    }

    [Fact]
    public void Decrypt_ModifiedCiphertext_ThrowsCryptographicException()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("tamper test");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());

        envelope[^1] ^= 1;

        Assert.Throws<CryptographicException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, envelope));
    }

    [Fact]
    public void Decrypt_ModifiedTag_ThrowsCryptographicException()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("tag test");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());

        envelope[22] ^= 1;

        Assert.Throws<CryptographicException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, envelope));
    }

    [Fact]
    public void Decrypt_ModifiedAAD_ThrowsCryptographicException()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("aad test");
        byte[] externalAAD = Encoding.UTF8.GetBytes("original");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, externalAAD);

        byte[] wrongAAD = Encoding.UTF8.GetBytes("modified");

        Assert.Throws<CryptographicException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, envelope, wrongAAD));
    }

    [Fact]
    public void Decrypt_InvalidKeyLength_ThrowsArgumentException()
    {
        byte[] envelope = CreateValidEnvelope(TestKey, new byte[16], Array.Empty<byte>());

        Assert.Throws<ArgumentException>(() =>
            AesGcmDecryptor.Decrypt(new byte[16], envelope));
    }

    [Fact]
    public void Decrypt_TooShort_ThrowsArgumentException()
    {
        byte[] tooShort = new byte[33];

        Assert.Throws<ArgumentException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, tooShort));
    }

    [Fact]
    public void Decrypt_WrongMagic_ThrowsArgumentException()
    {
        byte[] envelope = CreateValidEnvelope(TestKey, new byte[16], Array.Empty<byte>());
        envelope[0] = 0xFF;

        Assert.Throws<ArgumentException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, envelope));
    }

    [Fact]
    public void Decrypt_WrongVersion_ThrowsArgumentException()
    {
        byte[] envelope = CreateValidEnvelope(TestKey, new byte[16], Array.Empty<byte>());
        envelope[4] = 2;

        Assert.Throws<ArgumentException>(() =>
            AesGcmDecryptor.Decrypt(TestKey, envelope));
    }

    [Fact]
    public void TryDecrypt_ValidEnvelope_ReturnsTrue()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("try decrypt test");
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());
        Span<byte> destination = new byte[plaintext.Length];

        bool success = AesGcmDecryptor.TryDecrypt(TestKey, envelope, destination, out int bytesWritten);

        Assert.True(success);
        Assert.Equal(plaintext.Length, bytesWritten);
        Assert.True(plaintext.AsSpan().SequenceEqual(destination));
    }

    [Fact]
    public void TryDecrypt_DestinationTooSmall_ReturnsFalse()
    {
        byte[] plaintext = new byte[100];
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());
        Span<byte> destination = new byte[50];

        bool success = AesGcmDecryptor.TryDecrypt(TestKey, envelope, destination, out int bytesWritten);

        Assert.False(success);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void TryDecrypt_InvalidKey_ReturnsFalse()
    {
        byte[] envelope = CreateValidEnvelope(TestKey, new byte[16], Array.Empty<byte>());
        Span<byte> destination = new byte[16];

        bool success = AesGcmDecryptor.TryDecrypt(new byte[16], envelope, destination, out int bytesWritten);

        Assert.False(success);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void TryDecrypt_AuthenticationFails_ReturnsFalse()
    {
        byte[] plaintext = new byte[32];
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());
        envelope[^1] ^= 1;
        Span<byte> destination = new byte[plaintext.Length];

        bool success = AesGcmDecryptor.TryDecrypt(TestKey, envelope, destination, out int bytesWritten);

        Assert.False(success);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void GetPlaintextLength_ValidEnvelope_ReturnsCorrectLength()
    {
        byte[] plaintext = new byte[123];
        byte[] envelope = CreateValidEnvelope(TestKey, plaintext, Array.Empty<byte>());

        int length = AesGcmDecryptor.GetPlaintextLength(envelope);

        Assert.Equal(123, length);
    }

    [Fact]
    public void GetPlaintextLength_InvalidEnvelope_ThrowsArgumentException()
    {
        byte[] invalid = new byte[10];

        Assert.Throws<ArgumentException>(() =>
            AesGcmDecryptor.GetPlaintextLength(invalid));
    }

    [Fact]
    public void Decrypt_EmptyPlaintext_Succeeds()
    {
        byte[] empty = Array.Empty<byte>();
        byte[] envelope = CreateValidEnvelope(TestKey, empty, Array.Empty<byte>());

        byte[] decrypted = AesGcmDecryptor.Decrypt(TestKey, envelope);

        Assert.Empty(decrypted);
    }

    private static byte[] CreateValidEnvelope(byte[] key, byte[] plaintext, byte[] externalAAD)
    {
        byte[] keyId = Array.Empty<byte>();
        // magic(2) + version(1) + profile(1) + flags(1) + keyIdLen(1) + keyId(0) + nonce(12) + ciphertextLen(4) + ciphertext(N) + tag(16)
        int envelopeSize = 2 + 1 + 1 + 1 + 1 + keyId.Length + 12 + 4 + plaintext.Length + 16;
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
        envelope[pos++] = (byte)keyId.Length;
        
        int headerEnd = pos + keyId.Length;
        if (keyId.Length > 0)
            keyId.CopyTo(envelope, pos);
        pos = headerEnd;
        
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        nonce.CopyTo(envelope, pos);
        pos += 12;
        
        // CiphertextLen
        BinaryPrimitives.WriteInt32LittleEndian(envelope.AsSpan(pos, 4), plaintext.Length);
        pos += 4;
        
        Span<byte> ciphertextSpan = envelope.AsSpan(pos, plaintext.Length);
        pos += plaintext.Length;
        
        Span<byte> tagSpan = envelope.AsSpan(pos, 16);
        
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
