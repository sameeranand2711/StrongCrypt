# StrongCrypt

**Opinionated, boundary-enforced AES-256-GCM encryption for .NET 10+**

Version: **1.0.0-rc.1**

StrongCrypt provides physically separated encryption and decryption packages with a frozen wire format. Designed for scenarios where you want strong cryptographic boundaries, deterministic envelope format, and no accidental capability leakage.

## Packages

- **StrongCrypt.Encryption** — Encrypt plaintext, produce V1 envelopes
- **StrongCrypt.Decryption** — Decrypt V1 envelopes, hostile-input hardened parser
- **StrongCrypt.Protocol** — Shared constants and envelope format (no crypto operations)

**By design:** Encryption and Decryption packages never reference each other. A service that only encrypts cannot accidentally decrypt.

## V1 Cryptographic Profile

- **Algorithm:** AES-256-GCM (AEAD)
- **Key size:** 32 bytes (256 bits)
- **Nonce:** 12 bytes, cryptographically random, generated per encryption
- **Tag size:** 16 bytes (128 bits)
- **AAD:** Protocol header + optional external AAD

## Quick Start

### Installation

```bash
# For services that only encrypt
dotnet add package StrongCrypt.Encryption

# For services that only decrypt
dotnet add package StrongCrypt.Decryption

# For shared protocol constants (usually not needed directly)
dotnet add package StrongCrypt.Protocol
```

### Encryption

```csharp
using StrongCrypt.Encryption;

// Your 32-byte encryption key (manage securely, see Key Management below)
byte[] key = /* load from secure key storage */;

byte[] plaintext = "sensitive data"u8.ToArray();

// Basic encryption
byte[] envelope = AesGcmEncryptor.Encrypt(
    plaintext: plaintext,
    key: key
);

// With key identifier and external AAD
byte[] envelopeWithMetadata = AesGcmEncryptor.Encrypt(
    plaintext: plaintext,
    key: key,
    keyId: "key-2024-10"u8.ToArray(),
    associatedData: "tenant:acme"u8.ToArray()
);
```

### Decryption

```csharp
using StrongCrypt.Decryption;

// Your 32-byte decryption key
byte[] key = /* load from secure key storage */;

byte[] envelope = /* received encrypted data */;

try
{
    // Basic decryption
    byte[] plaintext = AesGcmDecryptor.Decrypt(
        key: key,
        envelope: envelope
    );

    // With external AAD (must match encryption)
    byte[] plaintextWithAAD = AesGcmDecryptor.Decrypt(
        key: key,
        envelope: envelope,
        externalAAD: "tenant:acme"u8.ToArray()
    );
}
catch (CryptographicException)
{
    // Authentication failed: wrong key, tampered data, or AAD mismatch
    // Plaintext is zeroed automatically on failure
}
catch (ArgumentException)
{
    // Invalid envelope format: truncated, corrupted, or hostile input
}
```

## Dependency Injection

StrongCrypt provides optional DI extensions for `Microsoft.Extensions.DependencyInjection`.

### Encryption with DI

```csharp
using Microsoft.Extensions.DependencyInjection;
using StrongCrypt.Encryption;

// 1. Implement a key provider
public class MyEncryptionKeyProvider : IEncryptionKeyProvider
{
    private readonly IKeyVaultClient _keyVault;

    public MyEncryptionKeyProvider(IKeyVaultClient keyVault)
    {
        _keyVault = keyVault;
    }

    public byte[] GetKey(ReadOnlySpan<byte> keyId)
    {
        // Retrieve key from secure storage based on keyId
        string keyIdString = Encoding.UTF8.GetString(keyId);
        return _keyVault.GetKey(keyIdString);
    }
}

// 2. Register services
services.AddStrongCryptEncryption<MyEncryptionKeyProvider>();

// 3. Inject and use
public class MyService
{
    private readonly EncryptionService _encryption;

    public MyService(EncryptionService encryption)
    {
        _encryption = encryption;
    }

    public byte[] EncryptData(byte[] plaintext, string keyVersion)
    {
        byte[] keyId = Encoding.UTF8.GetBytes(keyVersion);
        return _encryption.Encrypt(plaintext, keyId);
    }
}
```

