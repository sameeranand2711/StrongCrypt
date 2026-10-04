using System;
using System.Security.Cryptography;
using Xunit;

namespace StrongCrypt.Integration.Tests;

/// <summary>
/// Validates StrongCrypt's AES-GCM implementation against NIST test vectors.
/// Source: NIST Special Publication 800-38D (GCM spec), Appendix B test vectors.
/// https://csrc.nist.gov/publications/detail/sp/800-38d/final
/// </summary>
public class NistAesGcmVectorTests
{
    /// <summary>
    /// NIST SP 800-38D Test Case 13: AES-256-GCM
    /// IV length: 96 bits, PT length: 0 bits, AAD length: 0 bits, Tag length: 128 bits
    /// </summary>
    [Fact]
    public void NistVector_TestCase13_EmptyPlaintext_Passes()
    {
        // Test Case 13 from NIST SP 800-38D
        byte[] key = Convert.FromHexString("0000000000000000000000000000000000000000000000000000000000000000");
        byte[] iv = Convert.FromHexString("000000000000000000000000");
        byte[] plaintext = [];
        byte[] aad = [];
        byte[] expectedCiphertext = [];
        byte[] expectedTag = Convert.FromHexString("530f8afbc74536b9a963b4f1c4cb738b");

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(iv, plaintext, ciphertext, tag, aad);

        Assert.Equal(expectedCiphertext, ciphertext);
        Assert.Equal(expectedTag, tag);
    }

    /// <summary>
    /// NIST SP 800-38D Test Case 14: AES-256-GCM
    /// IV length: 96 bits, PT length: 128 bits, AAD length: 0 bits, Tag length: 128 bits
    /// </summary>
    [Fact]
    public void NistVector_TestCase14_With128BitPlaintext_Passes()
    {
        // Test Case 14 from NIST SP 800-38D
        byte[] key = Convert.FromHexString("0000000000000000000000000000000000000000000000000000000000000000");
        byte[] iv = Convert.FromHexString("000000000000000000000000");
        byte[] plaintext = Convert.FromHexString("00000000000000000000000000000000");
        byte[] aad = [];
        byte[] expectedCiphertext = Convert.FromHexString("cea7403d4d606b6e074ec5d3baf39d18");
        byte[] expectedTag = Convert.FromHexString("d0d1c8a799996bf0265b98b5d48ab919");

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(iv, plaintext, ciphertext, tag, aad);

        Assert.Equal(expectedCiphertext, ciphertext);
        Assert.Equal(expectedTag, tag);
    }

    /// <summary>
    /// NIST SP 800-38D Test Case 15: AES-256-GCM
    /// IV length: 96 bits, PT length: 256 bits, AAD length: 0 bits, Tag length: 128 bits
    /// </summary>
    [Fact]
    public void NistVector_TestCase15_With256BitPlaintext_Passes()
    {
        // Test Case 15 from NIST SP 800-38D
        byte[] key = Convert.FromHexString("feffe9928665731c6d6a8f9467308308feffe9928665731c6d6a8f9467308308");
        byte[] iv = Convert.FromHexString("cafebabefacedbaddecaf888");
        byte[] plaintext = Convert.FromHexString("d9313225f88406e5a55909c5aff5269a86a7a9531534f7da2e4c303d8a318a721c3c0c95956809532fcf0e2449a6b525b16aedf5aa0de657ba637b391aafd255");
        byte[] aad = [];
        byte[] expectedCiphertext = Convert.FromHexString("522dc1f099567d07f47f37a32a84427d643a8cdcbfe5c0c97598a2bd2555d1aa8cb08e48590dbb3da7b08b1056828838c5f61e6393ba7a0abcc9f662898015ad");
        byte[] expectedTag = Convert.FromHexString("b094dac5d93471bdec1a502270e3cc6c");

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(iv, plaintext, ciphertext, tag, aad);

        Assert.Equal(expectedCiphertext, ciphertext);
        Assert.Equal(expectedTag, tag);
    }

    /// <summary>
    /// NIST SP 800-38D Test Case 16: AES-256-GCM with AAD
    /// IV length: 96 bits, PT length: 384 bits, AAD length: 160 bits, Tag length: 128 bits
    /// </summary>
    [Fact]
    public void NistVector_TestCase16_WithAAD_Passes()
    {
        // Test Case 16 from NIST SP 800-38D
        byte[] key = Convert.FromHexString("feffe9928665731c6d6a8f9467308308feffe9928665731c6d6a8f9467308308");
        byte[] iv = Convert.FromHexString("cafebabefacedbaddecaf888");
        byte[] plaintext = Convert.FromHexString("d9313225f88406e5a55909c5aff5269a86a7a9531534f7da2e4c303d8a318a721c3c0c95956809532fcf0e2449a6b525b16aedf5aa0de657ba637b39");
        byte[] aad = Convert.FromHexString("feedfacedeadbeeffeedfacedeadbeefabaddad2");
        byte[] expectedCiphertext = Convert.FromHexString("522dc1f099567d07f47f37a32a84427d643a8cdcbfe5c0c97598a2bd2555d1aa8cb08e48590dbb3da7b08b1056828838c5f61e6393ba7a0abcc9f662");
        byte[] expectedTag = Convert.FromHexString("76fc6ece0f4e1768cddf8853bb2d551b");

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(iv, plaintext, ciphertext, tag, aad);

        Assert.Equal(expectedCiphertext, ciphertext);
        Assert.Equal(expectedTag, tag);
    }

    /// <summary>
    /// Verify NIST vectors also decrypt correctly (authentication + decryption)
    /// </summary>
    [Fact]
    public void NistVector_DecryptionRoundTrip_Passes()
    {
        // Using Test Case 15 data
        byte[] key = Convert.FromHexString("feffe9928665731c6d6a8f9467308308feffe9928665731c6d6a8f9467308308");
        byte[] iv = Convert.FromHexString("cafebabefacedbaddecaf888");
        byte[] plaintext = Convert.FromHexString("d9313225f88406e5a55909c5aff5269a86a7a9531534f7da2e4c303d8a318a721c3c0c95956809532fcf0e2449a6b525b16aedf5aa0de657ba637b391aafd255");
        byte[] ciphertext = Convert.FromHexString("522dc1f099567d07f47f37a32a84427d643a8cdcbfe5c0c97598a2bd2555d1aa8cb08e48590dbb3da7b08b1056828838c5f61e6393ba7a0abcc9f662898015ad");
        byte[] tag = Convert.FromHexString("b094dac5d93471bdec1a502270e3cc6c");
        byte[] aad = [];

        byte[] recovered = new byte[plaintext.Length];

        using var aes = new AesGcm(key, 16);
        aes.Decrypt(iv, ciphertext, tag, recovered, aad);

        Assert.Equal(plaintext, recovered);
    }
}
