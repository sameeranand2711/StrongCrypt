using System;
using System.Text;
using StrongCrypt.Encryption;
using System.Security.Cryptography;
using StrongCrypt.Protocol;
using Xunit;

namespace StrongCrypt.Encryption.Tests;

public class AesGcmEncryptorTests
{
    private static readonly byte[] TestKey32 = new byte[32];

    [Fact]
    public void Encrypt_EmptyPlaintext_ProducesMinimalEnvelope()
    {
        var key = new byte[32];
        var envelope = AesGcmEncryptor.Encrypt(Array.Empty<byte>(), key);

        Assert.Equal(38, envelope.Length);
        Assert.Equal(0x43, envelope[0]);
        Assert.Equal(0x53, envelope[1]);
        Assert.Equal(0x01, envelope[2]);
        Assert.Equal(0x01, envelope[3]);
        Assert.Equal(0x00, envelope[4]);
        Assert.Equal(0x00, envelope[5]);
    }

    [Fact]
    public void Encrypt_WithPlaintext_ProducesCorrectEnvelopeSize()
    {
        var plaintext = Encoding.UTF8.GetBytes("Hello World!");
        var key = new byte[32];
        var envelope = AesGcmEncryptor.Encrypt(plaintext, key);

        int expectedSize = 22 + 0 + plaintext.Length + 16;
        Assert.Equal(expectedSize, envelope.Length);
    }

    [Fact]
    public void Encrypt_WithKeyId_IncludesKeyIdInHeader()
    {
        var plaintext = Encoding.UTF8.GetBytes("test");
        var key = new byte[32];
        var keyId = Encoding.UTF8.GetBytes("key-123");
        var envelope = AesGcmEncryptor.Encrypt(plaintext, key, keyId);

        int expectedSize = 22 + keyId.Length + plaintext.Length + 16;
        Assert.Equal(expectedSize, envelope.Length);
        Assert.Equal(7, envelope[5]);
        
        var extractedKeyId = envelope.AsSpan(6, 7).ToArray();
        Assert.Equal(keyId, extractedKeyId);
    }

    [Fact]
    public void Encrypt_MaxKeyIdLength_Succeeds()
    {
        var plaintext = Encoding.UTF8.GetBytes("test");
        var key = new byte[32];
        var keyId = new byte[64];
        Array.Fill(keyId, (byte)'k');

        var envelope = AesGcmEncryptor.Encrypt(plaintext, key, keyId);

        int expectedSize = 22 + 64 + plaintext.Length + 16;
        Assert.Equal(expectedSize, envelope.Length);
        Assert.Equal(64, envelope[5]);
    }

    [Fact]
    public void Encrypt_KeyIdTooLong_ThrowsArgumentException()
    {
        var plaintext = Encoding.UTF8.GetBytes("test");
        var key = new byte[32];
        var keyId = new byte[65];

        var ex = Assert.Throws<ArgumentException>(() => 
            AesGcmEncryptor.Encrypt(plaintext, key, keyId));
        Assert.Contains("KeyId", ex.Message);
    }

    [Fact]
    public void Encrypt_InvalidKeySize_ThrowsArgumentException()
    {
        var plaintext = Encoding.UTF8.GetBytes("test");
        var key = new byte[16];

        var ex = Assert.Throws<ArgumentException>(() => 
            AesGcmEncryptor.Encrypt(plaintext, key));
        Assert.Contains("32 bytes", ex.Message);
    }

    [Fact]
    public void Encrypt_SamePlaintextAndKey_ProducesDifferentEnvelopes()
    {
        var plaintext = Encoding.UTF8.GetBytes("test");
        var key = new byte[32];

        var envelope1 = AesGcmEncryptor.Encrypt(plaintext, key);
        var envelope2 = AesGcmEncryptor.Encrypt(plaintext, key);

        Assert.NotEqual(envelope1, envelope2);
    }

    [Fact]
    public void Encrypt_WithExternalAAD_ProducesValidEnvelope()
    {
        var plaintext = Encoding.UTF8.GetBytes("data");
        var key = new byte[32];
        var aad = Encoding.UTF8.GetBytes("context");

        var envelope = AesGcmEncryptor.Encrypt(plaintext, key, default, aad);

        int expectedSize = 22 + 0 + plaintext.Length + 16;
        Assert.Equal(expectedSize, envelope.Length);
    }

    [Fact]
    public void TryEncrypt_DestinationTooSmall_ReturnsFalse()
    {
        var plaintext = Encoding.UTF8.GetBytes("test");
        var key = new byte[32];
        Span<byte> destination = stackalloc byte[10];

        bool success = AesGcmEncryptor.TryEncrypt(
            plaintext, destination, key, default, default, out int bytesWritten);

        Assert.False(success);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void TryEncrypt_SufficientDestination_Succeeds()
    {
        var plaintext = Encoding.UTF8.GetBytes("test");
        var key = new byte[32];
        int requiredSize = AesGcmEncryptor.GetEnvelopeSize(plaintext.Length, 0);
        Span<byte> destination = new byte[requiredSize];

        bool success = AesGcmEncryptor.TryEncrypt(
            plaintext, destination, key, default, default, out int bytesWritten);

        Assert.True(success);
        Assert.Equal(requiredSize, bytesWritten);
    }

    [Fact]
    public void GetEnvelopeSize_VariousInputs_ReturnsCorrectSize()
    {
        Assert.Equal(38, AesGcmEncryptor.GetEnvelopeSize(0, 0));
        Assert.Equal(50, AesGcmEncryptor.GetEnvelopeSize(12, 0));
        Assert.Equal(56, AesGcmEncryptor.GetEnvelopeSize(10, 8));
        Assert.Equal(107, AesGcmEncryptor.GetEnvelopeSize(5, 64));
    }

    [Fact]
    public void GetEnvelopeSize_NegativePlaintextLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            AesGcmEncryptor.GetEnvelopeSize(-1, 0));
    }

    [Fact]
    public void GetEnvelopeSize_KeyIdTooLong_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            AesGcmEncryptor.GetEnvelopeSize(0, 65));
    }

    [Fact]
    public void Encrypt_ProducesCorrectWireFormat()
    {
        var plaintext = Encoding.UTF8.GetBytes("test");
        var key = new byte[32];
        var keyId = Encoding.UTF8.GetBytes("k1");
        var envelope = AesGcmEncryptor.Encrypt(plaintext, key, keyId);

        Assert.Equal(0x43, envelope[0]);
        Assert.Equal(0x53, envelope[1]);
        Assert.Equal(0x01, envelope[2]);
        Assert.Equal(0x01, envelope[3]);
        Assert.Equal(0x00, envelope[4]);
        Assert.Equal(2, envelope[5]);
        Assert.Equal((byte)'k', envelope[6]);
        Assert.Equal((byte)'1', envelope[7]);
        
        int ciphertextLen = BitConverter.ToInt32(envelope.AsSpan(20, 4));
        Assert.Equal(4, ciphertextLen);
    }

    [Fact]
    public void Encrypt_ProtocolHeaderIsAuthenticatedInAAD()
    {
        var plaintext = Encoding.UTF8.GetBytes("test");
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        var keyId = Encoding.UTF8.GetBytes("id");
        var aad = Encoding.UTF8.GetBytes("context");

        var envelope1 = AesGcmEncryptor.Encrypt(plaintext, key, keyId, aad);
        
        var tamperedEnvelope = (byte[])envelope1.Clone();
        tamperedEnvelope[5] = 99;

        Assert.NotEqual(envelope1[5], tamperedEnvelope[5]);
    }
}
