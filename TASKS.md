# StrongCrypt V1 Task Plan

Statuses: `PENDING | READY | IN_PROGRESS | REVIEW | REMEDIATION | DONE | BLOCKED | HUMAN_APPROVAL_REQUIRED`

All external side-effect authority is `NONE` unless explicitly stated. Local repository modifications, build/test commands, feature-branch creation, and local commits are governed by `AGENT_GUARDRAILS.md` and are allowed where scoped.

---

## SC-T00 — Repository bootstrap

- **Owner:** Agent 01
- **Risk:** MEDIUM
- **Status:** DONE
- **Dependencies:** none
- **Objective:** Establish the minimal .NET 10 solution/project/test skeleton and safe Git hygiene without overwriting existing user work.
- **In scope:** Inspect current repo; initialize Git only if absent; feature branch if appropriate; solution; three production projects; test projects; nullable/analyzers/warnings policy; `.gitignore` additions; package-reference boundary checks.
- **Exclusions:** Crypto implementation, protocol implementation, publishing, remote actions.
- **Expected outputs:** Compilable skeleton for `StrongCrypt.Protocol`, `StrongCrypt.Encryption`, `StrongCrypt.Decryption`, and focused test projects.
- **Acceptance criteria:** Projects target `net10.0`; Encryption and Decryption each reference Protocol but not each other; BCL-only production dependencies; build passes; existing work preserved.
- **Validation:** `dotnet restore`, `dotnet build`; project-reference inspection.
- **READ:** repository root and existing build/config files.
- **WRITE:** solution/project files, `src/**`, `tests/**`, `.gitignore`, scoped build props.
- **PROTECTED:** unrelated existing application code; remote repository; production/config secrets.
- **Done checklist:** behavior/output complete; acceptance met; scope respected; build passed; state updated; local commit when safe.
- **Completion note:** Git initialized (`main` baseline, work on `feature/strongcrypt-v1`); `StrongCrypt.sln` with 3 production + 3 test projects on `net10.0`; central package management with test-only dependencies; `TreatWarningsAsErrors` + .NET analyzers; boundary tests assert Encryption/Decryption reference Protocol only and never each other, and that production projects declare zero NuGet references. `dotnet build` clean (0 warnings), `dotnet test` 12/12 passed.

---

## SC-T01 — Threat model and security invariants

- **Owner:** Agent 01
- **Reviewer:** Agent 03
- **Risk:** HIGH
- **Status:** READY
- **Dependencies:** SC-T00
- **Objective:** Convert `SPEC.md` security requirements into a compact implementation threat model and testable invariants without expanding V1 scope.
- **In scope:** attacker capabilities; assets; trust boundaries; misuse cases; nonce/key/AAD risks; hostile payload parsing; error-oracle risks; package capability separation; explicit non-goals.
- **Exclusions:** new algorithms, KMS/HSM, password crypto, streaming.
- **Expected outputs:** `docs/THREAT_MODEL.md` plus precise invariant/test mapping.
- **Acceptance criteria:** every V1 security invariant maps to at least one implementation/test responsibility; no unsupported security claims; no custom cryptography proposed.
- **Validation:** targeted document consistency review against `SPEC.md`.
- **READ:** governing pack + project skeleton.
- **WRITE:** `docs/THREAT_MODEL.md`, task/state files.
- **PROTECTED:** `SPEC.md` cryptographic contract unless human approves change.
- **Review gate:** Agent 03 independent targeted review required.
- **Done checklist:** output complete; review passed; any findings remediated once and verified; state/commit updated.

---

## SC-T02 — Freeze V1 envelope and public API contract

- **Owner:** Agent 01
- **Reviewer:** Agent 03
- **Risk:** HIGH
- **Status:** PENDING
- **Dependencies:** SC-T01
- **Objective:** Define the exact canonical V1 binary envelope and minimal public API shapes before implementation.
- **In scope:** byte layout; byte order; field widths; key-id limits; flags; length rules; authenticated-header framing; external AAD framing; format/authentication error taxonomy; encryption API; decryption API; buffer-sizing contract; compatibility fixture strategy.
- **Exclusions:** additional algorithms, streaming, KMS/HSM, caller nonce controls.
- **Expected outputs:** `docs/PROTOCOL_V1.md`; public API signatures/stubs if useful; canonical test fixtures/spec vectors.
- **Acceptance criteria:** deterministic parse/write rules; no ambiguous concatenation; all interpretation/key-selection fields authenticated; decryption can parse without referencing Encryption; Encryption cannot decrypt; hostile length handling specified; public API has no dangerous knobs.
- **Validation:** independent protocol/security review; compile API stubs if created.
- **READ:** SPEC, threat model, project skeleton, relevant official cryptography docs.
- **WRITE:** `docs/PROTOCOL_V1.md`, protocol constants/types, API stubs/contracts, fixtures, task/state files.
- **PROTECTED:** algorithm/key/nonce/tag profile; package separation.
- **Freeze rule:** once Agent 03 passes this task, wire-format semantics and reviewed public API become frozen; later breaking changes require human approval.
- **Done checklist:** contract exact; review passed; compatibility fixtures retained; state/commit updated.

