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
The check-status read below degrades the same way: print the exact `gh api` calls and judge nothing.

## Merge

- **Read the pull request**: `gh pr view <number> --json state,mergeable,headRefOid,mergeCommit` (or
  `GET /repos/bomret/CitiesSkylinesMods/pulls/<pull_number>` for `state`, `merged`, `merge_commit_sha`, `head.sha`,
  and `mergeable`). GitHub computes `mergeable` asynchronously: while it does, REST answers `null` and
  `gh pr view --json mergeable` answers `UNKNOWN`. Both read as not yet known: re-read up to 3 times with a short
  pause and, if it is still not known, report it as **unknown** — never as mergeable.
- **Read the draft state**: `gh pr view <number> --json isDraft --jq .isDraft` prints `true` for a draft.
- **Mark ready**: `gh pr ready <number>` takes a draft pull request out of draft. GitHub refuses to merge a
  draft, so `/superdev:ship` calls it before the merge under the merge consent.
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
  `scope.js suite reusable` just accepted, the carry covered, ship's own local run passed, or whose named checks
  `scope.js ci` judged green, resolved with `git rev-parse <branch>` in the scope checkout.
  `/superdev:ship` reads the pull request's own `head.sha` only to decide whether to push and to refuse
  when it still differs from that tip after pushing, never as the merge's own pin.

## Check status

`/superdev:ship`, `/superdev:review`, and `/superdev:setup` read the latest check status of one commit through this
call, and `scope.js ci` judges it against the named checks the `## CI` section of `docs/agents/verification.md` lists (ADR 0255).
The read emits one `name<TAB>state<TAB>link` line per latest check run and per latest commit-status context, across
every page:

- **Check runs**: `gh api --paginate 'repos/bomret/CitiesSkylinesMods/commits/<sha>/check-runs?per_page=100' --jq '.check_runs[] | [.name, (if .status == "completed" then .conclusion else .status end), (.html_url // .details_url // "")] | @tsv'`.
  The endpoint's default `filter=latest` keeps the latest run per name, and `--paginate` follows every page.
- **Commit statuses**: `gh api --paginate 'repos/bomret/CitiesSkylinesMods/commits/<sha>/status?per_page=100' --jq '.statuses[] | [.context, .state, (.target_url // "")] | @tsv'`.
  The combined status holds the latest status per context.
- **Read into a file, then judge.** `c=$(mktemp) && gh api … > "$c" && gh api … >> "$c" && node
  "${CLAUDE_PLUGIN_ROOT}/scripts/scope.js" ci --sha <sha> < "$c"`. A pipe drops the read's exit status, so a failed
  read would reach the verdict as empty input. No `gh`, no login, a 401, 403, or 404, or a network error ends the chain
  at the failed read with its error: report it, and pass nothing to `scope.js ci`.
- Nothing here waits for a check, re-reads it on a timer, or starts, re-runs, or cancels a CI run.

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
