# Agent State — StrongCrypt

## Repository

- `ASSUMPTION`: Repository contents have not yet been inspected by the executing agent.
- Intended feature branch when no project policy exists: `feature/strongcrypt-v1`.
- Remote push/merge/publish authority: **NOT GRANTED**.

## Current execution

- Active task: `SC-T00`.
- Status: `READY`.
- Next approved action: inspect Git/repository state, then bootstrap only what is missing.
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

- None.

## Open blockers

- None known before repository inspection.

## Scope changes

- None.

## Last validation

- None; execution has not started.

## Resume rule

On continuation: inspect Git first, then guardrails, this state, active task, relevant SPEC sections, assigned role, and active-task files. Do not reread the whole repository or `agent_logs/` by default.
