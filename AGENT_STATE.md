# Agent State — StrongCrypt

## Repository

- `VERIFIED`: Repository inspected. It contained only the governing pack documents (no pre-existing application code), so bootstrap overwrote no user work.
- `VERIFIED`: Git initialized by Agent 01 during `SC-T00`. Baseline commit on `main`; all V1 work on `feature/strongcrypt-v1`.
- `VERIFIED`: SDK `10.0.101` present; `global.json` pins it with `latestFeature` roll-forward.
- Layout: `StrongCrypt.sln`; `src/{StrongCrypt.Protocol,StrongCrypt.Encryption,StrongCrypt.Decryption}`; `tests/{StrongCrypt.Protocol.Tests,StrongCrypt.Encryption.Tests,StrongCrypt.Decryption.Tests,StrongCrypt.Integration.Tests}`.
- Build config: `Directory.Build.props` (root/src/tests) + `Directory.Packages.props` central package management. `net10.0`, nullable enabled, implicit usings disabled, `TreatWarningsAsErrors`, .NET analyzers at `latest-Recommended`.
- Remote push/merge/publish authority: **NOT GRANTED**.

## Current execution

- Active task: `SC-T06` (production readiness sweep and API polish).
- Status: READY.
- Agent 02 complete: SC-T05 interoperability tests, NIST SP 800-38D vectors (5), compatibility fixtures (5), 92 tests passing.
- Continuous execution: **ENABLED**. Do not pause between approved tasks.

## Durable decisions

- `DECISION`: Encryption and Decryption are physically separate production packages and must never reference each other.
- `DECISION`: Shared `StrongCrypt.Protocol` is capability-neutral and minimal.
- `DECISION`: V1 cryptographic profile is AES-256-GCM only: 32-byte key, 12-byte nonce, 16-byte tag.
