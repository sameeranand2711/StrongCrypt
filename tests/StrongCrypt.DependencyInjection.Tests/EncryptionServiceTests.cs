using System;
using Microsoft.Extensions.DependencyInjection;
using StrongCrypt.Encryption;
using Xunit;

namespace StrongCrypt.DependencyInjection.Tests;

public sealed class EncryptionServiceTests
{
    [Fact]
    public void AddStrongCryptEncryption_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEncryptionKeyProvider, TestKeyProvider>();
        services.AddStrongCryptEncryption();
        
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<EncryptionService>();
        
        Assert.NotNull(service);
    }

    [Fact]
    public void AddStrongCryptEncryption_WithKeyProvider_RegistersBoth()
    {
        var services = new ServiceCollection();
        services.AddStrongCryptEncryption<TestKeyProvider>();
        
        var provider = services.BuildServiceProvider();
        var keyProvider = provider.GetService<IEncryptionKeyProvider>();
        var service = provider.GetService<EncryptionService>();
        
        Assert.NotNull(keyProvider);
        Assert.NotNull(service);
        Assert.IsType<TestKeyProvider>(keyProvider);
    }

    [Fact]
    public void EncryptionService_EncryptsAndUsesKeyProvider()
    {
        var services = new ServiceCollection();
        services.AddStrongCryptEncryption<TestKeyProvider>();
        
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<EncryptionService>();
        
        byte[] plaintext = "test message"u8.ToArray();
        byte[] envelope = service.Encrypt(plaintext);
        
        Assert.NotNull(envelope);
        Assert.True(envelope.Length > plaintext.Length);
    }

    private sealed class TestKeyProvider : IEncryptionKeyProvider
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
