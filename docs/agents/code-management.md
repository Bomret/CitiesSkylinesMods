# Code host: GitHub

Code lives on GitHub. Use the `gh` CLI for push + pull requests. `gh` infers the repo from
`git remote -v` when run inside a clone.

## Configuration

- **Owner / repo**: `bomret/CitiesSkylinesMods` on `github.com`.
- **Remote**: `origin` (GitHub). Confirm with `git remote -v`.
- **Default PR posture**: `draft` (open as draft; the user flips to ready). Override: `ready` | `push-only`.
- **Base branch**: the branch the scope branch was cut from (default `main`).

## Conventions

- **Push the scope branch**: `git push -u origin <scope-branch>`.
- **Open the PR** (default draft):
  `gh pr create --draft --base <base> --head <scope-branch> --title "<title>" --body-file <file>`.
  Use a body file (heredoc) built per `/superdev:submit-pr`'s body rules.
- **Ready (non-draft)**: same without `--draft`.
- **Never interpolate a store-derived value into a shell string.** The title comes from the slice/plan;
  if the slice store is multi-writer, treat it as untrusted. Pass it as a single quoted `--title "<title>"`
  argument (so `gh` receives it as one argv, not a shell fragment) and always use `--body-file` for the
  body — never build a command line by concatenating a title/body that could carry `"`/`` ` ``/`$( )`.
- **Report**: print the PR URL (`gh pr view --json url --jq .url`).

## Degrade when `gh` is absent

Do not fail. Push the branch, then print the compare URL
(`https://github.com/bomret/CitiesSkylinesMods/compare/<base>...<scope-branch>?expand=1`) and the exact
`gh pr create ...` command the user can run once `gh` is available.
The merge call below degrades the same way: print the exact `gh pr merge`/REST call and merge nothing.

## Merge

- **Read the pull request**: `gh pr view <number> --json state,mergeable,headRefOid,mergeCommit` (or
  `GET /repos/bomret/CitiesSkylinesMods/pulls/<pull_number>` for `state`, `merged`, `merge_commit_sha`, `head.sha`,
  and `mergeable`). GitHub computes `mergeable` asynchronously and answers `null` while it does;
  re-read up to 3 times with a short pause and, if it is still `null`, report it as **unknown** — never
  as mergeable.
- **Merge it, pinned**: `gh pr merge --squash --match-head-commit <tested-tip-sha> <number>` (or
  `PUT /repos/bomret/CitiesSkylinesMods/pulls/<pull_number>/merge` with
  `{ "merge_method": "squash", "commit_title": "<pr-title>", "sha": "<tested-tip-sha>" }`).
  `--match-head-commit`/`sha` pins the merge to that sha: the host answers 409 when the head moved
  since. Never pass `--admin` (or any other check/review bypass) — this seed never sends it.
- **A refusal names the reason**: 405 means the pull request cannot be merged (an unmet required
  review or status check, or it is already merged/closed); 409 means the head moved since it was read,
  or a conflict exists. Relay the host's own message to the operator instead of retrying.
- **Find an open pull request by its head branch** (a fallback, when no number is on hand): `gh pr list
  --head <branch> --state open` (or `GET /repos/bomret/CitiesSkylinesMods/pulls?head=bomret:<branch>&state=open`).
  Leaving `state=open` off would surface closed and merged pull requests too, including an old one left
  over from a reused branch name — never widen the filter to catch one of those.
- **`<tested-tip-sha>` above is never read from the host.** It is the local scope-branch tip
  `scope.js suite reusable` just accepted, resolved with `git rev-parse <branch>` in the scope checkout.
  `/superdev:ship` reads the pull request's own `head.sha` only to decide whether to push and to refuse
  when it still differs from that tip after pushing, never as the merge's own pin.

## Edit the pull request body

A run that resolves a deferred finding after `/superdev:submit-pr` opened the pull request rebuilds that
pull request's `## Deferred to a plan` section (`references/scope-findings.md`'s `## Deferred findings`,
ADR 0121). Only an open pull request is edited.

- **Read it**: `gh pr view <number> --json state,body` (or `GET /repos/bomret/CitiesSkylinesMods/pulls/<pull_number>`
  for `state`, `merged`, and `body`), into a file. Only `state` `OPEN` (`open` and `merged` false on the
  REST read) is edited; a merged or closed pull request gets no edit.
- **Edit it, by file**: read into one file, build the new body into a second, and edit only when every
  step succeeded: `gh pr view <number> --json state,body > "$r" && jq -e '.state == "OPEN"' "$r"
  >/dev/null && jq -je .body "$r" > "$b" && <producer> < "$b" > "$f" && [ -s "$f" ] && gh pr edit
  <number> --body-file "$f"`. Name each file with `mktemp`. `<producer>` builds the new body from `"$b"`.
  A failed read, a merged or closed pull request, a failed producer, or an empty result stops the chain
  before the edit, and the body stays as it was. Never pass the body on stdin from a pipe: a pipe drops
  the producer's exit status, and an empty body would replace the pull request's own.

## Compute the base

A scope's BASE for the final-reviewer's BASE..HEAD diff (HEAD = scope branch tip) is its range base,
computed by `scope.js base resolve` per `${CLAUDE_PLUGIN_ROOT}/references/scope-mode.md`'s `## The scope's range base`.
An ad-hoc range with no run root uses `git merge-base <base> <scope-branch>`.
