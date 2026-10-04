# StrongCrypt.Decryption

**Decryption-only package for StrongCrypt V1 envelopes**

This package provides decryption operations only. It cannot encrypt data.

## Installation

```bash
dotnet add package StrongCrypt.Decryption
```

## Basic Usage

```csharp
using StrongCrypt.Decryption;

byte[] key = /* load your 32-byte key from secure storage */;
byte[] envelope = /* received encrypted data */;

try
{
    byte[] plaintext = AesGcmDecryptor.Decrypt(
        key: key,
        envelope: envelope
    );
    
    // Use plaintext
    ProcessData(plaintext);
    
    // Zero sensitive data when done
    CryptographicOperations.ZeroMemory(plaintext);
}
catch (CryptographicException)
{
    // Authentication failed: wrong key, tampered data, or AAD mismatch
    // Plaintext is already zeroed automatically
}
catch (ArgumentException)
{
    // Invalid envelope format: malformed, truncated, or hostile input
}
```

## API Reference

### `AesGcmDecryptor.Decrypt`

Decrypts a V1 envelope and returns plaintext.

**Parameters:**
- `key` — 32-byte AES-256 key (ReadOnlySpan<byte>)
- `envelope` — Complete V1 envelope (ReadOnlySpan<byte>)
- `externalAAD` — Optional external AAD, must match encryption (ReadOnlySpan<byte>)

**Returns:** Decrypted plaintext as byte array

**Throws:**
- `ArgumentException` — Invalid key size or malformed envelope structure
- `CryptographicException` — Authentication or decryption failed

**Security:** On `CryptographicException`, the plaintext buffer is automatically zeroed before throwing.

### `AesGcmDecryptor.TryDecrypt`

Zero-copy decryption into caller-provided buffer.

**Parameters:**
- `key` — 32-byte AES-256 key
- `envelope` — Complete V1 envelope
- `plaintext` — Target buffer for decrypted data
- `externalAAD` — Optional external AAD
- `bytesWritten` — Output: actual plaintext length

**Returns:** `true` if decryption succeeded, `false` on authentication failure or invalid format

**Security:** On failure, the plaintext buffer is zeroed before returning false.

### `AesGcmDecryptor.GetPlaintextSize`

Parses envelope header to determine plaintext size without decrypting.

**Parameters:**
- `envelope` — V1 envelope to inspect

**Returns:** Expected plaintext size in bytes

**Throws:** `ArgumentException` if envelope header is malformed

**Use case:** Pre-allocate exact-sized buffer for `TryDecrypt`.

## Key Management Examples

### With Key Rotation Support

```csharp
public class DecryptionService
{
    private readonly IKeyProvider _keyProvider;

    public byte[] DecryptWithKeyRotation(byte[] envelope)
    {
        // Extract keyId from envelope header (if present)
        string keyId = ExtractKeyId(envelope);
        
        byte[] key = keyId != null 
            ? _keyProvider.GetKeyById(keyId)
            : _keyProvider.GetCurrentKey();
        
        return AesGcmDecryptor.Decrypt(
            key: key,
            envelope: envelope
        );
    }
    
    private string? ExtractKeyId(byte[] envelope)
    {
        if (envelope.Length < 6) return null;
        
        int keyIdLen = envelope[5];
        if (keyIdLen == 0 || envelope.Length < 18 + keyIdLen) return null;
        
        return Encoding.UTF8.GetString(envelope, 6, keyIdLen);
    }
}
```

### With Tenant Isolation

```csharp
public byte[] DecryptForTenant(byte[] envelope, string tenantId)
{
    byte[] tenantKey = _keyProvider.GetKeyForTenant(tenantId);
    byte[] aad = Encoding.UTF8.GetBytes($"tenant:{tenantId}");
    
    return AesGcmDecryptor.Decrypt(
        key: tenantKey,
        envelope: envelope,
        externalAAD: aad
    );
}
```

### High-Performance Batch Decryption

