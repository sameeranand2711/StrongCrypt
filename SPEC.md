# StrongCrypt V1 — Approved Specification

## Status

This document is the durable project contract for V1. It is intentionally narrow. Agents may refine implementation details inside these boundaries without asking for routine approval. Any change that crosses an approval gate in `AGENT_GUARDRAILS.md` must stop and request human approval.

## Objective

Build a production-quality .NET cryptographic data-protection library with a deliberately small, misuse-resistant public API and a hard package-level separation between encryption capability and decryption capability.

The project does **not** create new cryptographic algorithms. It composes vetted platform cryptographic primitives and focuses on safe API design, authenticated envelopes, hostile-input handling, testability, auditability, and least privilege.

## Runtime and language

- Target: `.NET 10` / `net10.0`.
- Language: C# with nullable reference types enabled.
- Production cryptography: `System.Security.Cryptography` platform APIs only in V1.
- No third-party runtime cryptography dependency without explicit human approval.
- Test framework: xUnit is approved for V1; avoid extra assertion/mocking packages unless justified.

## Hard architecture boundary

V1 consists of exactly three production packages:

```text
StrongCrypt.Protocol
StrongCrypt.Encryption
StrongCrypt.Decryption
```

Dependency direction:

```text
StrongCrypt.Protocol
      ▲       ▲
      │       │
Encryption   Decryption
```

Forbidden:

```text
StrongCrypt.Encryption -> StrongCrypt.Decryption
StrongCrypt.Decryption -> StrongCrypt.Encryption
```

`StrongCrypt.Protocol` must remain small and capability-neutral. It may define wire-format identifiers, version constants, immutable non-secret protocol metadata, and shared validation constants. It must not contain encryption operations, decryption operations, key-access implementation, or hostile-input parsing that would effectively give one capability package the other's behavior.

The encryption package owns encoding/writing of V1 envelopes. The decryption package owns parsing/validation of untrusted V1 envelopes.

## V1 cryptographic profile

V1 implements one profile only:

- AEAD primitive: AES-256-GCM.
- Key size: 256 bits / 32 bytes.
- Nonce size: 96 bits / 12 bytes.
- Authentication tag: 128 bits / 16 bytes.
- Nonce generation: cryptographically secure randomness from `RandomNumberGenerator`; caller-controlled nonces are not part of the normal public API.
- Associated data: supported.
- Header fields that affect interpretation or key selection must be cryptographically bound as authenticated data.
- Callers must not select cipher modes, padding, tag length, nonce length, or RNG through the normal API.
- The implementation must use the .NET `AesGcm` constructor that requires an explicit tag size.

Do not expose AES-CBC, AES-ECB, raw CTR, DES/3DES/RC4, unauthenticated encryption, or custom ciphers.

### GCM usage limit discipline

Nonce uniqueness under a key is security-critical. Random 96-bit nonces reduce operational complexity but do not make a key safe for unlimited encryptions. V1 documentation must clearly state that keys have a finite operational lifetime and that deployments with very high per-key encryption volume require an explicit key-usage policy. Agents must not invent a misleading "unlimited" safety claim.

A concrete automated usage-counter policy is **not** part of V1 unless later approved.

## Envelope protocol requirements

The exact V1 binary layout is established in task `SC-T02` and becomes frozen after its independent review passes. It must include enough information to safely identify the protocol and resolve the decryption key without giving the encryption package decryption capability.

Required semantic fields:

- fixed magic bytes;
- format version;
- algorithm/profile identifier;
- key identifier or key-reference identifier;
- flags/reserved field with strict handling;
- nonce;
- ciphertext length or canonical framing sufficient for deterministic parsing;
- ciphertext;
- authentication tag.

Protocol rules:

1. Use a canonical byte order and exact field widths.
2. Reject unsupported versions, algorithms, invalid reserved bits, impossible lengths, truncation, trailing bytes when disallowed, and non-canonical encodings.
3. Header interpretation fields and key identifier must be authenticated.
4. External/user AAD must be unambiguously framed with protocol-authenticated data so concatenation cannot create ambiguity.
5. The protocol must be forward-versionable but V1 decryption must fail closed on unknown versions/profiles.
6. Parsing attacker-controlled lengths must not cause unbounded allocation or integer overflow.
7. Protocol changes after freeze require human approval.

## Encryption library responsibilities

`StrongCrypt.Encryption` must:

- encrypt plaintext using the fixed V1 profile;
- generate nonces internally;
- support optional caller-supplied associated data;
- produce the canonical V1 envelope;
- validate key size and key identifier constraints before encryption;
- provide allocation-conscious `ReadOnlySpan<byte>` / `Span<byte>` APIs where practical plus a safe convenience API;
- avoid logging plaintext, keys, nonces with keys, or other secret material;
- zero owned temporary key/plaintext buffers when doing so materially reduces exposure and ownership is clear;
- never reference the decryption package.

The normal public API must not permit caller-supplied nonce, cipher mode, padding, tag size, or algorithm/profile selection.

## Decryption library responsibilities

`StrongCrypt.Decryption` must treat every envelope as hostile input. It must:

