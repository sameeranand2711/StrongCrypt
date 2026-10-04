# StrongCrypt.Encryption

**Encryption-only package for StrongCrypt V1 envelopes**

This package provides encryption operations only. It cannot decrypt data.

## Installation

```bash
dotnet add package StrongCrypt.Encryption
```

## Basic Usage

```csharp
using StrongCrypt.Encryption;

byte[] key = /* load your 32-byte key from secure storage */;
byte[] plaintext = "confidential data"u8.ToArray();

// Encrypt with automatic nonce generation
byte[] envelope = AesGcmEncryptor.Encrypt(
    plaintext: plaintext,
    key: key
);

// Store or transmit the envelope
await SaveToDatabase(envelope);
```

## API Reference

### `AesGcmEncryptor.Encrypt`

Encrypts plaintext and returns a complete V1 envelope.

**Parameters:**
- `plaintext` — Data to encrypt (ReadOnlySpan<byte>)
- `key` — 32-byte AES-256 key (ReadOnlySpan<byte>)
- `keyId` — Optional key identifier, 0-64 bytes (ReadOnlySpan<byte>)
- `associatedData` — Optional external AAD (ReadOnlySpan<byte>)

**Returns:** Complete V1 envelope as byte array

**Throws:**
- `ArgumentException` — Invalid key size (must be 32 bytes) or keyId length (max 64 bytes)
- `CryptographicException` — Encryption operation failed

### `AesGcmEncryptor.TryEncrypt`

Zero-copy encryption into caller-provided buffer.

**Parameters:**
- `plaintext` — Data to encrypt
- `destination` — Target buffer (must have sufficient capacity)
- `key` — 32-byte AES-256 key
- `keyId` — Optional key identifier, 0-64 bytes
- `associatedData` — Optional external AAD
- `bytesWritten` — Output: actual bytes written to destination

**Returns:** `true` if encryption succeeded, `false` if destination too small

### `AesGcmEncryptor.GetEnvelopeSize`

Calculates required buffer size for envelope.

**Parameters:**
- `plaintextLength` — Length of plaintext in bytes
- `keyIdLength` — Length of keyId field (0-64)

**Returns:** Total envelope size in bytes

**Formula:** `22 + keyIdLength + plaintextLength + 16`

## Key Management Examples

### With Key Versioning

```csharp
public class EncryptionService
{
    private readonly IKeyProvider _keyProvider;

    public byte[] EncryptWithCurrentKey(byte[] plaintext)
    {
        var (key, keyId) = _keyProvider.GetCurrentKey();
        
        return AesGcmEncryptor.Encrypt(
            plaintext: plaintext,
            key: key,
            keyId: Encoding.UTF8.GetBytes(keyId)
        );
    }
}
```

### With Tenant Isolation

```csharp
public byte[] EncryptForTenant(byte[] plaintext, string tenantId)
{
    byte[] tenantKey = _keyProvider.GetKeyForTenant(tenantId);
    byte[] aad = Encoding.UTF8.GetBytes($"tenant:{tenantId}");
    
    return AesGcmEncryptor.Encrypt(
        plaintext: plaintext,
        key: tenantKey,
        keyId: Encoding.UTF8.GetBytes(tenantId),
        associatedData: aad
    );
}
```

### High-Performance Batch Encryption

```csharp
public void EncryptBatch(ReadOnlySpan<byte[]> plaintexts, byte[] key, List<byte[]> output)
{
    foreach (var plaintext in plaintexts)
    {
        int envelopeSize = AesGcmEncryptor.GetEnvelopeSize(plaintext.Length, 0);
        byte[] envelope = new byte[envelopeSize];
        
        bool success = AesGcmEncryptor.TryEncrypt(
            plaintext: plaintext,
            destination: envelope,
            key: key,
            keyId: default,
            associatedData: default,
            out int written
        );
        
        if (success)
        {
            output.Add(envelope);
        }
    }
}
```

## Security Guidelines

### DO

✅ Generate keys using `RandomNumberGenerator.GetBytes(32)`  
✅ Store keys in Azure Key Vault, AWS KMS, or equivalent HSM  
✅ Use separate keys for different environments (dev/staging/prod)  
✅ Implement key rotation with version tracking via `keyId`  
✅ Use external AAD for request/session binding  
✅ Zero plaintext buffers after encryption when handling sensitive data  

### DON'T

❌ Hard-code keys in source code  
❌ Log encryption keys or plaintext  
❌ Reuse keys across trust boundaries  
❌ Store keys in environment variables without additional protection  
❌ Manually generate or control nonces (handled internally)  
❌ Encrypt more than 2^32 messages with the same key  

## Thread Safety

`AesGcmEncryptor` is stateless and thread-safe. Safe for concurrent use without synchronization.

## See Also

- [Main README](../../README.md) — Complete StrongCrypt documentation
- [Protocol Specification](../../docs/PROTOCOL_V1.md) — V1 wire format details
- [Threat Model](../../docs/THREAT_MODEL.md) — Security considerations
