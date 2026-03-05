---
name: no-cd-commands
enabled: true
event: bash
pattern: ^cd\s+|&&\s*cd\s+|;\s*cd\s+
action: warn
---

**Use relative paths instead of cd commands**

You attempted to use a `cd` command before running something. This is discouraged.

**Best practice:**
- Use relative paths from the project root instead of changing directories
- Example: Instead of `cd src && npm build`, use `npm build --prefix src` or run commands with full relative paths

**Why:**
- Maintains consistent working directory throughout the session
- Makes commands more reproducible and easier to understand
- Avoids state issues from directory changes

Please rewrite your command using relative paths from the project root.
