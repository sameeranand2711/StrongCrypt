# Continuation Prompt — StrongCrypt V1

Resume the approved StrongCrypt V1 workflow without rediscovering the project.

In this order:

1. inspect `git status`, current branch, and recent relevant commit;
2. read `AGENT_GUARDRAILS.md`;
3. read `AGENT_STATE.md`;
4. read only the active/next ready task in `TASKS.md`;
5. read only the relevant `SPEC.md`/protocol/threat-model sections and assigned agent role;
6. inspect active-task changes and relevant source/tests;
7. reconcile state with Git if needed, then continue from the recorded resume point.

Do not start by reading the whole repository or `agent_logs/`.

Continue automatically through subsequent approved tasks. Do not ask "should I continue?" while ready approved work remains.

Stop only for an explicit human-approval gate, an exhausted genuine blocker, an unreconcilable state mismatch, or completion of all approved work.

Preserve the three-agent cap, independent HIGH-risk review, one-review/one-remediation limit, and two-attempt blocker limit. Never push/merge/publish/deploy without explicit human approval.