### Decryption with DI

```csharp
using Microsoft.Extensions.DependencyInjection;
using StrongCrypt.Decryption;

// 1. Implement a key provider
public class MyDecryptionKeyProvider : IDecryptionKeyProvider
{
    private readonly IKeyVaultClient _keyVault;

    public MyDecryptionKeyProvider(IKeyVaultClient keyVault)
    {
        _keyVault = keyVault;
    }

    public byte[] GetKey(ReadOnlySpan<byte> keyId)
    {
        // Retrieve key from secure storage based on keyId
        string keyIdString = Encoding.UTF8.GetString(keyId);
        return _keyVault.GetKey(keyIdString);
    }
}

// 2. Register services
services.AddStrongCryptDecryption<MyDecryptionKeyProvider>();

// 3. Inject and use
public class MyService
{
    private readonly DecryptionService _decryption;

    public MyService(DecryptionService decryption)
    {
        _decryption = decryption;
    }

    public byte[] DecryptData(byte[] envelope)
    {
        // keyId is automatically extracted from envelope
        return _decryption.Decrypt(envelope);
    }
}
```

### Alternative: Manual Key Provider Registration

```csharp
// Register key provider separately, then add service
services.AddSingleton<IEncryptionKeyProvider, MyEncryptionKeyProvider>();
services.AddStrongCryptEncryption();

// Or for decryption
services.AddSingleton<IDecryptionKeyProvider, MyDecryptionKeyProvider>();
services.AddStrongCryptDecryption();
```

**Important:** The DI integration automatically handles key zeroing. Keys retrieved from `IEncryptionKeyProvider` or `IDecryptionKeyProvider` are zeroed via `CryptographicOperations.ZeroMemory` after use.

## Zero-Copy APIs

For performance-sensitive scenarios, use `TryEncrypt` / `TryDecrypt`:

```csharp
// Encryption with caller-provided buffer
int envelopeSize = AesGcmEncryptor.GetEnvelopeSize(plaintext.Length, keyId.Length);
byte[] destination = new byte[envelopeSize];

bool success = AesGcmEncryptor.TryEncrypt(
    plaintext: plaintext,
    destination: destination,
    key: key,
    keyId: keyId,
    associatedData: associatedData,
    out int bytesWritten
);

// Decryption with caller-provided buffer
int maxPlaintextSize = AesGcmDecryptor.GetPlaintextSize(envelope);
byte[] plaintextBuffer = new byte[maxPlaintextSize];

bool decryptSuccess = AesGcmDecryptor.TryDecrypt(
    key: key,
    envelope: envelope,
    plaintext: plaintextBuffer,
    externalAAD: externalAAD,
    out int plaintextLength
);
```

## Key Management

**StrongCrypt does not manage keys.** You are responsible for:

- **Generation:** Use `RandomNumberGenerator.GetBytes(32)` for new keys
- **Storage:** Azure Key Vault, AWS KMS, HashiCorp Vault, or equivalent HSM/KMS
- **Rotation:** Track key versions, support decryption of old envelopes during transition
- **Access control:** Limit key access to authorized services only

**Never:**
- Hard-code keys in source code
- Store keys in configuration files or environment variables without encryption
- Log or transmit keys in plaintext
- Reuse keys across security boundaries

### Key Identifier (keyId)

The optional `keyId` field (0-64 bytes) is:
- **Non-secret:** Included in the envelope plaintext header
- **Application-defined:** Use for key versioning, rotation tracking, or tenant isolation
- **Authenticated but not encrypted:** Protected by GCM authentication

Example usage:
```csharp
byte[] keyId = "prod-key-2024-10"u8.ToArray();  // Version identifier
byte[] keyId = Encoding.UTF8.GetBytes($"tenant-{tenantId}");  // Tenant routing
```

