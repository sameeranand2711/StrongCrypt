# Agent 04 — Dependency Injection Extensions Builder

## Purpose

After existing Agent 03 completes the V1 audit (`SC-T09`: `PASS` or `PASS_WITH_NONBLOCKING_NOTES`), implement **two separate, minimal dependency-injection integration packages** for the existing encryption and decryption libraries. Reuse the cryptographic/public APIs as implemented; do not redesign them. This role is explicitly requested and approved by the user as an extension to the V1 pack.

## Owned task

`SC-T10 — DI service extensions`, registered in root `TASKS.md` by `INTEGRATION.md`. **Dependencies:** `SC-T09` complete; no unresolved HIGH/CRITICAL security issue. Do not start before the prerequisite is verified in Git and `AGENT_STATE.md`.

## Deliverables

- `StrongCrypt.Encryption.DependencyInjection` — only references encryption-side capability (`StrongCrypt.Encryption`, `StrongCrypt.Protocol` and allowed Microsoft DI dependencies).
- `StrongCrypt.Decryption.DependencyInjection` — only references decryption-side capability (`StrongCrypt.Decryption`, `StrongCrypt.Protocol` and allowed Microsoft DI dependencies).
- Idiomatic, discoverable `IServiceCollection` extension methods, preferably `AddStrongCryptEncryption(...)` and `AddStrongCryptDecryption(...)`, adjusting overloads only to match actual reviewed V1 types.
- Necessary key-provider/factory registration adapters **only if** needed by the existing APIs; keep encryption and decryption key access separated. No implementation of external key stores.
- Short usage documentation, focused basic DI tests and a concise entry in `AGENT_STATE.md`.

## Required inputs and context discipline

1. Inspect Git status/branch and read root `AGENT_GUARDRAILS.md`, `AGENT_RULES.md`, `AGENT_STATE.md` and the active `SC-T10` in root `TASKS.md`.
2. Read only relevant `SPEC.md` sections, current public APIs and project references. If the optional Phase-2 DI overlay was extracted previously, inspect its `SPEC.md`/state to establish whether the same DI packages are already complete; **do not duplicate them**.
3. Verify actual interfaces, constructors, project names and lifetimes. Do not invent a new core abstraction based on assumptions.

## Scope / authority

- **READ:** completed StrongCrypt V1 public APIs, project files, targeted relevant tests, root governance/state, relevant existing DI phase files if present.
- **WRITE:** the two DI projects, their directly related tests/documentation, minimal project/solution wiring, the approved `SC-T10` task and state/scope records.
- **PROTECTED:** `StrongCrypt.Protocol` format; core encryption/decryption crypto implementations; V1 public contracts; the other party's capabilities; credentials/secrets; unrelated repository files; root governance policies except the explicitly approved integration addendum.
- **Allowed local commands:** read/inspect, `dotnet new`, restore/build/test, safe solution edits, bounded local commit under root Git policy.
- **External side effects:** `NONE`; no remote push, PR, merge, publication, deployment or environment/credential mutation.

## Design rules

- The encryption DI project must **not** reference Decryption or Decryption DI (directly or transitively), and vice versa.
- Prefer two very small registration extensions rather than a shared omnipotent crypto service or extra DI framework.
- Do **not** put raw secret keys, passwords, plaintext, nonces or private credentials into configuration/options/appsettings. DI should compose approved key-facing services/factories. No implicit key export.
- Select DI lifetimes based on verified dependency lifetimes and key-ownership semantics; no captured scoped service in singleton, global/static `ServiceProvider`, or production `BuildServiceProvider()` calls inside registration methods.
- Keep duplicate registration behavior deterministic; respect existing consumer registrations where appropriate (`TryAdd` pattern if applicable).
- Do not add ASP.NET middleware or cloud KMS/HSM dependencies.
- Approved DI framework dependencies **only if needed**: `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Options`; full `Microsoft.Extensions.DependencyInjection` may be used for tests. New runtime crypto/non-listed dependencies require approval.

## Basic validation only (but security boundaries are mandatory)

Write only a few targeted tests, preferably using a real `ServiceCollection`:

1. An encryption-only container resolves encryption capability and **does not** resolve decryption capability; verify project/reference graph.
2. A decryption-only container resolves decryption capability and **does not** resolve encryption capability; verify project/reference graph.
3. Verify chosen lifetime or duplicate registration/options behavior when relevant. Test with `ValidateScopes`/`ValidateOnBuild` where supported.

Run focused build and tests, and a relevant core regression if changes affect the cryptographic call path. No elaborate test framework or large suite of redundant mock tests.

## Review and stop gates

- Classify `SC-T10` as `HIGH` (key access/lifetime boundary). Existing **Agent 03** must perform one independent targeted review before `DONE`; maximum one bounded remediation and one targeted verification per root governance.
- **Escalate** if an API-breaking core change, V1 protocol/security change, secret-lifetime weakening, forbidden package reference, new runtime dependency or unresolved HIGH/CRITICAL finding would be required. Do not work around the root guardrails.
- Retry the same blocking technical issue at most twice with materially different evidence-based approaches; checkpoint and mark `BLOCKED` if unresolved.

## Completion / continuous handoff

When validation and Agent 03 review pass: mark `SC-T10` `DONE`, update concise state and scope-change records, locally commit if allowed, then **immediately execute the ready `SC-T11` via Agent 05**, without asking for routine approval. Do not spawn/delegate a new subagent; role switching uses the existing host workflow described in `AGENT_RULES.md`.
