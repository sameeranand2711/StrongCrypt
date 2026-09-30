# Development Standards — StrongCrypt

## Engineering priorities

1. Security correctness.
2. Readability and explicit invariants.
3. Small public API surface.
4. Simple design with minimal abstraction.
5. Testability and auditability.
6. Reasonable performance and allocations.
7. Extensibility only where the V1 protocol requires it.

## Code style

- Prefer straightforward C# over cleverness.
- Enable nullable reference types and useful analyzers.
- Treat warnings as errors for production projects unless a documented repository constraint prevents it.
- Prefer immutable protocol metadata.
- Keep methods small enough to audit, but do not fragment logic into unnecessary layers.
- Avoid reflection, dynamic dispatch, source generation, unsafe code, and custom memory tricks unless a measured V1 need exists and the task explicitly permits it.

## Cryptography implementation

- Use `System.Security.Cryptography` primitives; do not reimplement primitives.
- Use the `AesGcm` constructor with the explicit required tag size.
- Use `RandomNumberGenerator` for cryptographic randomness.
- Prefer span-based APIs when they improve safety/allocation behavior without making the public API harder to use correctly.
- Zero owned sensitive temporary buffers with `CryptographicOperations.ZeroMemory` when ownership is clear and zeroing is meaningful; do not pretend zeroization guarantees elimination of every managed-memory copy.
- Avoid secret-dependent custom comparisons; use platform cryptographic operations.

## API design

- Make the safe path the easiest path.
- Do not expose configuration that V1 does not safely support.
- Validate arguments early.
- Document buffer sizing and ownership precisely.
- Avoid returning mutable internal buffers.
- Do not expose secrets in `ToString()`, exception text, debugger-friendly projections, or serialization.
- Keep encryption/decryption public types in their respective packages.

## Protocol code

- Encode fields canonically.
- Parse with checked arithmetic.
- Validate before allocating based on untrusted lengths.
- Reject unknown/invalid reserved bits unless the frozen protocol explicitly says otherwise.
- Keep protocol constants centralized in `StrongCrypt.Protocol` without moving capability logic there.
- Retain compatibility fixtures once the V1 protocol freezes.

## Error handling

- Public authentication failures must not reveal whether key, nonce, tag, ciphertext, or AAD was incorrect.
- Malformed-format errors should be deterministic and safe.
- Never swallow cryptographic exceptions to create false success.
- Never include plaintext/key bytes in exceptions.

## Testing

- Favor behavior/security-invariant tests over implementation-detail tests.
- Include official known-answer vectors and document their source.
- Include deterministic mutation tests for every envelope region.
- Tests must verify package dependency separation.
- Tests that generate random payloads should report reproducible seeds on failure where practical, but never serialize secret production data.
- Do not add broad snapshot tests of ciphertext if they would freeze non-deterministic nonce output; freeze protocol fixtures intentionally instead.

## Performance

- Do not optimize cryptographic code before correctness and auditability.
- Measure before adding pooling, unsafe code, or complex buffer ownership.
- Avoid avoidable whole-payload copies when a clear span/destination API can prevent them.
- Never trade authentication, validation, or secret handling for throughput.

## Dependencies

- V1 production runtime should remain BCL-only.
- xUnit/test SDK are approved test dependencies.
- Any additional runtime crypto/native dependency requires human approval.
- Additional dev/test packages require a written justification in the active task and should be avoided when the BCL/test framework suffices.

## Documentation

Every public API must document:

- security purpose;
- input/key requirements;
- AAD semantics where applicable;
- ownership/lifetime expectations;
- failure behavior;
- protocol compatibility where relevant.

Do not use unverifiable security marketing language.

## General principles

Apply KISS, YAGNI, DRY, and SOLID as guidance, not dogma. Prefer the smallest design that preserves security invariants and clear package boundaries.
