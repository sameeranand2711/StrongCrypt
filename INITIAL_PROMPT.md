# Initial Execution Prompt — StrongCrypt V1

Execute this repository's StrongCrypt V1 agent pack continuously.

Start by reading, in order:

1. `AGENT_GUARDRAILS.md`
2. `AGENT_STATE.md`
3. `TASKS.md` entry `SC-T00`
4. relevant sections of `SPEC.md`
5. `agents/agent-01-encryption-protocol-builder.md`

Then inspect Git and the repository.

## Execution behavior

- Work through the approved task chain continuously.
- Do **not** ask me to approve normal implementation choices or whether to continue.
- After each task passes its required validation/review, update state, create the allowed local checkpoint/commit when safe, and immediately continue to the next ready task.
- Switch among Agent 01, Agent 02, and Agent 03 roles exactly as `TASKS.md` requires. No subagents and no fourth agent.
- Keep context bounded to the active task.
- Use the least expensive capable model; do not escalate to a premium/expensive model without explicit approval.

Stop only when:

1. `AGENT_GUARDRAILS.md` defines a human-approval gate that is actually triggered;
2. a blocker remains after the bounded retry/remediation policy;
3. repository/state disagreement cannot be safely reconciled; or
4. all approved V1 work is complete.

If approval is required, provide one concise decision packet: exact decision, why it is security/scope critical, options, consequences, and your safest default recommendation. Do not ask unrelated questions.

Do not push, merge, publish, release, deploy, access production, or change credentials.
