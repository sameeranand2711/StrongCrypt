using System;
using System.Text;
using System.Text.Json;
using StrongCrypt.Decryption;
using Xunit;
using Xunit.Abstractions;

namespace StrongCrypt.Integration.Tests;

public class CompatibilityFixtureTests
{
    private readonly ITestOutputHelper _output;

    public CompatibilityFixtureTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Fixture_Minimal_GeneratesAndDecrypts()
    {
        byte[] key = Convert.FromHexString("0000000000000000000000000000000000000000000000000000000000000000");
        byte[] plaintext = [];
        byte[] nonce = Convert.FromHexString("000000000000000000000000");

        byte[] envelope = FixtureGenerator.GenerateEnvelopeWithFixedNonce(key, plaintext, nonce);

        _output.WriteLine($"minimal envelope: {Convert.ToHexString(envelope).ToLowerInvariant()}");

        byte[] recovered = AesGcmDecryptor.Decrypt(key, envelope);
        Assert.Empty(recovered);
    }

    [Fact]
    public void Fixture_Basic_GeneratesAndDecrypts()
    {
        byte[] key = Convert.FromHexString("0101010101010101010101010101010101010101010101010101010101010101");
        byte[] plaintext = Encoding.UTF8.GetBytes("hello");
        byte[] nonce = Convert.FromHexString("010101010101010101010101");

        byte[] envelope = FixtureGenerator.GenerateEnvelopeWithFixedNonce(key, plaintext, nonce);

        _output.WriteLine($"basic envelope: {Convert.ToHexString(envelope).ToLowerInvariant()}");

        byte[] recovered = AesGcmDecryptor.Decrypt(key, envelope);
        Assert.Equal("hello", Encoding.UTF8.GetString(recovered));
    }

    [Fact]
    public void Fixture_WithKeyId_GeneratesAndDecrypts()
    {
        byte[] key = Convert.FromHexString("0202020202020202020202020202020202020202020202020202020202020202");
        byte[] plaintext = Encoding.UTF8.GetBytes("data");
        byte[] keyId = Encoding.UTF8.GetBytes("key-001");
        byte[] nonce = Convert.FromHexString("020202020202020202020202");

        byte[] envelope = FixtureGenerator.GenerateEnvelopeWithFixedNonce(key, plaintext, nonce, keyId: keyId);

        _output.WriteLine($"with_keyid envelope: {Convert.ToHexString(envelope).ToLowerInvariant()}");

        byte[] recovered = AesGcmDecryptor.Decrypt(key, envelope);
        Assert.Equal("data", Encoding.UTF8.GetString(recovered));
    }

    [Fact]
    public void Fixture_WithAad_GeneratesAndDecrypts()
    {
        byte[] key = Convert.FromHexString("0303030303030303030303030303030303030303030303030303030303030303");
        byte[] plaintext = Encoding.UTF8.GetBytes("secret");
        byte[] externalAAD = Encoding.UTF8.GetBytes("user:alice");
        byte[] nonce = Convert.FromHexString("030303030303030303030303");

        byte[] envelope = FixtureGenerator.GenerateEnvelopeWithFixedNonce(key, plaintext, nonce, externalAAD: externalAAD);

        _output.WriteLine($"with_aad envelope: {Convert.ToHexString(envelope).ToLowerInvariant()}");

        byte[] recovered = AesGcmDecryptor.Decrypt(key, envelope, externalAAD: externalAAD);
        Assert.Equal("secret", Encoding.UTF8.GetString(recovered));
    }

    [Fact]
    public void Fixture_Full_GeneratesAndDecrypts()
    {
        byte[] key = Convert.FromHexString("0404040404040404040404040404040404040404040404040404040404040404");
        byte[] plaintext = Encoding.UTF8.GetBytes("full test");
        byte[] keyId = Encoding.UTF8.GetBytes("prod-key-123");
        byte[] externalAAD = Encoding.UTF8.GetBytes("tenant:acme");
        byte[] nonce = Convert.FromHexString("040404040404040404040404");

        byte[] envelope = FixtureGenerator.GenerateEnvelopeWithFixedNonce(key, plaintext, nonce, keyId: keyId, externalAAD: externalAAD);

        _output.WriteLine($"full envelope: {Convert.ToHexString(envelope).ToLowerInvariant()}");

        byte[] recovered = AesGcmDecryptor.Decrypt(key, envelope, externalAAD: externalAAD);
        Assert.Equal("full test", Encoding.UTF8.GetString(recovered));
    }
}