```csharp
public List<byte[]> DecryptBatch(ReadOnlySpan<byte[]> envelopes, byte[] key)
{
    var results = new List<byte[]>();
    
    foreach (var envelope in envelopes)
    {
        try
        {
            int plaintextSize = AesGcmDecryptor.GetPlaintextSize(envelope);
            byte[] plaintext = new byte[plaintextSize];
            
            bool success = AesGcmDecryptor.TryDecrypt(
                key: key,
                envelope: envelope,
                plaintext: plaintext,
                externalAAD: default,
                out int written
            );
            
            if (success)
            {
                results.Add(plaintext);
            }
        }
        catch (ArgumentException)
        {
            // Skip malformed envelopes
            continue;
        }
    }
    
    return results;
}
```

## Error Handling

### Authentication Failures

```csharp
try
{
    byte[] plaintext = AesGcmDecryptor.Decrypt(key, envelope, externalAAD);
}
catch (CryptographicException ex)
{
    // Possible causes:
    // 1. Wrong decryption key
    // 2. Data tampering (ciphertext or tag modified)
    // 3. External AAD mismatch
    // 4. Nonce reuse attack (if key was compromised)
    
    _logger.LogWarning("Decryption authentication failed: {Message}", ex.Message);
    // DO NOT log key, plaintext, or full envelope
    
    // Plaintext buffer is already zeroed
    // Safe to return error to caller
}
```

### Structural Failures

```csharp
try
{
    byte[] plaintext = AesGcmDecryptor.Decrypt(key, envelope);
}
catch (ArgumentException ex)
{
    // Possible causes:
    // 1. Truncated envelope
    // 2. Invalid magic number or version
    // 3. Malformed length fields
    // 4. Hostile input rejected by parser
    
    _logger.LogWarning("Invalid envelope format: {Message}", ex.Message);
    // Safe to log exception message (contains no secrets)
}
```

## Hostile Input Resistance

The parser is hardened against:

- **Truncation:** Rejects envelopes shorter than minimum valid length
- **Length field attacks:** Validates ciphertext length against remaining bytes
- **Integer overflow:** Uses checked arithmetic for size calculations
- **Trailing data:** Rejects envelopes with extra bytes after tag
- **Invalid reserved bits:** Rejects non-zero reserved flags
- **Unknown versions/profiles:** Only V1 profile 0x01 accepted

See [AdversarialTests.cs](../../tests/StrongCrypt.Decryption.Tests/AdversarialTests.cs) for comprehensive hostile input test suite.

## Security Guidelines

### DO

✅ Validate envelope source before attempting decryption  
✅ Use external AAD for request/session binding  
✅ Implement key rotation with fallback to old keys during transition  
✅ Zero plaintext buffers after use with `CryptographicOperations.ZeroMemory()`  
✅ Log authentication failures for security monitoring  
✅ Rate-limit decryption attempts to prevent brute-force attacks  

### DON'T

❌ Log or transmit decryption keys  
❌ Log plaintext content  
❌ Retry decryption with different keys automatically (potential oracle attack)  
❌ Trust envelope metadata (keyId is authenticated but application-controlled)  
❌ Decrypt untrusted envelopes without rate limiting  
❌ Expose detailed decryption failure reasons to untrusted callers  

## Thread Safety

`AesGcmDecryptor` is stateless and thread-safe. Safe for concurrent use without synchronization.

## Performance

- **Parser overhead:** ~100-200 ns for envelope validation
- **Decryption:** Hardware-accelerated AES-GCM (AES-NI / ARMv8 Crypto)
- **Memory:** Zero-copy parsing, no unnecessary allocations
- **GetPlaintextSize:** Parse-only, does not perform decryption

## See Also

- [Main README](../../README.md) — Complete StrongCrypt documentation
- [Protocol Specification](../../docs/PROTOCOL_V1.md) — V1 wire format details
- [Threat Model](../../docs/THREAT_MODEL.md) — Security considerations
