# Code host: Forgejo / self-hosted (Gitea-compatible)

Code lives on a Forgejo (or Gitea) instance. Every host call goes through the `tea` CLI: a subcommand
where one performs the operation, `tea api` where none does.

## Configuration (fill in at setup)

- **Base URL**: `https://codeberg.org`
- **Owner / repo**: `bomret/CitiesSkylinesMods` (confirm with `git remote -v`).
- **Remote**: `origin`. Confirm with `git remote -v`.
- **Default PR posture**: `draft` (override: `ready` | `push-only`).
- **Base branch**: the branch the scope branch was cut from (default `main`).

## tea conventions

- **Login**: resolved, never stored. It is the one entry of `tea login list -o json` whose `url` equals the
  Base URL. Every call passes `--login <login> --repo <owner>/<repo>`: without `--login`, `tea` falls back
  to another host's login when none matches the current remote. Zero matching entries, or no `tea` on the
  path, stops the step (*Degrade* below). Two or more matching entries are named, and the operator picks.
- **`jq` is required.** The pinned merge body below is built with it, so a missing `jq` stops the step the
  way a missing `tea` does (*Degrade* below).
- **A deciding `tea api` call passes `-i`** and reads the HTTP status from stderr, because `tea api` exits
  0 on a 4xx or 5xx answer.
- **Push**: `git push -u origin <scope-branch>`.
- **Open PR**: `tea pr create --login <login> --repo <owner>/<repo> --base <base> --head <scope-branch>
  --title "$title" --description-file -`, the body on stdin, `title` set from a quoted heredoc
  (`title=$(cat <<'SUPERDEV_TEXT_EOF' … SUPERDEV_TEXT_EOF)`) so a backtick or `$(` in it stays literal. The `draft` posture adds `--draft`, which
  prefixes the title with `WIP:` (Forgejo's draft marker). `tea pr edit <index> --ready` strips that
  prefix.
- **Read**: `tea pr <index> --login <login> --repo <owner>/<repo> -o json`.
- **Report**: the URL `tea pr create` prints.

## Degrade when no tea login

Do not fall back to a token or a raw HTTP call (curl). No `tea` or `jq` on the path, or no `tea login list -o json` entry
whose `url` equals the Base URL: push the branch, then print the instance compare URL
(`<base-url>/<owner>/<repo>/compare/<base>...<scope-branch>`) and the exact `tea` command the step would
have run. The merge call below degrades the same way: print the exact `tea api` call and merge nothing.

## Merge

- **Read the pull request**: `tea api -i '/repos/{owner}/{repo}/pulls/<index>'` with `--login <login>
  --repo <owner>/<repo>` → `state`, `merged`, `merge_commit_sha` (once merged), `head.sha`, and
  `mergeable`. `tea pr <index> -o json` lacks `merge_commit_sha`, so this read is a `tea api` call.
- **Merge it, pinned**: `jq -n --arg t "$title" --arg sha "<tested-tip-sha>"
  '{Do:"squash",MergeTitleField:$t,head_commit_id:$sha}'` piped to `tea api -i -X POST
  '/repos/{owner}/{repo}/pulls/<index>/merge' -d @-`, `title` set from a quoted heredoc as the PR-create
  line does, so a quote or backtick in the title stays literal in the JSON.
  `head_commit_id` pins the merge to that sha: the host answers 409 when the head moved since. `tea pr
  merge` carries no head-pin, so this call is the merge. Never send `force_merge` — that field overrides
  branch protection and required approvals, and this seed never sets it.
- **A refusal names the reason**: the status is on stderr (`-i`). 405 means the pull request cannot be
  merged (an unmet required approval or protection rule, or it is already merged/closed); 409 means the
  head moved since it was read, or a conflict exists. Relay the host's own message, and merge nothing.
- **Find an open pull request by its head branch** (a fallback, when no index is on hand):
  `tea pr list --state open -o json --fields index,head --page <p> --limit 50` — the listing carries no
  head filter, so filter the returned array client-side by `head` matching the scope branch. The list
  paginates: page through every page, until one returns fewer than 50 entries, before counting matches, or
  a match sitting on a later page reads as zero. Leaving
  `--state open` off would surface closed and merged pull requests too, including an old one left over from
  a reused branch name — never widen the filter to catch one of those.
- **`<tested-tip-sha>` above is never read from the host.** It is the local scope-branch tip
  `scope.js suite reusable` just accepted, resolved with `git rev-parse <branch>` in the scope checkout.
  `/superdev:ship` reads the pull request's own `head.sha` only to decide whether to push and to refuse
  when it still differs from that tip after pushing, never as the merge's own pin.

## Edit the pull request body

A run that resolves a deferred finding after `/superdev:submit-pr` opened the pull request rebuilds that
pull request's `## Deferred to a plan` section (`references/scope-findings.md`'s `## Deferred findings`,
ADR 0121). Only an open pull request is edited.

- **Read it**: `tea api '/repos/{owner}/{repo}/pulls/<index>'` with `--login <login> --repo <owner>/<repo>`,
  into a file → `state`, `merged`, and the markdown `body`.
- **Edit it, by file**: read into one file, build the new body into a second, wrap it as JSON in a third,
  and edit only when every step succeeded: `tea api '/repos/{owner}/{repo}/pulls/<index>' > "$r" &&
  jq -e '.state == "open" and (.merged | not)' "$r" >/dev/null && jq -je .body "$r" > "$b" &&
  <producer> < "$b" > "$f" && [ -s "$f" ] && jq -n --rawfile body "$f" '{body: $body}' > "$j" &&
  tea api -i -X PATCH '/repos/{owner}/{repo}/pulls/<index>' -d @"$j"`, with the same `--login` and
  `--repo`. Name each file with `mktemp`. `<producer>` builds the new body from `"$b"`. A failed read, a
  merged or closed pull request, a failed producer, or an empty result stops the chain before the edit,
  and the body stays as it was. Read the HTTP status from stderr (`-i`): `tea api` exits 0 on a 4xx or
  5xx answer.

## Compute the base

A scope's BASE for the final-reviewer's BASE..HEAD diff is its range base, computed by `scope.js base resolve`
per `${CLAUDE_PLUGIN_ROOT}/references/scope-mode.md`'s `## The scope's range base`. An ad-hoc range with no run
root uses `git merge-base <base> <scope-branch>`.
