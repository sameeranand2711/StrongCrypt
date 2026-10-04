# StrongCrypt V1 — Threat Model and Security Invariants

**Status:** implementation threat model for the approved V1 scope in `SPEC.md`.
**Scope discipline:** this document interprets the approved contract. It proposes no new
cryptography, no new algorithm, and no scope expansion. Where a risk is out of V1 scope it is
recorded as an explicit non-goal with the mitigation a *consumer* must supply.

---

## 1. System under analysis

Three production packages, one AEAD profile.

```text
                 StrongCrypt.Protocol
              (constants, no capability)
                    ▲            ▲
                    │            │
       StrongCrypt.Encryption   StrongCrypt.Decryption
       (writes V1 envelopes)    (parses hostile envelopes)
```

V1 cryptographic profile: AES-256-GCM, 32-byte key, 12-byte nonce, 16-byte tag, nonce generated
internally from `RandomNumberGenerator`.

The library is a data-protection primitive. It is **not** a key-management system, not a transport
protocol, and not a file-format toolkit.

---

## 2. Assets

| ID | Asset | Why it matters |
|----|-------|----------------|
| `A1` | Plaintext | Confidentiality objective of the whole library. |
| `A2` | 256-bit data-encryption key | Compromise breaks confidentiality and authenticity for every envelope under that key. |
| `A3` | Nonce uniqueness per key | GCM nonce reuse under one key is catastrophic (§6.1). |
| `A4` | Envelope integrity/authenticity | Consumers act on decrypted output; forgery is a trust failure. |
| `A5` | Capability separation | An encryption-only deployment must not be able to decrypt, and vice versa. |
| `A6` | Protocol/format stability | Post-freeze compatibility for data already written. |
| `A7` | Absence of secrets in observable channels | Logs, exception text, fixtures, and agent logs must stay secret-free. |

Key identifiers (`A8`) are explicitly **non-secret** and travel in cleartext inside the envelope.
They must still be authenticated, because they select which key a receiver uses (§6.4).

---

## 3. Trust boundaries

| ID | Boundary | Trust posture |
|----|----------|---------------|
| `TB1` | Caller → Encryption API | Caller is trusted for its own plaintext, but assumed capable of *misuse*. API must be misuse-resistant. |
| `TB2` | Untrusted storage/transport → Decryption API | **Fully hostile.** Every byte is attacker-chosen. This is the primary attack surface. |
| `TB3` | Caller → Decryption API (key + AAD supply) | Caller supplies the key and expected AAD; the library must not silently accept a mismatch. |
| `TB4` | Encryption package ↔ Decryption package | No reference in either direction, enforced structurally. |
| `TB5` | Library → observable output (logs, exceptions, diagnostics) | Anything crossing outward may reach an attacker or a log aggregator. |
| `TB6` | Host process memory | Partially outside our control. Managed memory permits copies; zeroization is best-effort, not a guarantee. |

---

## 4. Attacker capabilities (assumed)

**In scope — the design must resist these:**

- `C1` Full read of ciphertext envelopes at rest or in transit.
- `C2` Arbitrary modification, truncation, extension, reordering, and byte-level mutation of envelopes.
- `C3` Envelope forgery from scratch, including hand-crafted headers with hostile length fields.
- `C4` Unlimited submission of chosen/malformed envelopes to a decryptor (error-oracle probing).
- `C5` Replay of a previously valid envelope, and swapping a valid envelope into a different context.
- `C6` Observation of error types/messages returned to callers, and of application logs.
- `C7` Knowledge of the complete protocol and source code (no security through obscurity).

**Out of scope for V1 — documented as non-goals, not silently ignored:**

- `C8` Physical/host compromise, debugger attach, memory scraping of a live process (`TB6`).
- `C9` Fine-grained timing/cache/power side-channel analysis of the platform AES-GCM implementation.
  V1 delegates constant-time behaviour to the platform and adds no secret-dependent branching of
  its own; it does not claim side-channel resistance beyond that.
