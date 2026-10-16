# Cities: Skylines Mods

## Agent skills

### Slice store
Codeberg Issues on `bomret/CitiesSkylinesMods`, through `tea`. See `docs/agents/issue-tracker.md`.

### Code host
Codeberg (`bomret/CitiesSkylinesMods`) through `tea`, base branch `main`, pull requests open as drafts.
See `docs/agents/code-management.md`.

### Documentation
In-repo `AGENTS.md` hierarchy, written to the controlled writing standard. See
`docs/agents/documentation.md`.

### Knowledge base
Codeberg wiki for players and developers, in English, sent only by `/superdev:publish-kb`. See
`docs/agents/knowledge-base.md`.

### Toolchain
pnpm workspace plus .NET (`dotnet build CitiesSkylinesMods.slnx`), per-mod `publish` and `release`
scripts, Unity 5.6.7f1 for the shader bundle. See `docs/agents/toolchain.md`.

### Verification
None: the repository has no test suite.

### Deployment
Posture `manual`, 2 targets (`naturallighting`, `removechirper`). See `docs/agents/deployment.md`.

## Invariants

`docs/invariants.md` holds the rules the mods must never break. Read it before you change how a feature
loads, unloads, or finds scene objects.