---

## SC-T03 — Encryption implementation

- **Owner:** Agent 01
- **Reviewer:** Agent 03
- **Risk:** HIGH
- **Status:** PENDING
- **Dependencies:** SC-T02
- **Objective:** Implement safe AES-256-GCM encryption and canonical V1 envelope writing.
- **In scope:** key validation; CSPRNG nonce generation; explicit 16-byte GCM tag; AAD/header binding; envelope serialization; span/destination API where justified; convenience API; focused unit tests; safe exceptions and zeroization of clearly owned temporary sensitive buffers.
- **Exclusions:** decryption, caller nonce API, additional algorithms, password APIs, KMS.
- **Expected outputs:** production encryption code and tests.
- **Acceptance criteria:** only platform `AesGcm`; no decryption dependency; caller cannot select nonce/tag/mode/padding/profile; same plaintext/key normally produces different envelope due to nonce; header/AAD contract implemented exactly; no secret logging.
- **Validation:** encryption unit tests; build; dependency check; targeted static/analyzer checks.
- **READ:** encryption source, Protocol contract, relevant tests/docs.
- **WRITE:** `src/StrongCrypt.Encryption/**`, its tests, directly required Protocol constants/types already authorized by SC-T02, task/state files.
- **PROTECTED:** Decryption implementation; frozen protocol semantics.
- **Review gate:** Agent 03 independent targeted review required.
- **Done checklist:** tests pass; review passes; one bounded remediation maximum; state/commit updated.

---

## SC-T04 — Decryption and hostile parser implementation

- **Owner:** Agent 02
- **Reviewer:** Agent 03
- **Risk:** HIGH
- **Status:** PENDING
- **Dependencies:** SC-T03
- **Objective:** Implement independent hostile-input parsing, AES-256-GCM authentication/decryption, and fail-closed plaintext release.
- **In scope:** envelope parser; checked arithmetic; canonical validation; unsupported version/profile handling; key-id extraction; AAD/header reconstruction; authentication; generic auth failure semantics; span/destination API where justified; malformed-input tests.
- **Exclusions:** referencing Encryption package; reusing Encryption serializer/parser internals; changing frozen protocol; additional algorithms.
- **Expected outputs:** production decryption code and tests.
- **Acceptance criteria:** Decryption references Protocol only; no plaintext on auth failure; wrong key/ciphertext/tag/AAD share non-oracular auth failure behavior; length parsing cannot cause unchecked allocation/overflow; unknown/non-canonical format fails closed.
- **Validation:** decryption/parser tests; build; dependency check; analyzer checks.
- **READ:** frozen protocol docs/constants, Decryption project, relevant fixtures/tests.
- **WRITE:** `src/StrongCrypt.Decryption/**`, its tests, task/state files.
- **PROTECTED:** Encryption implementation; frozen protocol semantics.
- **Review gate:** Agent 03 independent targeted review required.
- **Done checklist:** tests pass; review passes; one bounded remediation maximum; state/commit updated.

---

## SC-T05 — Cross-package interoperability and compatibility fixtures

- **Owner:** Agent 02
- **Reviewer:** Agent 03
- **Risk:** HIGH
- **Status:** PENDING
- **Dependencies:** SC-T04
- **Objective:** Prove Encryption-produced V1 payloads are independently consumable by Decryption without implementation coupling and freeze durable compatibility fixtures.
- **In scope:** round-trip integration tests; AAD tests; canonical fixed-key/fixed-input fixture generation through test-only controlled vectors; package-reference assertions; recognized AES-GCM known-answer vectors with documented source.
- **Exclusions:** production nonce injection API; changing protocol to simplify tests.
- **Expected outputs:** integration/compatibility tests and permanent V1 fixtures.
- **Acceptance criteria:** no cross-package production reference; fixtures independently parse/decrypt; official vectors pass; test-only determinism does not leak into production API.
- **Validation:** targeted integration tests plus relevant regression suite.
- **READ:** all three production projects, frozen protocol, test fixtures.
- **WRITE:** integration/compatibility test project/files, fixture docs/data, task/state files.
- **PROTECTED:** frozen public protocol/API.
- **Review gate:** Agent 03 independent targeted review required.
- **Done checklist:** interoperability proven; fixtures committed; review passed; state/commit updated.

---

## SC-T06 — Mutation, truncation, and abuse-resistance tests

