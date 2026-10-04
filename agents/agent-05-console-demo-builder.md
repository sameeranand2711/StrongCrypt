# Agent 05 — Minimal Console Demonstration Builder

## Purpose

After `SC-T10` (Agent 04) completes and passes its required review, add one **small console-based .NET application** showing a successful StrongCrypt encrypt/decrypt round trip. This is a demonstration, **not** a production template or new cryptography layer.

## Owned task

`SC-T11 — Console demo`, registered in root `TASKS.md` by `INTEGRATION.md`.
**Dependencies:** `SC-T10` `DONE` and `SC-T09` V1 audit `PASS`/`PASS_WITH_NONBLOCKING_NOTES` with no blocking finding.

## Deliverables

- One small `samples/StrongCrypt.ConsoleDemo/` console project, using the current repository target-framework and naming conventions.
- A single short, readable `Program.cs` with a deterministic explanation and a **working** end-to-end flow:
  1. Create a **demo-only, ephemeral random** key using the existing V1 supported key creation/ownership mechanism (never hard-code a fixed production-like secret).
  2. Configure a simple in-memory demo key provider or existing V1 DI-compatible registration/factory; retain correct key association for both operations.
  3. Register `AddStrongCryptEncryption(...)` and `AddStrongCryptDecryption(...)` via the separate DI extension packages.
  4. Encrypt a harmless fixed sample string, optionally with non-secret associated data.
  5. Decrypt the envelope, verify an exact round-trip match, and print a clear **PASS/FAIL** result. It may print synthetic demo text but never actual secret keys or production payloads.
- A short `README.md` in the sample directory (or a few comments) with one command such as `dotnet run --project samples/StrongCrypt.ConsoleDemo` and a warning that combined capabilities are **demo-only**; production services should install only the package they need.

## Required inputs / context

1. Inspect Git status/branch; read root `AGENT_GUARDRAILS.md`, `AGENT_RULES.md`, `AGENT_STATE.md` and active `SC-T11`.
2. Inspect only the verified **current** DI and core public APIs, sample/project conventions, relevant key-provider lifetime rules, and references.
3. Reuse actual existing types. Do not create a second implementation of encryption, decryption, envelope parsing, nonce handling or key store logic.

## Scope / authority

- **READ:** core public APIs, DI extensions, existing examples/tests, repository/phase state, directly relevant project files.
- **WRITE:** `samples/StrongCrypt.ConsoleDemo/**`, minimal solution/project wiring, short directly related usage documentation, task/state records.
- **PROTECTED:** both crypto core packages and protocol, DI implementations, frozen public contracts/security invariants, unrelated files, secrets and deployment infrastructure.
- **Allowed local commands:** inspect, `dotnet new console`, restore/build/run, safe project references, bounded local commit under root Git policy.
- **External side effects:** `NONE`; no remote push, PR, merge, NuGet publish, credentials or production access.

## Keep it intentionally simple

- No web app, CLI argument framework, API host, database, file storage, cloud provider, advanced configuration, benchmarking, abstractions beyond what the existing APIs require, or interactive prompts.
- No production configuration key embedding; do **not** use environment/appsettings raw keys.
- No new cryptography package or algorithm. Keep key disposal/zeroization consistent with existing V1 ownership contracts.
- Sample combines both capabilities only to demonstrate interoperability; **never** alter the separation between the actual libraries or DI packages.

## Validation and review

- Required: `dotnet build` for the sample and a successful `dotnet run` showing the expected `PASS` result with no leaked key material; run the smallest relevant existing regression if needed.
- Extra automated tests are **not required**: the demo's self-check and build/run smoke validation are enough unless an actual defect or repository convention requires a basic smoke test.
- Have existing Agent 03 perform a **targeted security review** of the example's key handling and references. One review, one remediation, targeted re-verification; no looping.
- The reviewer must not approve claims of production security certification; demo-only disclaimers remain explicit.

## Completion / termination

Record execution evidence, mark `SC-T11` `DONE`, update `AGENT_STATE.md` and `SCOPE_CHANGES.md` if applicable, locally commit under root policy, and end with `ALL_APPROVED_WORK_COMPLETE`. Do **not** publish/release/merge. If blocked by security/contract/scope changes, checkpoint and request the specific important approval; otherwise do not ask to continue.
