I have added two new agents to the existing StrongCrypt project:

- `agents/agent-04-dependency-injection-builder.md`
- `agents/agent-05-console-demo-builder.md`

The original Agents 01–03 have already completed their implementation.

**Your first task is to register Agents 04 and 05 into the existing workflow, then execute them continuously.**

1. Read the existing `AGENT_GUARDRAILS.md`, `AGENT_RULES.md`, `AGENT_STATE.md`, `SPEC.md`, and `TASKS.md`.
2. Read the new Agent 04 and Agent 05 instructions.
3. Verify that `SC-T09` has passed and that no blocking security findings remain.
4. Register the following tasks in `TASKS.md`:
   - **SC-T10:** Agent 04 — Implement dependency injection extensions for encryption and decryption, with only essential tests. Require Agent 03's independent security review.
   - **SC-T11:** Agent 05 — Implement a simple console application demonstrating encryption and decryption using DI.
5. Update `SPEC.md`, `AGENT_RULES.md`, `AGENT_STATE.md`, and the relevant approval/scope records to reflect my explicit authorization for these two additional agents and features.
6. Preserve the original security guardrails, implementation, and V1 audit history.
7. Check whether DI functionality already exists. Do not duplicate existing implementations or execute the older Phase 2 DI overlay as a separate workflow.

**Execution instructions:**

- Start `SC-T10` immediately after registration and prerequisite verification.
- Once Agent 04 passes validation and security review, automatically proceed to Agent 05.
- Do not request approval between normal tasks.
- Keep implementations minimal, readable, and maintainable.
- Avoid unnecessary tests, abstractions, dependencies, repository rereading, and token consumption.
- Update state and commit completed tasks according to the existing Git policy.
- Stop only for significant security, architecture, scope, dependency, or authority decisions covered by the existing guardrails.

**Do not stop after updating the task files. Continue with implementation until both tasks are completed, a genuinely important approval is required, or a blocking condition prevents safe progress.**