- `C10` Compromise of the caller's key storage. V1 is not a KMS (`SPEC.md` key-handling boundary).
- `C11` Denial of service through unbounded *legitimate* volume. V1 bounds attacker-declared
  allocation (§6.5) but does not rate-limit callers.
- `C12` Quantum adversary. AES-256 is considered acceptable; no claim is made beyond that.

---

## 5. Misuse cases (the caller as a threat to itself)

The V1 API is designed so these are difficult or impossible rather than merely documented:

| ID | Misuse | V1 response |
|----|--------|-------------|
| `M1` | Caller supplies its own nonce and repeats it | Not expressible: no nonce parameter in the normal public API. |
| `M2` | Caller selects ECB/CBC/CTR, custom padding, or a short tag | Not expressible: no mode/padding/tag knobs. One profile only. |
| `M3` | Caller passes a wrong-size key | Rejected by explicit length validation before any crypto operation. |
| `M4` | Caller treats a failed decryption as success | Failure is signalled by exception; no API returns unauthenticated plaintext. |
| `M5` | Caller logs the key or plaintext via library output | Library never logs secrets and never exposes key material through `ToString()`, exceptions, or serialization. |
| `M6` | Caller assumes one key is safe forever | Documentation states an explicit finite key lifetime (§6.1). No "unlimited" claim. |
| `M7` | Caller relies on AAD but forgets to supply it on decrypt | AAD mismatch fails authentication; it cannot be silently skipped. |
| `M8` | Caller builds ambiguous AAD by concatenation | Protocol frames external AAD unambiguously with authenticated length framing (§6.6). |

---

## 6. Threats, mitigations, and invariant mapping

Each threat maps to at least one implementation responsibility and at least one test
responsibility, as required by `SC-T01` acceptance criteria. `SI-n` refers to the numbered
security invariants in `SPEC.md` §"Security invariants".

### 6.1 `T1` — GCM nonce reuse under one key (CRITICAL)

Reusing a nonce with one key in GCM reveals the XOR of plaintexts and enables forgery by leaking
the authentication subkey relationship. This is the single most severe failure mode of the chosen
primitive.

- **Attack path:** caller-controlled nonce (`M1`), a predictable RNG, or a counter reset.
- **Implementation responsibility:** nonce generated internally per encryption via
  `RandomNumberGenerator`; no nonce parameter in the normal public API; never derive a nonce from
  a timestamp, counter, GUID, machine identifier, or hash of predictable data.
- **Residual risk (accepted and documented, not eliminated):** random 96-bit nonces carry a
  birthday-bound collision probability. A key therefore has a *finite* operational lifetime.
  Documentation must state this and must not claim unlimited safety. An automated usage counter is
  out of V1 scope per `SPEC.md`.
- **Invariants:** `SI-2`.
- **Test responsibility:** repeated encryption of identical plaintext under one key yields
  different envelopes/nonces; public encryption API surface exposes no nonce parameter.

### 6.2 `T2` — Plaintext release before authentication (CRITICAL)

- **Attack path:** attacker submits modified ciphertext hoping for a decrypt-then-verify leak.
- **Implementation responsibility:** use platform `AesGcm.Decrypt`, which verifies the tag before
  yielding plaintext; construct `AesGcm` with the explicit-tag-size constructor (per `SPEC.md`
  requirement and .NET 8+ guidance) rather than defaulting; on any failure, do not return, log, or
  partially expose candidate plaintext; zero owned temporary sensitive buffers (key/plaintext) with
  `CryptographicOperations.ZeroMemory` when ownership is clear and zeroization materially reduces
  exposure, acknowledging that managed memory copies cannot be eliminated (`TB6`).
- **Invariants:** `SI-3`, `SI-4`.
- **Test responsibility:** modified ciphertext/tag/nonce/header/AAD each fail; no plaintext is
  observable on the failure path.

### 6.3 `T3` — Authentication error oracle (HIGH)

If a decryptor distinguishes "wrong key" from "tampered ciphertext", an attacker with capability
`C4`/`C6` learns which key a target holds and gains a tampering oracle.

