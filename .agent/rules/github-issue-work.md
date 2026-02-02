---
trigger: manual
---

Trigger: When a user requests work on a GitHub issue and provides an issue URL.

Requirements:
- Extract the issue number from the URL and use it as the ticket ID.

- Create a new branch named: issue-<ticket-id>-<short-description>

- Follow all repository and project rules.

- Create a new ADR in the ADRs solution folder:

- Match the structure and style of the most recent ADR.

- Reference the ticket ID.

- Implement the solution in small, logical steps.

Create one atomic commit per step:

Each commit must reference the ticket ID.

No mixed or bulk changes.

Constraints

No commits to the default branch.

No skipping ADR creation.