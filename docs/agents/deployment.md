# Deployment

A repository's deploy posture and its deploy targets: what `/superdev:ship` does with a feature's deploy
targets once its pull request has merged (ADR 0101). `/superdev:plan` reads the targets table to ask what
a feature deploys to; `/superdev:ship` reads the posture and runs (or prints) the targets a feature's
anchor lists.

## Posture

One of `none`, `manual`, or `auto`, for the whole repository — never one per target:

- **`none`** — nothing deploys. The default for a repository with no deploy target at all.
- **`manual`** — the operator deploys outside superdev. `/superdev:ship` prints each target's command and
  records it pending.
- **`auto`** — `/superdev:ship` runs each target's command itself, after the merge, in a tree checked out
  at the merge commit.

**Only `/superdev:ship` ever runs a target's command, and only under the `auto` posture** (INV-0098). No
other command, and no unattended run, deploys. `/superdev:setup` proposes `manual` as the default and
never proposes `auto`: the operator picks `auto` themselves.

**A repository carrying no `docs/agents/deployment.md` reads as posture `none`.**

`/superdev:ship` reads the posture from this document's own `Posture:` line, in this `## Posture`
section. **A document carrying no such line reads as posture `none`**, the same as a repository carrying
no `docs/agents/deployment.md` at all.

Posture: `manual`

## Targets

One row per deploy target: its name, the command that deploys it, and how to reach it **read-only** for
verification — never a command that changes anything. Empty under `manual` or `auto` is valid: the
repository deploys nothing yet.

| Name | Command | Read-only reach |
|---|---|---|
| `naturallighting` | `pnpm --filter naturallighting run release`, then upload the mod from the game's Content Manager to the Steam Workshop | https://steamcommunity.com/sharedfiles/filedetails/?id=3145348939 |
| `removechirper` | `pnpm --filter removechirper run release`, then upload the mod from the game's Content Manager to the Steam Workshop | https://steamcommunity.com/sharedfiles/filedetails/?id=3134254484 |

Under `auto`, a target's command runs in a tree detached at the merge commit that can still carry ignored
files from the run — installed dependencies, build output — so the command must not rely on their
presence or absence: it installs and builds what it needs itself.

`/superdev:verify-ship` reads the `Read-only reach` column to observe a deployed target, and runs no
target's command.

`/superdev:plan` offers this table's names on a feature's `**Deploy:**` line, in deploy order.
`/superdev:ship` reads that line from the anchor and runs (or prints) the listed targets' commands in
that order.
