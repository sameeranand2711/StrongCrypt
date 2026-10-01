# Agent State — StrongCrypt

## Repository

- `VERIFIED`: Repository inspected. It contained only the governing pack documents (no pre-existing application code), so bootstrap overwrote no user work.
- `VERIFIED`: Git initialized by Agent 01 during `SC-T00`. Baseline commit on `main`; all V1 work on `feature/strongcrypt-v1`.
- `VERIFIED`: SDK `10.0.101` present; `global.json` pins it with `latestFeature` roll-forward.
- Layout: `StrongCrypt.sln`; `src/{StrongCrypt.Protocol,StrongCrypt.Encryption,StrongCrypt.Decryption}`; `tests/{StrongCrypt.Protocol.Tests,StrongCrypt.Encryption.Tests,StrongCrypt.Decryption.Tests}`.
- Build config: `Directory.Build.props` (root/src/tests) + `Directory.Packages.props` central package management. `net10.0`, nullable enabled, implicit usings disabled, `TreatWarningsAsErrors`, .NET analyzers at `latest-Recommended`.
- Remote push/merge/publish authority: **NOT GRANTED**.

## Current execution

- Active task: `SC-T03` (implementation: StrongCrypt.Protocol, StrongCrypt.Encryption).
- Status: READY.
- Agent 01 complete: `docs/PROTOCOL_V1.md` (wire format, AAD framing, parsing rules, public API contracts, buffer sizing, security considerations) and `tests/fixtures/v1-test-vectors.json` (6 canonical vectors + 11 hostile-input cases). Ready for Agent 03 independent review.
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

## Open blockers

- None.

## Scope changes

- None.

## Last validation

- `SC-T00`: `dotnet restore` OK; `dotnet build` succeeded with 0 warnings / 0 errors; `dotnet test` 12/12 passed (Protocol 8, Encryption 2, Decryption 2).
- Boundary evidence: Encryption and Decryption each reference `StrongCrypt.Protocol` only; neither references the other (asserted at both project-file and compiled-assembly level); all three production projects declare zero NuGet `PackageReference` entries.

## Resume rule

On continuation: inspect Git first, then guardrails, this state, active task, relevant SPEC sections, assigned role, and active-task files. Do not reread the whole repository or `agent_logs/` by default.
