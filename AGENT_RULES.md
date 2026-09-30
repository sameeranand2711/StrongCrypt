# Agent Execution Rules — StrongCrypt

## Operating mode: continuous by default

This pack runs in **continuous execution mode**.

When approved work remains, the active AI must continue through ready tasks without asking the user to approve ordinary progress. Human approval is an exception gate defined in `AGENT_GUARDRAILS.md`, not a step between tasks.

Terminal states for a run are:

- `ALL_APPROVED_WORK_COMPLETE`
- `HUMAN_APPROVAL_REQUIRED`
- `BLOCKED`

Do not stop merely to report progress if the next approved action can be performed safely in the same run.

## Role model

There are three agents/roles and no orchestrator agent:

1. Agent 01 — Encryption & Protocol Builder.
2. Agent 02 — Decryption & Adversarial Builder.
3. Agent 03 — Independent Cryptography Security Reviewer.

The host AI may execute these roles sequentially as required by `TASKS.md`. Role switching is not a fourth agent. Each role must read its own role file before acting.

Agents may not spawn or delegate to subagents.

## Why no orchestrator

The dependency graph is mostly linear and task routing is explicit in `TASKS.md`. A dedicated controller would add context/token cost without enough coordination value. `TASKS.md` + `AGENT_STATE.md` are the workflow controller.

## Task lifecycle

For each task:

1. verify Git/working-tree state;
2. read `AGENT_GUARDRAILS.md`;
3. read `AGENT_STATE.md`;
4. read only the active task in `TASKS.md` plus relevant `SPEC.md` sections;
5. read the assigned role file;
6. inspect only relevant repository files;
7. mark task `IN_PROGRESS` and record exact scope in state;
8. implement within WRITE scope;
9. run task-specific validation;
10. perform builder self-review;
11. if task risk is `HIGH`, mark `REVIEW` and switch to Agent 03 for one independent targeted review;
12. if review passes, mark `DONE`; if review returns bounded findings, owner performs one remediation pass and Agent 03 verifies those findings only;
13. update `AGENT_STATE.md`, `TASKS.md`, and `SCOPE_CHANGES.md` if needed;
14. create a local commit when repository state permits and no unrelated user changes would be captured;
15. immediately select the next `READY` approved task.

Do not ask "continue?" between steps 1–15.

## Task selection

Choose the earliest task whose dependencies are `DONE` and status is `READY`/`PENDING` with no unresolved approval gate.

A dependent task cannot start until all listed dependencies are `DONE`.

## Scope changes

For unrelated findings:

- record them in `SCOPE_CHANGES.md`;
- mark `BLOCKING` or `NON_BLOCKING`;
- continue current work when non-blocking;
- do not deeply investigate or implement unapproved scope.

If the finding changes cryptographic design, capability boundaries, frozen protocol/public API, runtime crypto dependencies, or V1 scope, stop at `HUMAN_APPROVAL_REQUIRED`.

## Risk and review

All cryptographic design/implementation/parser tasks are `HIGH` risk. Agent 03 independent review is mandatory before those tasks become `DONE`.

The reviewer must inspect the current task, acceptance criteria, relevant diff, nearby invariants, and targeted tests. It must not reread the whole repository by default.

## Cost controls

- Load only task-relevant context.
- Reuse deterministic build/test evidence when inputs have not changed.
- Do not run the entire suite after every tiny edit; run task-specific tests during implementation and the required regression points in `TASKS.md`.
- Do not create extra agents for docs, Git, routine testing, formatting, or status reporting.
- Do not escalate to premium models without approval.
- Stop blind debugging after two materially different failed attempts on the same blocker.
- Avoid repeated security essay generation; use concise evidence and actionable findings.

## Git policy

Local authority is granted to:

- initialize Git if absent and safe;
- preserve/create an appropriate `.gitignore` including `agent_logs/`, `bin/`, `obj/`, IDE artifacts, local temp outputs, and secrets;
- create/use a feature branch such as `feature/strongcrypt-v1` when repository policy does not provide another branch;
- commit each fully completed bounded task locally.

Do not include unrelated pre-existing user changes in a commit.

Never auto-merge. Never push, create PRs/tags/releases, or publish packages without human approval.

## State discipline

`AGENT_STATE.md` is the compact resume checkpoint. Keep it under roughly 180 lines. It must contain only what another session needs to continue safely.

`agent_logs/` may hold concise obsolete runtime detail and must remain Git-ignored. Normal continuation must never require reading all logs.

## Repository evidence

Do not invent files, classes, packages, commands, or behaviors. Verify them. Record important unresolved assumptions explicitly.

## Completion report

When all approved tasks are complete, report:

- packages produced;
- protocol version/profile;
- validation executed;
- security-review status;
- known limitations/deferred scope;
- whether any human approval is still required for publish/release.

Do not claim external publish/release occurred unless it actually did and was authorized.
