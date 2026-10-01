using System.Security.Cryptography;
using System.Text;
using StrongCrypt.Encryption;
using StrongCrypt.Decryption;
using Xunit;

namespace StrongCrypt.Integration.Tests;

public class InteroperabilityTests
{
    [Fact]
    public void RoundTrip_NoAad_Success()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] plaintext = Encoding.UTF8.GetBytes("interop test message");
        
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key);
        byte[] recovered = AesGcmDecryptor.Decrypt(key, envelope);
        
        Assert.Equal(plaintext, recovered);
    }
    
    [Fact]
    public void RoundTrip_WithAad_Success()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] plaintext = Encoding.UTF8.GetBytes("message with context");
        byte[] aad = Encoding.UTF8.GetBytes("user:alice;action:read");
        
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key, associatedData: aad);
        byte[] recovered = AesGcmDecryptor.Decrypt(key, envelope, externalAAD: aad);
        
        Assert.Equal(plaintext, recovered);
    }
    
    [Fact]
    public void RoundTrip_WithKeyId_Success()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] plaintext = Encoding.UTF8.GetBytes("message");
        byte[] keyId = Encoding.UTF8.GetBytes("key-2024-10");
        
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key, keyId: keyId);
        byte[] recovered = AesGcmDecryptor.Decrypt(key, envelope);
        
        Assert.Equal(plaintext, recovered);
    }
    
    [Fact]
    public void RoundTrip_EmptyPlaintext_Success()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] plaintext = [];
        
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key);
        byte[] recovered = AesGcmDecryptor.Decrypt(key, envelope);
        
        Assert.Empty(recovered);
    }
    
    [Fact]
    public void RoundTrip_LargePlaintext_Success()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] plaintext = new byte[1024 * 1024]; // 1 MB
        RandomNumberGenerator.Fill(plaintext);
        
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key);
        byte[] recovered = AesGcmDecryptor.Decrypt(key, envelope);
        
        Assert.Equal(plaintext, recovered);
    }
    
    [Fact]
    public void RoundTrip_WrongKey_ThrowsCryptographicException()
    {
        byte[] key1 = RandomNumberGenerator.GetBytes(32);
        byte[] key2 = RandomNumberGenerator.GetBytes(32);
        byte[] plaintext = Encoding.UTF8.GetBytes("secret");
        
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key1);
        
        Assert.Throws<CryptographicException>(() => AesGcmDecryptor.Decrypt(key2, envelope));
    }
    
    [Fact]
    public void RoundTrip_WrongAad_ThrowsCryptographicException()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] plaintext = Encoding.UTF8.GetBytes("secret");
        byte[] aad1 = Encoding.UTF8.GetBytes("context1");
        byte[] aad2 = Encoding.UTF8.GetBytes("context2");
        
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key, associatedData: aad1);
        
        Assert.Throws<CryptographicException>(() => AesGcmDecryptor.Decrypt(key, envelope, externalAAD: aad2));
    }
    
    [Fact]
    public void RoundTrip_MissingAad_ThrowsCryptographicException()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] plaintext = Encoding.UTF8.GetBytes("secret");
        byte[] aad = Encoding.UTF8.GetBytes("required context");
        
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key, associatedData: aad);
        
        Assert.Throws<CryptographicException>(() => AesGcmDecryptor.Decrypt(key, envelope));
    }
    
    [Fact]
    public void RoundTrip_MultipleMessages_IndependentNonces()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        
        byte[] plaintext1 = Encoding.UTF8.GetBytes("message one");
        byte[] plaintext2 = Encoding.UTF8.GetBytes("message two");
        byte[] plaintext3 = Encoding.UTF8.GetBytes("message three");
        
        byte[] envelope1 = AesGcmEncryptor.Encrypt(plaintext1, key);
        byte[] envelope2 = AesGcmEncryptor.Encrypt(plaintext2, key);
        byte[] envelope3 = AesGcmEncryptor.Encrypt(plaintext3, key);
        
        // Envelopes should differ (independent nonces)
        Assert.NotEqual(envelope1, envelope2);
        Assert.NotEqual(envelope2, envelope3);
        
        // All decrypt correctly
        Assert.Equal(plaintext1, AesGcmDecryptor.Decrypt(key, envelope1));
        Assert.Equal(plaintext2, AesGcmDecryptor.Decrypt(key, envelope2));
        Assert.Equal(plaintext3, AesGcmDecryptor.Decrypt(key, envelope3));
    }
    
    [Fact]
    public void RoundTrip_AllParametersCombined_Success()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] plaintext = Encoding.UTF8.GetBytes("full-featured message");
        byte[] keyId = Encoding.UTF8.GetBytes("prod-key-001");
        byte[] aad = Encoding.UTF8.GetBytes("tenant:acme;region:us-east");
        
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key, keyId: keyId, associatedData: aad);
        byte[] recovered = AesGcmDecryptor.Decrypt(key, envelope, externalAAD: aad);
        
        Assert.Equal(plaintext, recovered);
    }
}