- **Implementation responsibility:** all authentication failures — wrong key, modified ciphertext,
  modified tag, modified nonce, modified authenticated header, modified/mismatched AAD — surface as
  one indistinguishable authentication-failure result. Failure messages must be static: no key
  identifier echo, no byte offsets, no "tag mismatch at ..." detail. Structural/format errors *may*
  be distinguished from authentication failure (a malformed envelope is not a secret), but must not
  encode which cryptographic input was wrong.
- **Invariants:** `SI-4`, `SI-9`.
- **Test responsibility:** wrong key and each mutation class produce the same exception type and
  the same message; error text contains no secret and no discriminating detail.

### 6.4 `T4` — Unauthenticated header / key-identifier substitution (HIGH)

The key identifier and profile/version fields steer interpretation and key selection. If
unauthenticated, an attacker could redirect a receiver to a different key or profile, or downgrade.

- **Implementation responsibility:** bind the full protocol header — magic, version, profile,
  flags/reserved, key identifier, and framing lengths — as AEAD associated data, together with
  external AAD. Every field that affects interpretation or key selection is inside the
  authenticated region.
- **Invariants:** `SI-4`, `SI-5`.
- **Test responsibility:** mutating each authenticated header field, including the key identifier
  and any reserved bit, causes failure.

### 6.5 `T5` — Hostile length fields: unchecked allocation and integer overflow (HIGH)

The parser reads attacker-declared lengths (`C3`). Naive handling yields `OutOfMemoryException`
DoS, wraparound, or out-of-bounds reads.

- **Implementation responsibility:** validate every declared length against the *actual* remaining
  buffer length before allocating or slicing; use checked arithmetic for all offset/length maths;
  never allocate a buffer sized from an untrusted declared value before that value is proven
  consistent with the real input; enforce a documented maximum key-identifier length and reject
  impossible/negative/overflowing values.
- **Invariants:** `SI-6`.
- **Test responsibility:** declared lengths at and beyond `int.MaxValue`, lengths exceeding the real
  buffer, and lengths that would overflow when summed with offsets are all rejected without
  allocation; truncation at every structural region is rejected.

### 6.6 `T6` — AAD framing ambiguity (MEDIUM-HIGH)

If protocol-authenticated bytes and caller AAD are concatenated without framing, two distinct
(header, AAD) pairs can produce identical authenticated input — a canonicalization flaw that lets a
field's meaning shift while the tag stays valid.

- **Implementation responsibility:** frame external AAD unambiguously against the protocol header
  using explicit authenticated length framing, so no two distinct inputs yield the same
  authenticated byte sequence. Distinguish "absent AAD" from "present but empty AAD" deterministically
  and identically on both sides.
- **Invariants:** `SI-4`.
- **Test responsibility:** a fixed-length-boundary-shift case proves distinct (header, AAD) pairs
  produce distinct authenticated input; absent vs empty AAD behave consistently across encrypt and
  decrypt.

### 6.7 `T7` — Version/profile confusion and downgrade (HIGH)

- **Implementation responsibility:** V1 decryption accepts exactly the known magic, version, and
  profile identifier and fails closed on anything else. Unknown reserved/flag bits are rejected
  rather than ignored, so future semantics cannot be silently discarded by a V1 reader. The format
  stays forward-versionable, but a V1 reader never guesses.
- **Invariants:** `SI-5`.
- **Test responsibility:** unknown magic, unknown version, unknown profile, and any set reserved
  bit each fail closed.

### 6.8 `T8` — Non-canonical encoding and trailing data (MEDIUM)

Multiple byte sequences decoding to one logical envelope enable signature/dedup bypass and parser
differentials.

- **Implementation responsibility:** one canonical encoding per envelope: fixed field widths, fixed
  byte order, exact declared lengths, and a strict policy on trailing bytes (rejected unless the
  frozen protocol explicitly permits them). Parse deterministically with no lenient fallback.
- **Invariants:** `SI-5`, `SI-6`.
- **Test responsibility:** appended trailing bytes are rejected; a declared length shorter or longer
  than the true ciphertext is rejected.

### 6.9 `T9` — Capability boundary erosion (HIGH, architectural)

