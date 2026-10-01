using System;
using System.IO;
using System.Text;
using System.Text.Json;
using StrongCrypt.Decryption;
using Xunit;

namespace StrongCrypt.Integration.Tests;

public class FixtureDrivenTests
{
    private static string GetFixturePath()
    {
        string testDir = AppContext.BaseDirectory;
        string repoRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "tests", "fixtures", "v1-compatibility-fixtures.json");
    }

    [Theory]
    [InlineData("minimal")]
    [InlineData("basic")]
    [InlineData("with_keyid")]
    [InlineData("with_aad")]
    [InlineData("full")]
    public void CanonicalFixture_DecryptsCorrectly(string fixtureName)
    {
        string fixturesPath = GetFixturePath();
        string json = File.ReadAllText(fixturesPath);
        
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement fixtures = doc.RootElement.GetProperty("fixtures");
        
        JsonElement? fixture = null;
        foreach (JsonElement f in fixtures.EnumerateArray())
        {
            if (f.GetProperty("name").GetString() == fixtureName)
            {
                fixture = f;
                break;
            }
        }
        
        Assert.True(fixture.HasValue, $"Fixture '{fixtureName}' not found");
        
        byte[] key = Convert.FromHexString(fixture.Value.GetProperty("key").GetString()!);
        string plaintextHex = fixture.Value.GetProperty("plaintext").GetString()!;
        byte[] expectedPlaintext = string.IsNullOrEmpty(plaintextHex) ? [] : Convert.FromHexString(plaintextHex);
        byte[] envelope = Convert.FromHexString(fixture.Value.GetProperty("envelope").GetString()!);
        
        string externalAadHex = fixture.Value.GetProperty("externalAAD").GetString()!;
        byte[] externalAAD = string.IsNullOrEmpty(externalAadHex) ? [] : Convert.FromHexString(externalAadHex);
        
        byte[] recovered = externalAAD.Length > 0
            ? AesGcmDecryptor.Decrypt(key, envelope, externalAAD: externalAAD)
            : AesGcmDecryptor.Decrypt(key, envelope);
        
        Assert.Equal(expectedPlaintext, recovered);
    }
}
