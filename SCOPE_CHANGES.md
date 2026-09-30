# Scope Changes — StrongCrypt

Use this file only for work discovered outside the approved V1 task contract.

## Entry format

```text
ID: SC-SCOPE-###
Status: DISCOVERED | PENDING_APPROVAL | APPROVED_NOW | DEFERRED | REJECTED
Impact: BLOCKING | NON_BLOCKING
Risk: LOW | MEDIUM | HIGH
Discovered during: SC-T##
Summary:
Why outside scope:
Minimum decision needed:
Human decision:
```

## Initial deferred scope

The following items are already known to be outside V1 and do not require repeated rediscovery entries unless a concrete blocker makes one immediately relevant:

- password encryption / KDFs;
- ChaCha20-Poly1305 or other algorithms;
- streaming/chunked file encryption;
- KMS/HSM/cloud key-vault integration;
- envelope encryption / DEK-KEK wrapping;
- key rotation/re-encryption service;
- asymmetric encryption/signing/certificates;
- framework/database/CLI/UI integrations;
- NuGet publication/release automation;
- formal compliance/certification claims.

No active scope-change requests.
