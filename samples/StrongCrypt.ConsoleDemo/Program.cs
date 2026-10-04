using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using StrongCrypt.Encryption;
using StrongCrypt.Decryption;

namespace StrongCrypt.ConsoleDemo;

/// <summary>
/// Minimal console demo showing encryption/decryption round-trip using DI extensions.
///
/// WARNING: This demo combines both encryption and decryption for demonstration only.
/// Production services should use ONLY ONE package (Encryption OR Decryption), never both.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("StrongCrypt V1 Console Demo");
        Console.WriteLine("============================");
        Console.WriteLine();
        Console.WriteLine("WARNING: Demo-only. Production services should reference");
        Console.WriteLine("         only Encryption OR Decryption, never both.");
        Console.WriteLine();

        // Generate ephemeral 32-byte demo key (AES-256)
        byte[] demoKey = new byte[32];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(demoKey);
        }

        try
        {
            // Configure DI container with both encryption and decryption services
            ServiceCollection services = new();

            // Register key provider and services
            DemoKeyProvider keyProvider = new(demoKey);
            services.AddSingleton<IEncryptionKeyProvider>(keyProvider);
            services.AddSingleton<IDecryptionKeyProvider>(keyProvider);
            services.AddStrongCryptEncryption();
            services.AddStrongCryptDecryption();

            using ServiceProvider provider = services.BuildServiceProvider();

            // Retrieve services
            EncryptionService? encryptionService = provider.GetService<EncryptionService>();
            DecryptionService? decryptionService = provider.GetService<DecryptionService>();

            if (encryptionService is null || decryptionService is null)
            {
                Console.WriteLine("FAIL: Services not registered correctly");
                return;
            }

            // Encrypt
            byte[] plaintext = Encoding.UTF8.GetBytes("Hello, StrongCrypt V1!");
            byte[] keyId = Encoding.UTF8.GetBytes("demo-key-001");
            byte[] externalAAD = Encoding.UTF8.GetBytes("demo-context");

            Console.WriteLine($"Plaintext:    {Encoding.UTF8.GetString(plaintext)}");
            Console.WriteLine($"Key ID:       {Encoding.UTF8.GetString(keyId)}");
            Console.WriteLine($"External AAD: {Encoding.UTF8.GetString(externalAAD)}");
            Console.WriteLine();

            byte[] encrypted = encryptionService.Encrypt(plaintext, keyId, externalAAD);
            Console.WriteLine($"Encrypted:    {encrypted.Length} bytes");
            Console.WriteLine();

            // Decrypt
            byte[] decryptedBytes = decryptionService.Decrypt(encrypted, keyId, externalAAD);
            string decrypted = Encoding.UTF8.GetString(decryptedBytes);

            Console.WriteLine($"Decrypted:    {decrypted}");
            Console.WriteLine();

            // Verify round-trip
            bool success = decrypted == "Hello, StrongCrypt V1!";

            Console.WriteLine(success ? "PASS: Round-trip successful" : "FAIL: Round-trip verification failed");
        }
        finally
        {
            // Zero ephemeral demo key
            CryptographicOperations.ZeroMemory(demoKey);
        }
    }

    private sealed class DemoKeyProvider : IEncryptionKeyProvider, IDecryptionKeyProvider
    {
        private readonly byte[] _key;

        public DemoKeyProvider(byte[] key)
        {
            _key = key;
        }

        public byte[] GetKey(ReadOnlySpan<byte> keyId)
        {
            byte[] copy = new byte[_key.Length];
            _key.CopyTo(copy, 0);
            return copy;
        }
    }
}
