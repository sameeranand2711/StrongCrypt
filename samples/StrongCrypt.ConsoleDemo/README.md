# StrongCrypt Console Demo

Minimal console application demonstrating encryption/decryption round-trip using the StrongCrypt V1 DI extensions.

## ⚠️ WARNING: Demo Only

**This demo combines both encryption and decryption capabilities for demonstration purposes only.**

In production services:
- **Encryption services** should reference `StrongCrypt.Encryption` only
- **Decryption services** should reference `StrongCrypt.Decryption` only
- **Never combine both packages** in the same service boundary

The physical separation of encryption and decryption capabilities is a core security principle of StrongCrypt V1.

## Running the Demo

```bash
cd samples/StrongCrypt.ConsoleDemo
dotnet run
```

Expected output:
```
StrongCrypt V1 Console Demo
============================

WARNING: Demo-only. Production services should reference
         only Encryption OR Decryption, never both.

Plaintext:    Hello, StrongCrypt V1!
Key ID:       demo-key-001
External AAD: demo-context

Encrypted:    [N] bytes

Decrypted:    Hello, StrongCrypt V1!
Recovered Key ID: demo-key-001

PASS: Round-trip successful
```

## What This Demonstrates

1. **DI Registration**: Both `AddStrongCryptEncryption` and `AddStrongCryptDecryption` with a custom key provider
2. **Ephemeral Key**: 32-byte AES-256 key generated at runtime and zeroed after use
3. **Key Provider Pattern**: `DemoKeyProvider` implementing both `IEncryptionKeyProvider` and `IDecryptionKeyProvider`
4. **Round-trip**: Encrypt plaintext with key ID and external AAD, then decrypt and verify
5. **Secure Disposal**: Key material zeroed via `CryptographicOperations.ZeroMemory`

## Production Usage

See the main repository documentation for production patterns:
- Use `IEncryptionKeyProvider` or `IDecryptionKeyProvider` (not both)
- Implement proper key management (Azure Key Vault, AWS KMS, etc.)
- Deploy encryption and decryption to separate services
- Never log or persist key material
