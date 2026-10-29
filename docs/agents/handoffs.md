# Private handoffs between machines

The public DESMAP repository holds code and reviewed documentation. Store internal handoffs in a separate private notes repository cloned beside it:

```text
workspace/
  DESMAP/
  DESMAP-notes/
    README.md
    handoffs/
      <task-slug>.md
```

Resolve `../DESMAP-notes/` relative to the DESMAP checkout root, regardless of the shell's current directory. The same layout works under different drive letters on each machine.

## Write a handoff

When asked to hand off work, or when pausing an unfinished task:

1. Inspect the current branch, HEAD, and working tree. Record the actual state, including untracked files needed to continue. Preserve changes made by other tasks.
2. Update `../DESMAP-notes/handoffs/<task-slug>.md`. Use one current handoff per task and preserve stop instructions or unresolved decisions. Add a relative link and a one-line status to the notes repository's `README.md`.
3. Include the fields below. Separate completed checks from plans and unverified claims. For commands, state the working directory. Reference code and artifacts with paths relative to their respective roots.
4. Confirm the handoff exists and the index link resolves. Report its location and whether it is local only or has actually been committed and pushed to the private remote.

If the notes directory is absent, create the local directory, index, and handoff there. It is local only until a private Git remote is configured and the notes are pushed. Continue the authorized task without requiring remote setup. Never silently fall back to storing the handoff in the public checkout or `output/`.

Use this outline, omitting empty fields:

```markdown
# <Task>

Updated: <timestamp with timezone>
Status: <in progress / paused / blocked / complete>
Code branch: <branch>
Code HEAD: <full commit SHA>
Working tree: <clean, or exact changes not included in HEAD>

## Goal and constraints
## Completed work
## Verification
Commands, working directories, results, and unverified behaviour.

## Next steps
## Required local artifacts
Locations, transfer requirements, and whether each artifact is available on the other machine.
```

Keep credentials in each machine's environment or secret store. A private notes repository can hold operational context, but it is not a credential store. Keep large or sensitive runtime artifacts in separate private storage and reference their locations.

## Switch machines

1. Before switching, save the required code changes on a task branch and push it when Git synchronization is authorized. Record that exact commit in the handoff. If work remains uncommitted or unpushed, explicitly mark the transfer incomplete and list what exists only on the source machine.
2. Commit and push the notes to the verified private remote when authorized. Copy any required ignored artifacts through private storage. Git ignore rules do not transfer those files.
3. On the destination machine, fetch the code branch and notes. Compare the handoff's branch and SHA with the checkout and inspect the working tree before continuing. Reconcile differences without overwriting local work.
4. Recreate the local environment using `README.md`, including private configuration, models, and Unity third-party assets as needed. Confirm the artifacts required for the next step are available.

Branches in the public code repository are public too. Verify a notes remote's visibility before pushing internal notes; a directory or repository name does not establish privacy.

## Resume or migrate older notes

Read the task's indexed handoff, then verify its statements against the current checkout. Old paths, test counts, feature flags, and uncommitted-file lists describe a past state. A stopped task stays stopped until the user requests its continuation.

Older handoffs may still exist under `output/`. When migrating one, preserve its original text in `../DESMAP-notes/handoffs/archive/` as a historical snapshot and write a current indexed handoff after checking the code. Confirm the archived copy before removing the old file. Keep runtime artifacts intact unless cleanup is explicitly requested.