The value of split packages collapses if a reference or a helper leaks across.

- **Implementation responsibility:** `StrongCrypt.Encryption` never references
  `StrongCrypt.Decryption` and holds no decryption logic; `StrongCrypt.Decryption` never references
  `StrongCrypt.Encryption`, holds no encryption logic, and does **not** reuse the encryptor's
  serializer — it implements its own hostile-input parser. `StrongCrypt.Protocol` stays
  capability-neutral: constants, identifiers, immutable non-secret metadata, and shared validation
  limits only, with no crypto operation, no key access, and no envelope parsing. Where key-facing
  abstractions are introduced, encryption and decryption use separate types per `SPEC.md` key-handling
  boundary.
- **Invariants:** `SI-1`, `SI-7`, `SI-8`.
- **Test responsibility:** reference graph asserted at project-file and compiled-assembly level in
  both directions; each capability assembly absent from the other's dependency closure. *(Implemented
  in `SC-T00`.)*

### 6.10 `T10` — Secret leakage through observable channels (HIGH)

- **Implementation responsibility:** never log or include in exception text any key bytes,
  plaintext, nonce-with-key pairing, or credential; no key-bearing type exposes material via
  `ToString()`, diagnostics, debugger projections, or serialization; test keys are synthetic and
  clearly non-production; committed fixtures and `agent_logs/` contain no secret.
- **Invariants:** `SI-9`.
- **Test responsibility:** authentication-failure messages are asserted static and secret-free; any
  key-bearing type's `ToString()` asserted not to reveal material.

### 6.11 `T11` — Replay and context confusion (MEDIUM — shared responsibility)

A valid envelope replayed, or moved to a different context, remains cryptographically valid: GCM
authenticates contents, not freshness or destination.

- **V1 position:** this is **not** solvable inside the library and V1 does not claim to solve it.
  The library provides the *mechanism* — AAD — for the caller to bind context (tenant, record ID,
  purpose, version). Documentation must state plainly that replay/freshness protection is the
  consumer's responsibility and show AAD used for context binding.
- **Invariants:** supports `SI-4`; freshness explicitly a non-goal.
- **Test responsibility:** an envelope authenticated under one AAD context fails under a different
  AAD context.

### 6.12 `T12` — Weak or misused randomness (HIGH)

- **Implementation responsibility:** all cryptographic randomness comes from
  `RandomNumberGenerator`. `System.Random`, GUIDs, timestamps, process/machine identifiers, and
  hashes of predictable data are forbidden as sources of nonces or keys. V1 does not generate keys
  on the caller's behalf beyond any explicitly approved, clearly documented helper.
- **Invariants:** `SI-2`.
- **Test responsibility:** no production source references `System.Random` or `Guid` for
  cryptographic material; generated nonces are distinct across repeated calls.

### 6.13 `T13` — Concurrency-induced state corruption (MEDIUM)

Shared mutable state in an encryptor or decryptor could cause a nonce or buffer to be reused across
threads.

- **Implementation responsibility:** public types documented as stateless/thread-safe must hold no
  mutable shared state across calls; where a type owns disposable native state, ownership,
  lifetime, and thread-affinity are documented precisely. Nonce generation must be safe under
  concurrent use.
- **Invariants:** supports `SI-2`.
- **Test responsibility:** parallel encryption produces all-distinct nonces; parallel round trips
  succeed for any API documented as concurrency-safe.

### 6.14 `T14` — Test and validation integrity erosion (process, HIGH)

A security suite that is quietly weakened is worse than none, because it produces false assurance.

- **Implementation responsibility:** never delete or skip a security test, weaken an assertion,
  swallow an exception, disable an analyzer, or change an expected result to match known-bad
  behaviour in order to get a green run. A genuinely wrong test contract is recorded as
  `TEST_CONTRACT_CONFLICT` with evidence and resolved through approval, not edited away.
- **Invariants:** `SI-10`.
- **Test responsibility:** `TreatWarningsAsErrors` and analyzers stay enabled *(configured in
  `SC-T00`)*; compatibility fixtures are retained permanently once frozen.

