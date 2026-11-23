# Slice store plan writes: GitHub Issues

The calls `/superdev:plan` makes on GitHub Issues and no other command makes: creating a plan's issues,
writing the anchor lines, and extending a feature. `/superdev:setup` writes this seed to
`docs/agents/issue-tracker-plan.md`, filled in with the values of `docs/agents/issue-tracker.md`, whose
calls (**Edit an issue's body**, **Read a slice**, **Create a missing label**) this document uses.

## Plan-only fields

- **Parked questions**, **Non-goals**, and **Epic visuals** (all optional) ↔ `## Parked questions`,
  `## Non-goals`, and `## Slice graph`/`## Touch map` sections in the epic issue body, the same body-text
  pattern as `acceptance`. They are epic-only and advisory: `/implement` never reads them, and a
  single-slice feature's lone issue has no equivalent. GitHub renders mermaid in issue bodies.
  `plan-authoring.md` owns their shape and when each is written.

## Creating issues

- **Labels exist first.** Before the first issue that carries a label, run **Create a missing label** for
  the feature label, the release label (`released`), the status labels (`ready`, `in-progress`,
  `in-review`, `blocked`), the `behavior`/`mechanical`/`docs` set, and `epic`. Labels are addressed by
  name, so nothing else catches a missing one.
- **Two identifiers per issue.** An issue carries a **`number`** (the repo-scoped `#<n>`, the slice's `id`,
  what `#<epic>` references) and an **`id`** (the global database id). Capture both from the create
  response. The relation calls mix them:

  | Call | Path takes | Body takes |
  |---|---|---|
  | `POST .../issues/<epic-number>/sub_issues` | the epic's **`number`** | `sub_issue_id` = the slice's **`id`** |
  | `POST .../issues/<blocked-number>/dependencies/blocked_by` | the blocked slice's **`number`** | `issue_id` = the blocker's **`id`** |

  A `number` passed where an `id` is expected 404s or links the wrong issue.
- **Create an issue:** `POST /repos/bomret/CitiesSkylinesMods/issues` with
  `{ "title": "...", "body": "<specification + acceptance checklist>", "labels": ["<label>", …],
  "assignees": ["<assignee>"] }`, labels and assignees at creation, `assignees` omitted when the assignee
  resolves to `none`. With `gh`, `gh issue create` prints the issue URL: parse the `number` off it and
  fetch the `id` with **Read a slice**.
- **Link a slice to its epic (sub-issue):** `POST /repos/bomret/CitiesSkylinesMods/issues/<epic-number>/sub_issues`
  with `{ "sub_issue_id": <the slice's global id> }`. Skip it for a single-slice feature. **Best-effort:**
  log a `403`/`404` and carry on; the feature label, not the link, is how slices are found.
- **Record a dependency (`depends-on`):**
  `POST /repos/bomret/CitiesSkylinesMods/issues/<blocked-number>/dependencies/blocked_by` with
  `{ "issue_id": <the blocker's global id> }` → `201`. **Not best-effort:** `/implement` reads blockers
  from this API and cannot detect a lost edge. On any non-`201`, name every failed edge as a slice-id pair
  and end `/plan` reporting failure. Nothing is deleted or rolled back: the plan is repaired by writing the
  missing edges. A token lacking Issues write fails every edge at once; report that as the single cause.
- **Parallel-safe creation order:** create the epic first, then all slice creates in parallel, then the
  sub-issue links and the dependency writes in parallel once every slice's `number` and `id` are known.

## Anchor lines

Each is a body edit of the **anchor issue's own** body through **Edit an issue's body**, never a native
relation, so the slice-level and feature-level relations never collide on one issue.

- **Record an anchor dependency:** append `**Blocked by feature:** #<blocker-anchor>`.
- **Record the deploy line:** append `**Deploy:** <target>[, <target>]` (in the operator's answer order)
  or `**Deploy:** none`, revising the line in place on an append that re-answers the deploy question.
- **Record the Repairs line:** append `**Repairs:** #<original-anchor>` when `/superdev:plan` creates a
  repair, and never edit it afterwards. Nothing is written to the original anchor.
- **Record the Absorbs line:** append `**Absorbs:** #<n>[, #<n>]` (every absorbed record's reference),
  revising the line in place on an append that absorbs more.
- **Record the Intent line:** append `**Intent:** #<n>[, #<n>]` (every seeding intent record's
  reference), revising the line in place on an append seeded from another one.

## Assignee

Operator configuration, not a slice field (`assignee.md` → *Assignee*). The identifier is a GitHub
**login**, read from `.superdev/info.json` under `issue-tracker.assignee`, and applied to the **epic and
every slice** in the create call, never a follow-up pass.

```json
{
  "issue-tracker": { "assignee": "<login>" }
}
```

- `assignees` is an **array**: send the login as `["<assignee>"]`. Never the singular `assignee` field.
- **No file, no `issue-tracker` key, or no `assignee`** → resolve the token's own identity with
  `GET /user` and use its `login`.
- **`"assignee": "none"`** → omit `assignees` entirely. No warning, no error.
- **A token without push access silently drops the assignment**, and `labels` and `milestone` with it: the
  create returns `201` with `assignees` empty. Nothing is retried or rolled back.
- **A login GitHub cannot assign rejects the whole create** with `422` (`field: assignees`, `code:
  invalid`), and no issue exists. Retry that one create with `assignees` omitted, and report it once per
  run.
- **Check the returned `labels`.** An empty `labels` array on the response means the labels did not stick,
  and those issues are unfindable by label. Report it once per run, whatever the assignee is set to.

## Definition of Done tags

`/superdev:plan` §6 writes each criterion's tag in `plan-contract.md`'s own notation (a slice `id`,
`scope`, or `docs`) when it creates the epic. Once every slice issue exists, the controller rewrites each
slice-id tag into `#<number>`, in the same parallel step as **Record a dependency**, each rewrite a body
edit through **Edit an issue's body** whose producer is
`node "${CLAUDE_PLUGIN_ROOT}/scripts/scope.js" dod-tags --map-file <path>`, the map file holding one
`<id> #<number>` pair per slice. A tag naming several slices becomes `#<number>, #<number>`, never
bracketed. The controller types no store tag itself. `dod-tags` refuses, printing nothing, on any unjudged
line whose tag `dod-line` cannot read or whose slice id the map lacks, so the gate stops the edit.

## Append mode

`/superdev:plan` can extend a feature that already has issues (`plan-authoring.md` → *Append mode*).

- **Create only new records; never edit, close, or delete a merged slice.** Append creates issues for the
  new slices alone. It never `PATCH`es or closes an issue a prior run merged, and never rewrites its
  labels, sub-issue link, or `blocked_by` dependency. The one link it adds is the former lone slice's
  parent link under a new epic (next rule). A merged issue's `number` is a satisfied `depends-on` root for a
  new one.
- **Create the epic first when none exists.** A feature whose lone issue has no sub-issue parent has no
  epic yet. Append creates the epic issue first, its body holding the lone slice's `acceptance` checklist
  as the global DoD, then links every new slice as its sub-issue. It also links the former lone slice, open
  or closed, under the new epic: `POST /repos/bomret/CitiesSkylinesMods/issues/<epic-number>/sub_issues` with
  `{ "sub_issue_id": <the lone slice's global id> }`, so the epic stays the feature's one root. That link is
  best-effort like every sub-issue link: log a failure and carry on. The lone issue is never edited, with two
  exceptions: its `**Blocked by feature:**`, `**Deploy:**`, `**Repairs:**`, `**Absorbs:**`, and
  `**Intent:**` lines **migrate to the new epic's own body** and are removed from the lone issue, and it
  gains the epic as its sub-issue parent, since the epic is the anchor now.
- **Amend the epic's own body, the one sanctioned edit.** One edit through **Edit an issue's body** folds
  the interview's new criteria into the epic's Definition of Done in place, and deletes a resolved parked
  question's line from its `## Parked questions` section.

The `number` is server-assigned, unique, and stable, so append names no continuation rule for `id`.

## When a skill says "write slices to the store"

**≥2 slices**: create the epic first (global DoD), then one issue per slice, then link each slice as a
sub-issue of the epic and write the `blocked_by` dependencies. **Exactly one slice**: skip the epic, create
only the slice issue with the global DoD folded into its `acceptance` checklist, and skip both relation
calls. Either way, when the interview named a prerequisite feature, write the anchor dependency on the
anchor: the epic, or the lone slice issue.
