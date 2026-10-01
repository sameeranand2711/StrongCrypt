# Agent State — StrongCrypt

## Repository

- `VERIFIED`: Repository inspected. It contained only the governing pack documents (no pre-existing application code), so bootstrap overwrote no user work.
- `VERIFIED`: Git initialized by Agent 01 during `SC-T00`. Baseline commit on `main`; all V1 work on `feature/strongcrypt-v1`.
- `VERIFIED`: SDK `10.0.101` present; `global.json` pins it with `latestFeature` roll-forward.
- Layout: `StrongCrypt.sln`; `src/{StrongCrypt.Protocol,StrongCrypt.Encryption,StrongCrypt.Decryption}`; `tests/{StrongCrypt.Protocol.Tests,StrongCrypt.Encryption.Tests,StrongCrypt.Decryption.Tests,StrongCrypt.Integration.Tests}`.
- Build config: `Directory.Build.props` (root/src/tests) + `Directory.Packages.props` central package management. `net10.0`, nullable enabled, implicit usings disabled, `TreatWarningsAsErrors`, .NET analyzers at `latest-Recommended`.
- Remote push/merge/publish authority: **NOT GRANTED**.

## Current execution

- Active task: `SC-T05` (cross-package interoperability and compatibility fixtures).
- Status: READY.
- Agent 02 complete: StrongCrypt.Decryption production code and 42 passing tests. All acceptance criteria met.
- Continuous execution: **ENABLED**. Do not pause between approved tasks.
- Continuous execution: **ENABLED**. Do not pause between approved tasks.

## Durable decisions

- `DECISION`: Encryption and Decryption are physically separate production packages and must never reference each other.
- `DECISION`: Shared `StrongCrypt.Protocol` is capability-neutral and minimal.
- `DECISION`: V1 cryptographic profile is AES-256-GCM only: 32-byte key, 12-byte nonce, 16-byte tag.
- `DECISION`: Nonces are generated internally using cryptographically secure randomness; no caller nonce in the normal public API.
- `DECISION`: V1 uses .NET 10 and BCL cryptography only for production runtime.
- `DECISION`: Password encryption, streaming, KMS/HSM, extra algorithms, re-encryption, asymmetric crypto, signing, publishing, and deployment are deferred.
- `DECISION`: xUnit/test SDK are approved test dependencies.
- `DECISION`: local feature-branch work, build/test, and local task commits are authorized; remote/release actions are not.

## Security review policy

- All HIGH-risk design/implementation tasks require Agent 03 independent targeted review.
- Maximum review loop: one review -> one remediation -> targeted verification.
- Maximum blocker debugging: two materially different failed attempts.

## Completed tasks

- `SC-T00` — Repository bootstrap (Agent 01, MEDIUM risk, no independent review required). Skeleton compiles; package boundaries asserted by tests.
- `SC-T03` — Implementation: StrongCrypt.Protocol, StrongCrypt.Encryption, StrongCrypt.Decryption (Agent 01, HIGH risk, Agent 03 review complete). 44 tests passing.
- `SC-T05` — Cross-package interoperability tests (Agent 02, MEDIUM risk, no independent review required). 20 integration tests + compatibility fixtures. Full suite: 64 tests passing.

## Open blockers

- None.

## Scope changes

- None.

## Last validation

- Full test suite: 64/64 tests passing (Protocol 8, Encryption 17, Decryption 19, Integration 20).
- Boundary evidence: Encryption and Decryption each reference `StrongCrypt.Protocol` only; neither references the other; all three production projects declare zero NuGet `PackageReference` entries.
- Compatibility fixtures: 5 canonical test vectors in `tests/fixtures/v1-compatibility-fixtures.json` with cryptographically generated envelopes.

## Resume rule

On continuation: inspect Git first, then guardrails, this state, active task, relevant SPEC sections, assigned role, and active-task files. Do not reread the whole repository or `agent_logs/` by default.