---

## 7. Invariant → responsibility matrix

Every `SPEC.md` security invariant maps to at least one implementation responsibility and at least
one test responsibility.

| Invariant | Threats | Implementation owner | Verified by |
|-----------|---------|----------------------|-------------|
| `SI-1` no custom cipher/mode/GHASH/AES | `T9` | Both capability packages: platform `AesGcm` only | Source review; no hand-rolled primitive; `SC-T09` audit |
| `SI-2` no intentional key+nonce reuse | `T1`, `T12`, `T13` | Encryption: internal CSPRNG nonce, no nonce knob | Distinct-nonce tests, parallel-nonce test, API-surface test |
| `SI-3` authentication precedes plaintext release | `T2` | Decryption: platform verify-then-release | Tamper tests; no-plaintext-on-failure test |
| `SI-4` any modification fails | `T2`, `T3`, `T4`, `T6`, `T11` | Decryption: full header + framed AAD authenticated | Mutation suite across every envelope region (`SC-T06`) |
| `SI-5` unsupported data fails closed | `T7`, `T8` | Decryption: strict magic/version/profile/reserved checks | Unknown-version/profile/reserved-bit tests |
| `SI-6` no unchecked allocation or overflow | `T5`, `T8` | Decryption: validate-before-allocate, checked arithmetic | Hostile-length and truncation tests |
| `SI-7` encryption consumers gain no decryption reference | `T9` | Project/reference structure | Boundary tests *(done, `SC-T00`)* |
| `SI-8` decryption consumers gain no encryption reference | `T9` | Project/reference structure | Boundary tests *(done, `SC-T00`)* |
| `SI-9` no secret in logs/exceptions/fixtures | `T3`, `T10` | All packages: static messages, no secret-bearing output | Message-content assertions; fixture review |
| `SI-10` tests not weakened to pass | `T14` | Process discipline | Review gates; analyzers enforced; fixtures retained |

---

## 8. Explicit non-goals (restating `SPEC.md`, so absence is not read as oversight)

V1 does **not** provide, and must not claim: password-based encryption or KDF APIs; algorithms
other than AES-256-GCM; streaming/chunked file encryption; KMS/HSM/cloud key-vault integration;
envelope encryption with DEK/KEK wrapping; key rotation or re-encryption services; asymmetric
encryption, signing, or certificates; tokenization; replay/freshness protection (`T11`);
side-channel resistance beyond what the platform provides (`C9`); protection against host or
process compromise (`C8`); FIPS certification or compliance claims; and any "military-grade",
"unbreakable", "zero-risk", or formally-verified characterization.

Keys have a finite operational lifetime (§6.1). Deployments with very high per-key encryption
volume require an explicit key-usage policy that V1 does not automate.

---

## 9. Downstream task obligations

| Task | Obligation established here |
|------|-----------------------------|
| `SC-T02` | Freeze a canonical layout satisfying `T4`, `T5`, `T6`, `T7`, `T8`; specify the authenticated region, AAD framing, key-identifier limits, reserved-bit and trailing-data policy, and the format-vs-authentication error taxonomy of `T3`. |
| `SC-T03` | Encryption satisfying `T1`, `T10`, `T12`, `T13`; canonical envelope writing; no decryption capability. |
| `SC-T04` | Independent hostile parser satisfying `T2`, `T3`, `T5`, `T7`, `T8`, `T9`; fail-closed release. |
| `SC-T05` | Cross-package interoperability without coupling (`T9`); official AES-GCM known-answer vectors; permanent fixtures (`T14`). |
| `SC-T06` | Systematic mutation/truncation/abuse suite for `SI-4`, `SI-5`, `SI-6`. |
| `SC-T07` | Concurrency and API-misuse hardening for `T13`, `M1`–`M8`; no security regression. |
| `SC-T08` | Documentation carrying the `T1` key-lifetime caveat, the `T11` replay non-goal, and AAD context-binding guidance, with no misleading claims. |
| `SC-T09` | Independent audit that every row of §7 is actually satisfied by code and tests. |
