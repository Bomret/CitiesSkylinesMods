# Knowledge base: Codeberg wiki

The reader-facing library for this repository: player guides and developer docs, kept in the wiki of
`https://codeberg.org/bomret/CitiesSkylinesMods`. This is distinct from the in-repo `AGENTS.md` docs in
`docs/agents/documentation.md`: those are the agent-facing contract; this wiki is for human readers.

## Configuration

- **Backend:** Codeberg wiki (Forgejo), reached through the `tea` CLI only.
- **Host / owner / repo:** `https://codeberg.org` · `bomret` / `CitiesSkylinesMods`.
- **Login:** resolved, never stored: the one `tea login list -o json` entry whose `url` equals the Host.
  The commands below pass `--login codeberg.org`, the name of that entry. No token env var, header, or
  raw HTTP call (curl) appears in this document.
- Page bodies are markdown.

## Audience, language & outline

The wiki is **human-consumption first**.

- **Audience(s):** players, developers.
- **Language:** English. All wiki prose is written in English.
- **Outline** (living skeleton; `/superdev:plan` and `/superdev:document` fill and extend it):

  - `Home` (aggregate; sections are `## <page group>`) — the index of every page below.
  - Player Guide → `Natural Lighting` (settings, compatibility), `Remove Chirper`, `Troubleshooting`
  - Developer Docs → `Architecture`, `Shader pipeline`, `Build and release`, `Design decisions`
    (aggregate; sections are `## <decision>`)

The wiki has no page hierarchy. A page's place in the outline is its entry in `Home`, never a parent
page.

## Staging

The wiki is a **remote** backend, so nothing but `/superdev:publish-kb` calls it (ADR 0023).
`/superdev:plan` and `/superdev:document` write each page as a **staged page** under
`.superdev/kb-pending/<scope>/` in the main working tree, resolved by `scope.js staging <scope>`
(ADR 0119): YAML frontmatter carrying the page `title` and an `action` of `upsert`, `patch`, or `delete`,
the page body below. An aggregate page is staged one section at a time (`action: patch`) and merged into
the live page. A page reaches the wiki when a human runs `/superdev:publish-kb`, after the feature has
reached the readers this wiki is written for.

## Commands

The **backend command contract** (ADR 0069) `scripts/publish-kb.js` runs. `find`'s JSON keys reach
`read`, `update`, and `delete` as `SUPERDEV_KB_NAME` (the page's URL-escaped name) and `SUPERDEV_KB_SHA`
(its last commit). `create` and `update` also receive `SUPERDEV_KB_BODY_FILE`, a path to the staged
markdown body. Every command reads the HTTP status from `tea api -i`'s stderr, because `tea api` exits 0
on a 4xx or 5xx answer.

`find` exits 3 only on a 404 for the page, the one outcome `scripts/publish-kb.js` reads as positive
absence. Any status other than 200 or 404 exits 2, "could not answer."

Every command below runs under a **30-second bound** (ADR 0130), or whatever
`SUPERDEV_PUBLISH_KB_TIMEOUT_MS` names. `scripts/publish-kb.js` applies it at the spawn. A command killed
at the bound leaves its page staged and **stops the run**.

### find

```sh
name=$(jq -rn --arg t "$SUPERDEV_KB_TITLE" '$t | @uri') || exit 2
out=$(mktemp) && err=$(mktemp) || exit 2
tea api -i --login codeberg.org --repo bomret/CitiesSkylinesMods "/repos/{owner}/{repo}/wiki/page/$name" >"$out" 2>"$err" || exit 2
status=$(sed -n '1s/^HTTP\/[0-9.]* \([0-9][0-9][0-9]\).*/\1/p' "$err")
[ "$status" = 404 ] && exit 3
[ "$status" = 200 ] || exit 2
jq -ce '{name: .sub_url, sha: .last_commit.sha}' "$out" || exit 2
```

### read

```sh
out=$(mktemp) && err=$(mktemp) || exit 2
tea api -i --login codeberg.org --repo bomret/CitiesSkylinesMods "/repos/{owner}/{repo}/wiki/page/$SUPERDEV_KB_NAME" >"$out" 2>"$err" || exit 2
[ "$(sed -n '1s/^HTTP\/[0-9.]* \([0-9][0-9][0-9]\).*/\1/p' "$err")" = 200 ] || exit 2
content=$(jq -er '.content_base64' "$out") || exit 2
printf '%s' "$content" | base64 -d
```

### create

```sh
j=$(mktemp) && err=$(mktemp) || exit 2
jq -n --arg t "$SUPERDEV_KB_TITLE" --rawfile c "$SUPERDEV_KB_BODY_FILE" \
  '{title: $t, content_base64: ($c | @base64), message: ("docs: create " + $t)}' > "$j" || exit 2
tea api -i -X POST --login codeberg.org --repo bomret/CitiesSkylinesMods '/repos/{owner}/{repo}/wiki/new' -d @"$j" >/dev/null 2>"$err" || exit 2
[ "$(sed -n '1s/^HTTP\/[0-9.]* \([0-9][0-9][0-9]\).*/\1/p' "$err")" = 201 ] || exit 2
```

### update

```sh
j=$(mktemp) && err=$(mktemp) || exit 2
jq -n --arg t "$SUPERDEV_KB_TITLE" --rawfile c "$SUPERDEV_KB_BODY_FILE" \
  '{title: $t, content_base64: ($c | @base64), message: ("docs: update " + $t)}' > "$j" || exit 2
tea api -i -X PATCH --login codeberg.org --repo bomret/CitiesSkylinesMods "/repos/{owner}/{repo}/wiki/page/$SUPERDEV_KB_NAME" -d @"$j" >/dev/null 2>"$err" || exit 2
[ "$(sed -n '1s/^HTTP\/[0-9.]* \([0-9][0-9][0-9]\).*/\1/p' "$err")" = 200 ] || exit 2
```

### delete

```sh
err=$(mktemp) || exit 2
tea api -i -X DELETE --login codeberg.org --repo bomret/CitiesSkylinesMods "/repos/{owner}/{repo}/wiki/page/$SUPERDEV_KB_NAME" >/dev/null 2>"$err" || exit 2
[ "$(sed -n '1s/^HTTP\/[0-9.]* \([0-9][0-9][0-9]\).*/\1/p' "$err")" = 204 ] || exit 2
```

## superdev usage

- `/superdev:plan` — seeds feature specifications and design decisions into the outline and stages their
  pages.
- `/superdev:document` — reconciles and extends the wiki (via its doc-writer) and stages the pages it
  changed, after a scope lands and on demand.
- `/superdev:publish-kb` — sends the staged pages to the wiki. The only command that calls it.

## Degrade / switching

No `tea` login for the Host, or the host unreachable → `/superdev:publish-kb` leaves every page staged,
reports why, and deletes nothing. A later run retries exactly the pages that did not land. The in-repo
docs are the record meanwhile. To move to a different knowledge base later, re-run `/superdev:setup`.