- parse the V1 envelope independently of the encryption package;
- validate all lengths, fields, reserved bits, version/profile, and canonical framing before dangerous allocation/use;
- resolve or accept the required 256-bit key through a decryption-specific API;
- authenticate the protocol header plus external AAD;
- release plaintext only after successful AEAD authentication;
- return generic authentication failure semantics that do not distinguish wrong key from modified ciphertext/tag/AAD;
- avoid secret-bearing logs;
- never reference the encryption package.

Malformed-format errors may be distinguished from authentication failure when useful, but authentication failures must not leak whether key, ciphertext, tag, nonce, or AAD was wrong.

## Key handling boundary

V1 is a cryptographic data-protection library, not a KMS/HSM implementation.

- Raw 32-byte key material may enter V1 through tightly scoped APIs.
- Key identifiers are non-secret and are part of the envelope protocol.
- Encryption and decryption must use separate key-facing abstractions/types where abstractions are introduced.
- No API may expose key material via `ToString()`, logging, exceptions, diagnostics, or serialization.
- No real credentials or production keys may be used in tests.

KMS/HSM/Key Vault integration, envelope-encryption DEKs/KEKs, and remote key operations are future scope.

## Public API principles

- Safe defaults are mandatory; dangerous knobs are omitted rather than documented as "advanced" in V1.
- Prefer explicit typed results over bags of `byte[]` where this materially prevents misuse.
- Avoid abstraction for abstraction's sake.
- Public types and exceptions must be documented.
- No public API returns unauthenticated plaintext.
- Public APIs must be concurrency-safe when documented as stateless; otherwise document ownership/lifetime clearly.
- Breaking the reviewed V1 public contract after `SC-T02` requires human approval.

## Security invariants

The implementation must preserve all of the following:

1. No custom cipher, mode, GHASH, or AES implementation.
2. Same key + same nonce reuse must never be intentionally possible through the normal API.
3. Authentication precedes plaintext release.
4. Any modification to authenticated header, nonce, ciphertext, tag, or supplied AAD causes failure.
5. Unsupported protocol data fails closed.
6. Attacker-controlled lengths cannot drive unchecked allocation or arithmetic overflow.
7. Encryption-only consumers do not gain a reference to decryption implementation.
8. Decryption-only consumers do not gain a reference to encryption implementation.
9. No secret appears in logs, exception messages, test snapshots, committed fixtures, or generated agent logs.
10. Tests may not be weakened to make a security check pass.

## V1 tests

At minimum, tests must cover:

- encrypt -> decrypt round trip;
- empty, one-byte, binary, and representative multi-size payloads;
- AAD present/absent;
- repeated encryption of the same plaintext produces different envelopes;
- wrong key fails;
- modified header fails or is rejected;
- modified nonce fails;
- modified ciphertext fails;
- modified tag fails;
- modified AAD fails;
- truncated envelope for every structural region;
- unknown version/profile;
- malformed/overflowing length fields;
- non-canonical encoding/reserved flags;
- parallel calls where public API claims concurrency safety;
- official/recognized AES-GCM known-answer vectors with source documented;
- cross-package interoperability without implementation dependency;
- mutation/adversarial tests over envelope bytes;
- parser does not allocate from an untrusted declared size before verifying bounds.

## V1 non-goals / deferred scope

Do not implement these unless a human explicitly approves a scope change:

- password-based encryption or Argon2id/PBKDF APIs;
- ChaCha20-Poly1305 or additional algorithms;
- streaming/chunked file encryption;
- KMS, HSM, cloud Key Vault, AWS KMS, GCP KMS integration;
- envelope encryption with DEK/KEK wrapping;
- re-encryption/rotation service;
- asymmetric encryption;
- signing or certificates;
- tokenization;
- database integration;
- ASP.NET integration;
- CLI/UI;
- NuGet publishing or release automation;
- FIPS certification/compliance claims;
- "military-grade", "unbreakable", "zero-risk", or similar marketing claims.

## Compatibility and release policy

V1 protocol compatibility matters as soon as a reviewed protocol fixture is committed. After protocol freeze:

- encryption must continue producing canonical V1 payloads;
- decryption must continue reading canonical V1 payloads;
- breaking wire-format or public-contract changes require human approval;
- compatibility fixtures must be retained permanently unless a human explicitly approves removal.

No agent may publish packages, push a release tag, merge to a protected branch, or deploy anything without explicit human approval.

## Current external security references

Agents may use newer official guidance if it does not conflict with this approved contract. If newer guidance implies this specification is unsafe or materially obsolete, stop and request a specification decision rather than silently changing cryptographic behavior.

- NIST SP 800-38D, GCM/GMAC recommendation. NIST is actively revising it as of 2026; the published recommendation favors 96-bit IVs for interoperability, efficiency, and simplicity.
- NIST 2024/2026 revision notices: revision work is ongoing; do not implement draft/wGCM/Rijndael-256 proposals in V1.
- Microsoft .NET cryptography documentation: `AesGcm` supports 12-byte nonces; .NET 8+ recommends constructing `AesGcm` with an explicit required tag size.
- OWASP Cryptographic Storage Cheat Sheet: prefer standard, authenticated modes and do not invent custom algorithms.

Reference URLs:

- https://csrc.nist.gov/pubs/sp/800/38/d/final
- https://csrc.nist.gov/pubs/sp/800/38/d/r1/2prd
- https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm
- https://learn.microsoft.com/en-us/dotnet/standard/security/cross-platform-cryptography
- https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html
