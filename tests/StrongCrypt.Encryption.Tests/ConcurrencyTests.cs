using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace StrongCrypt.Encryption.Tests;

/// <summary>
/// Tests verifying thread-safety of stateless encryption APIs.
/// AesGcmEncryptor is stateless and must support concurrent calls.
/// </summary>
public class ConcurrencyTests
{
    [Fact]
    public void Encrypt_CalledConcurrently_ProducesValidEnvelopes()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);

        byte[] plaintext = "concurrent test"u8.ToArray();
        byte[] keyId = "key-123"u8.ToArray();

        const int parallelCount = 100;
        var envelopes = new byte[parallelCount][];

        Parallel.For(0, parallelCount, i =>
        {
            envelopes[i] = AesGcmEncryptor.Encrypt(plaintext, key, keyId);
        });

        // All envelopes should be valid and decrypt correctly
        foreach (var envelope in envelopes)
        {
            Assert.NotNull(envelope);
            Assert.True(envelope.Length > 38);
        }

        // Envelopes should differ (random nonces)
        var uniqueEnvelopes = envelopes.Distinct(new ByteArrayComparer()).Count();
        Assert.Equal(parallelCount, uniqueEnvelopes);
    }

    [Fact]
    public void TryEncrypt_CalledConcurrently_ProducesValidEnvelopes()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);

        byte[] plaintext = "concurrent test"u8.ToArray();
        byte[] keyId = "key-456"u8.ToArray();

        const int parallelCount = 100;
        var results = new (bool success, byte[] envelope)[parallelCount];

        Parallel.For(0, parallelCount, i =>
        {
            int envelopeSize = AesGcmEncryptor.GetEnvelopeSize(plaintext.Length, keyId.Length);
            byte[] destination = new byte[envelopeSize];
            bool success = AesGcmEncryptor.TryEncrypt(plaintext, destination, key, keyId, [], out int bytesWritten);
            results[i] = (success, destination);
        });

        // All calls should succeed
        Assert.All(results, r => Assert.True(r.success));

        // All envelopes should be unique (random nonces)
        var uniqueEnvelopes = results.Select(r => r.envelope).Distinct(new ByteArrayComparer()).Count();
        Assert.Equal(parallelCount, uniqueEnvelopes);
    }

    [Fact]
    public void GetEnvelopeSize_CalledConcurrently_ReturnsConsistentResults()
    {
        const int parallelCount = 1000;
        var results = new int[parallelCount];

        Parallel.For(0, parallelCount, i =>
        {
            results[i] = AesGcmEncryptor.GetEnvelopeSize(100, 10);
        });

        // All results should be identical
        Assert.All(results, size => Assert.Equal(148, size));
    }

    [Fact]
    public async Task Encrypt_MixedOperationsConcurrently_AllSucceed()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);

        var tasks = new List<Task<byte[]>>();

        // Launch 50 encrypt operations with varying inputs
        for (int i = 0; i < 50; i++)
        {
            int localI = i;
            tasks.Add(Task.Run(() =>
            {
                byte[] plaintext = new byte[localI + 1];
                RandomNumberGenerator.Fill(plaintext);
                byte[] keyId = localI % 2 == 0 ? "even"u8.ToArray() : Array.Empty<byte>();
                return AesGcmEncryptor.Encrypt(plaintext, key, keyId);
            }));
        }

        var envelopes = await Task.WhenAll(tasks);

        // All should succeed and be valid
        Assert.Equal(50, envelopes.Length);
        Assert.All(envelopes, env => Assert.NotNull(env));
    }

    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x == null || y == null) return x == y;
            return x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj == null || obj.Length == 0) return 0;
            return obj[0] ^ (obj.Length << 16);
        }
    }
}
