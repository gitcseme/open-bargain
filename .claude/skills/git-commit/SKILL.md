---
name: git-commit
description: Turn the working tree into one or more clean git commits, fast. Groups changed files into logical commits with Google-style messages, leaves out local-only changes the user made just to run the app, shows the plan, and commits only after the user approves. Use this whenever the user asks to commit, make commits, "commit this", "commit the task", split changes into commits, or reaches the commit step of a task, even if they don't name the skill.
---

# git-commit

Goal: a short series of commits, each one a single logical change, with nothing committed that the user changed only for local running. Be quick: no per-commit builds, no worktrees. Nothing is committed until the user approves the plan.

## Phase 1: Plan (read-only)

### 1. Look at what changed

Run these together:

```
git status --porcelain=v1 -uall
git diff HEAD
git log -5 --format='%s%n%b---'
```

Read new (untracked) files too, but skim: you need enough to group them and spot local-only changes, not a review. Skip generated files (migration designers/snapshots, lock files) beyond a glance.

If the current branch is main/default, say so and suggest a branch name.

### 2. Leave out local-only changes

Don't commit changes made just to run the app locally:

- Connection strings, ports, hostnames or URLs pointed at local values (appsettings, `launchSettings.json`, `docker-compose.yml`, `.env`, Vite config, ...)
- Secrets, tokens, passwords, API keys, personal paths (`C:\Users\...`, `/home/...`)
- Debug logging, `console.log`/debug prints, flags flipped on, auth or validation disabled, code commented out "for now"
- IDE settings, scratch files, data dumps, build output that `.gitignore` missed

A change explained by a task doc or `docs/decisions.md` is real work. If a file mixes real changes and a local tweak, commit only the real hunks (see "Partial files"). If you can't tell, list it under "Unsure". Leaving something out is recoverable; committing a secret is not.

### 3. Group into logical commits

Group by what the change does, not by file type or folder. One commit is fine when it's really one change; don't split for its own sake.

- A feature or fix together with its tests
- A new project together with the solution entry that includes it
- A model change together with its migration
- Tooling/config (CI, tool manifests, lint config) on its own
- Refactors and renames apart from behaviour changes
- Docs with the code they describe; general docs on their own

Order dependencies first (shared/contracts before consumers) so each commit makes sense on its own. Use judgment instead of building to check this.

### 4. Write Google-style commit messages

- **Subject:** one line, imperative mood ("Add", "Fix", not "Added"/"Adds"), about 50 characters and at most 72, no trailing period. It says what the commit does, and it's a complete thought on its own.
- Keep the repo's subject prefix if `git log` shows one (e.g. `M1-2:`).
- **No body.** The subject line is the whole message.
- Add any trailer lines the session or project requires (such as `Co-Authored-By`) after a blank line.

### 5. Show the plan and wait

Use exactly this shape, then ask for approval:

```
Branch: <branch>

commit-1: <subject>
Files:
- path/a.cs
- path/b.cs (partial: only the X hunk)

commit-2: <subject>
Files:
- path/c.cs

Left out (stays uncommitted):
- path/appsettings.Development.json: points at your local DB

Unsure (tell me which way):
- docker-compose.yml: port 5432 → 15432. Real change or local?
```

Leave out the "Left out" and "Unsure" sections when they're empty. If the user changes grouping, messages or exclusions, show the updated plan. Commit only on a clear yes.

## Phase 2: Commit (after approval)

For each commit, in order:

1. Stage only its files by explicit path: `git add -- <paths>`. Never `git add -A`, `git add .` or `git commit -a`, since those pick up the left-out changes.
2. For partial files: `git apply --cached <scratch>/cN-partial.patch`
3. Check that `git diff --cached --name-only` matches the planned file list.
4. Commit with the subject (plus any required trailer, using a heredoc or `-F <file>`). Don't use `--no-verify`. If a hook fails, stop and report it.

Afterwards, show `git log --oneline -<N>` and `git status --short`, and point out that what's left are the changes deliberately left out. Don't push unless asked.

## Partial files

When only some hunks of a file belong in a commit (`git add -p` isn't available):

1. `git diff HEAD -- <file> > <scratch>/full.patch`
2. Write a copy that keeps only the wanted hunks (fix the `@@` line counts if you trim inside a hunk): `<scratch>/cN-partial.patch`
3. Check it: `git apply --cached --check <scratch>/cN-partial.patch`