- **Owner:** Agent 02
- **Reviewer:** Agent 03
- **Risk:** HIGH
- **Status:** PENDING
- **Dependencies:** SC-T05
- **Objective:** Exercise hostile envelope input systematically without adding a heavy fuzzing dependency.
- **In scope:** byte mutation across envelope regions; systematic truncation; invalid lengths; invalid reserved bits; unknown version/profile; trailing data policy; allocation/bounds assertions where practical; randomized synthetic payload loops with reproducible seeds on failure.
- **Exclusions:** external fuzzing services/frameworks; denial-of-service benchmarking against live systems.
- **Expected outputs:** adversarial test suite with concise invariant mapping.
- **Acceptance criteria:** all authenticated-region mutations fail/reject as specified; parser does not trust hostile lengths; no crashes/undefined success for malformed corpus; failures do not expose secrets.
- **Validation:** adversarial test suite and relevant regression tests.
- **READ:** Decryption parser, protocol, integration fixtures/tests.
- **WRITE:** adversarial tests and minimal bug fixes within Decryption test/implementation scope; task/state files.
- **PROTECTED:** frozen protocol; unrelated packages.
- **Review gate:** Agent 03 independent targeted review required.
- **Done checklist:** adversarial tests pass; review passed; state/commit updated.

---

## SC-T07 — Public API, allocations, and concurrency hardening

- **Owner:** Agent 01
- **Reviewer:** Agent 03
- **Risk:** HIGH
- **Status:** PENDING
- **Dependencies:** SC-T06
- **Objective:** Harden API ergonomics and implementation quality without changing frozen semantics.
- **In scope:** API misuse review; buffer-size helpers if needed; disposal/lifetime review; avoidable allocation removal supported by measurement/reasoning; parallel-call tests for stateless APIs; XML docs; analyzers.
- **Exclusions:** unsafe code, pooling complexity, new public knobs, protocol changes, algorithm changes.
- **Expected outputs:** polished public APIs/docs/tests and only evidence-based low-risk performance improvements.
- **Acceptance criteria:** no security regression; clear sizing/failure docs; no unnecessary secret copies introduced; concurrency behavior tested/documented; build warning-clean.
- **Validation:** targeted API tests, concurrency tests, relevant regression suite.
- **READ:** all public APIs and relevant implementations/tests.
- **WRITE:** production/test/docs files within current three packages; task/state files.
- **PROTECTED:** frozen wire format and security invariants.
- **Review gate:** Agent 03 independent targeted review required.
- **Done checklist:** hardening complete; review passed; state/commit updated.

---

## SC-T08 — Usage documentation and safe samples

- **Owner:** Agent 01
- **Reviewer:** Agent 03 (targeted security review of sample usage)
- **Risk:** MEDIUM with security boundary
- **Status:** PENDING
- **Dependencies:** SC-T07
- **Objective:** Document correct consumption of encryption-only and decryption-only packages without teaching dangerous key/nonce practices.
- **In scope:** README/docs; minimal sample snippets; package-boundary examples; AAD guidance; key-lifetime caveat; failure handling; explicit V1 non-goals.
- **Exclusions:** deployable key vault implementation; real secrets; production config; extra sample applications unless minimal snippets are insufficient.
- **Expected outputs:** concise consumer documentation.
- **Acceptance criteria:** sample encryption consumer does not reference Decryption; sample decryption consumer does not reference Encryption; no hard-coded production-like secrets; no caller nonce control; no misleading security claims.
- **Validation:** compile sample snippets where practical; targeted Agent 03 review.
- **READ:** public APIs, protocol docs, threat model.
- **WRITE:** README/docs and directly related documentation tests/snippets; task/state files.
- **PROTECTED:** protocol/API semantics.
- **Done checklist:** docs accurate; sample review passed; state/commit updated.

---

## SC-T09 — Final V1 security and release-readiness audit

- **Owner:** Agent 03
- **Risk:** HIGH
- **Status:** PENDING
- **Dependencies:** SC-T08
- **Objective:** Independently determine whether the local repository satisfies the approved V1 security contract and is ready for human release review.
- **In scope:** package reference graph; public API surface; frozen protocol; threat-model invariants; secret/logging review; auth failure behavior; key/nonce/tag handling; adversarial tests; dependency list; build/tests; compatibility fixtures; docs claims.
- **Exclusions:** implementing new features; publishing; merging; accepting unresolved high risk.
- **Expected outputs:** concise audit result recorded in `AGENT_STATE.md` and task notes: `PASS`, `PASS_WITH_NONBLOCKING_NOTES`, or `BLOCKED`.
- **Acceptance criteria:** no unresolved HIGH/CRITICAL finding; all required suites pass; no forbidden dependency/capability coupling; no unjustified security claims; deferred scope remains deferred.
- **Validation:** clean build plus full V1 test suite once; targeted dependency/API inspection.
- **READ:** entire V1 diff and governing docs; broaden only as necessary for audit.
- **WRITE:** task/state/scope records only; implementation changes are prohibited for reviewer—route findings to owning builder for the single allowed remediation pass.
- **PROTECTED:** all production code from direct reviewer modification; remote/release systems.
- **Completion:** when PASS/PASS_WITH_NONBLOCKING_NOTES, set terminal state `ALL_APPROVED_WORK_COMPLETE`. Package publication/release still requires explicit human approval.
