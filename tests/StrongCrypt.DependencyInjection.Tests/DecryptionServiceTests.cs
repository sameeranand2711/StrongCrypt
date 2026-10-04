using System;
using Microsoft.Extensions.DependencyInjection;
using StrongCrypt.Decryption;
using StrongCrypt.Encryption;
using Xunit;

namespace StrongCrypt.DependencyInjection.Tests;

public sealed class DecryptionServiceTests
{
    [Fact]
    public void AddStrongCryptDecryption_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDecryptionKeyProvider, TestKeyProvider>();
        services.AddStrongCryptDecryption();
        
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<DecryptionService>();
        
        Assert.NotNull(service);
    }

    [Fact]
    public void AddStrongCryptDecryption_WithKeyProvider_RegistersBoth()
    {
        var services = new ServiceCollection();
        services.AddStrongCryptDecryption<TestKeyProvider>();
        
        var provider = services.BuildServiceProvider();
        var keyProvider = provider.GetService<IDecryptionKeyProvider>();
        var service = provider.GetService<DecryptionService>();
        
        Assert.NotNull(keyProvider);
        Assert.NotNull(service);
        Assert.IsType<TestKeyProvider>(keyProvider);
    }

    [Fact]
    public void DecryptionService_DecryptsAndUsesKeyProvider()
    {
        byte[] key = new byte[32];
        byte[] plaintext = "test message"u8.ToArray();
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key);
        
        var services = new ServiceCollection();
        services.AddStrongCryptDecryption<TestKeyProvider>();
        
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<DecryptionService>();
        
        byte[] decrypted = service.Decrypt(envelope);
        
        Assert.Equal(plaintext, decrypted);
    }

    private sealed class TestKeyProvider : IDecryptionKeyProvider
    {
        private static readonly byte[] TestKey = new byte[32];
        
        public byte[] GetKey(ReadOnlySpan<byte> keyId)
        {
            byte[] key = new byte[32];
            TestKey.CopyTo(key, 0);
            return key;
        }
    }
}