## Associated Data (AAD)

Optional AAD binds encrypted data to context without encrypting it:

```csharp
// Encrypt with context
byte[] envelope = AesGcmEncryptor.Encrypt(
    plaintext: sensitiveData,
    key: key,
    associatedData: $"user:{userId}:action:transfer"u8.ToArray()
);

// Decryption REQUIRES matching AAD
byte[] plaintext = AesGcmDecryptor.Decrypt(
    key: key,
    envelope: envelope,
    externalAAD: $"user:{userId}:action:transfer"u8.ToArray()  // Must match
);
```

**AAD mismatch = authentication failure.** Use AAD for:
- Request/session binding
- Multi-tenant isolation
- Operation context verification

## Error Handling

```csharp
try
{
    byte[] plaintext = AesGcmDecryptor.Decrypt(key, envelope, externalAAD);
    // Success: plaintext is valid
}
catch (CryptographicException)
{
    // Authentication failure:
    // - Wrong decryption key
    // - Tampered/corrupted ciphertext or tag
    // - AAD mismatch
    // - Nonce reuse (if key was compromised)
    
    // Plaintext buffer is automatically zeroed
    // Log securely (DO NOT log key or plaintext)
}
catch (ArgumentException ex)
{
    // Structural failure:
    // - Invalid envelope format
    // - Truncated data
    // - Malformed header
    // - Hostile input rejected
    
    // Safe to log exception message (contains no secrets)
}
```

## Thread Safety

All APIs are stateless and thread-safe. No locking required for concurrent calls.

## V1 Non-Goals

StrongCrypt V1 explicitly does **NOT** provide:

- Key derivation or password-based encryption (use Argon2id separately)
- Key management, rotation automation, or secure key storage
- Compression (compress before encryption if needed)
- Streaming encryption (V1 is single-pass, full-buffer only)
- Other algorithms or profiles (V1 = AES-256-GCM only)
- Nonce misuse resistance (XSalsa20-Poly1305, AES-GCM-SIV require different design)

## Wire Format

V1 envelope structure (little-endian):

```
[Magic:2][Ver:1][Prof:1][Flags:1][KeyIdLen:1][KeyId:0-64][Nonce:12][CtLen:4][Ciphertext:N][Tag:16]
```

- **Magic:** `0x4353` ("SC")
- **Version:** `0x01`
- **Profile:** `0x01` (AES-256-GCM)
- **Flags:** `0x00` (reserved, must be zero)
- **KeyIdLen:** Length of KeyId field (0-64)
- **KeyId:** Optional key identifier (0-64 bytes)
- **Nonce:** 12-byte nonce (random, unique per encryption)
- **CtLen:** Ciphertext length (32-bit signed int)
- **Ciphertext:** Encrypted plaintext
- **Tag:** 16-byte GCM authentication tag

**Authenticated region:** Entire protocol header + external AAD + ciphertext

See [docs/PROTOCOL_V1.md](docs/PROTOCOL_V1.md) for complete specification.

## Security Considerations

- **Nonce uniqueness:** Automatically enforced via CSPRNG. Never encrypt more than 2^32 messages with the same key (rotate before reaching this limit).
- **Key scope:** Use separate keys for separate trust boundaries, tenants, or environments.
- **Timing attacks:** Decryption uses constant-time comparison for authentication tags.
- **Memory safety:** Failed decryptions zero plaintext buffers automatically.
- **Parser hardening:** Decryption rejects truncated, oversized, and malformed envelopes before cryptographic operations.

See [docs/THREAT_MODEL.md](docs/THREAT_MODEL.md) for detailed security analysis.

## Requirements

- **.NET 10.0+**
- **Platform:** Requires AES-GCM hardware support (AES-NI on x64, ARMv8 Crypto Extensions on ARM64)

## License

[Add your license here]

## Support

[Add support information here]
