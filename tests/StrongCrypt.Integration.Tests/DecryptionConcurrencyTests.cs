using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using StrongCrypt.Encryption;
using StrongCrypt.Decryption;
using Xunit;

namespace StrongCrypt.Integration.Tests;

/// <summary>
/// Tests verifying thread-safety of stateless decryption APIs.
/// AesGcmDecryptor is stateless and must support concurrent calls.
/// </summary>
public class DecryptionConcurrencyTests
{
    [Fact]
    public void Decrypt_CalledConcurrently_AllSucceed()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);

        byte[] plaintext = "concurrent decryption test"u8.ToArray();
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key);

        const int parallelCount = 100;
        var results = new byte[parallelCount][];

        Parallel.For(0, parallelCount, i =>
        {
            results[i] = AesGcmDecryptor.Decrypt(key, envelope);
        });

        // All decryptions should succeed and produce identical plaintext
        Assert.All(results, result => Assert.Equal(plaintext, result));
    }

    [Fact]
    public void TryDecrypt_CalledConcurrently_AllSucceed()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);

        byte[] plaintext = "concurrent try decrypt"u8.ToArray();
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key);

        const int parallelCount = 100;
        var results = new (bool success, byte[] plaintext)[parallelCount];

        Parallel.For(0, parallelCount, i =>
        {
            byte[] destination = new byte[plaintext.Length];
            bool success = AesGcmDecryptor.TryDecrypt(key, envelope, destination, out int bytesWritten);
            results[i] = (success, destination);
        });

        // All should succeed
        Assert.All(results, r => Assert.True(r.success));
        Assert.All(results, r => Assert.Equal(plaintext, r.plaintext));
    }

    [Fact]
    public void GetPlaintextSize_CalledConcurrently_ReturnsConsistentResults()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);

        byte[] plaintext = new byte[100];
        RandomNumberGenerator.Fill(plaintext);
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key);

        const int parallelCount = 1000;
        var results = new int[parallelCount];

        Parallel.For(0, parallelCount, i =>
        {
            results[i] = AesGcmDecryptor.GetPlaintextSize(envelope);
        });

        // All results should be identical
        Assert.All(results, size => Assert.Equal(100, size));
    }

    [Fact]
    public async Task Decrypt_MixedOperationsConcurrently_AllSucceed()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);

        // Create 50 different envelopes
        var testCases = new (byte[] plaintext, byte[] envelope)[50];
        for (int i = 0; i < 50; i++)
        {
            byte[] plaintext = new byte[i + 1];
            RandomNumberGenerator.Fill(plaintext);
            byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key);
            testCases[i] = (plaintext, envelope);
        }

        var tasks = new List<Task<byte[]>>();

        // Launch concurrent decryptions
        for (int i = 0; i < 50; i++)
        {
            int localI = i;
            tasks.Add(Task.Run(() => AesGcmDecryptor.Decrypt(key, testCases[localI].envelope)));
        }

        var results = await Task.WhenAll(tasks);

        // All should match original plaintext
        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(testCases[i].plaintext, results[i]);
        }
    }

    [Fact]
    public void Decrypt_WithExternalAAD_CalledConcurrently_AllSucceed()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);

        byte[] plaintext = "aad test"u8.ToArray();
        byte[] externalAAD = "tenant:acme"u8.ToArray();
        byte[] envelope = AesGcmEncryptor.Encrypt(plaintext, key, associatedData: externalAAD);

        const int parallelCount = 100;
        var results = new byte[parallelCount][];

        Parallel.For(0, parallelCount, i =>
        {
            results[i] = AesGcmDecryptor.Decrypt(key, envelope, externalAAD);
        });

        Assert.All(results, result => Assert.Equal(plaintext, result));
    }

    [Fact]
    public void Decrypt_InterleavedSuccessAndFailure_BothHandledCorrectly()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);

        byte[] plaintext = "mixed test"u8.ToArray();
        byte[] validEnvelope = AesGcmEncryptor.Encrypt(plaintext, key);

        byte[] invalidEnvelope = (byte[])validEnvelope.Clone();
        invalidEnvelope[^1] ^= 0xFF; // Corrupt tag

        const int parallelCount = 100;
        var results = new (bool success, Exception? error)[parallelCount];

        Parallel.For(0, parallelCount, i =>
        {
            try
            {
                byte[] envelope = i % 2 == 0 ? validEnvelope : invalidEnvelope;
                AesGcmDecryptor.Decrypt(key, envelope);
                results[i] = (true, null);
            }
            catch (Exception ex)
            {
                results[i] = (false, ex);
            }
        });

        // Even indices should succeed, odd should fail
        for (int i = 0; i < parallelCount; i++)
        {
            if (i % 2 == 0)
            {
                Assert.True(results[i].success, $"Index {i} should succeed");
            }
            else
            {
                Assert.False(results[i].success, $"Index {i} should fail");
                Assert.NotNull(results[i].error);
            }
        }
    }
}
