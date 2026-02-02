---
trigger: always_on
---

When generating code, do not introduce magic numbers or magic strings.

A magic number or magic string is any literal value whose meaning is not immediately obvious from context or is likely to change.

Instead, always:

- Define meaningful named constants, enums, or configuration values
- Centralize reusable values in a single place
- Use self-describing names that express intent