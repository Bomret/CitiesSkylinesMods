# Cities: Skylines Mods

A monorepo of C# mods for Cities: Skylines (Windows and macOS only), built for .NET Framework 3.5 against
the game's managed assemblies.

## Structure

- `mods/NaturalLighting`: the Natural Lighting mod. One `Feature<ModSettings>` subclass per effect in
  `Features/`, the settings in `Settings/`, translations in `Assets/Locales/`, and the prebuilt shader
  bundles in `Assets/Shaders/<Windows|macOS>/`.
- `mods/RemoveChirper`: the Remove Chirper mod, a single `GameMod` class.
- `libs/Common`: the code both mods share: the `Feature<TSettings>` lifecycle base class, the settings
  store, the shader, LUT, and mod providers, the service provider, and translations.
- `unity/NaturalLighting`: the Unity 5.6.7f1 project that compiles the shaders into the bundles.
- `docs/agents/`: the superdev configuration files listed below. `docs/invariants.md`: the invariant
  register.

## Where to read

- Build, publish, and release commands: `docs/agents/toolchain.md`.
- Contribution rules and the commit message format: `CONTRIBUTING.md`.
- What each mod does for players: `mods/*/README.md` and `mods/*/SteamWorkshop.md`.
- The published reader documentation: the GitHub wiki, whose outline is in `docs/agents/knowledge-base.md`.

## Agent skills

### Slice store
GitHub Issues on `bomret/CitiesSkylinesMods`, through `gh`. See `docs/agents/issue-tracker.md`.

### Code host
GitHub (`bomret/CitiesSkylinesMods`) through `gh`, base branch `main`, pull requests open as drafts.
See `docs/agents/code-management.md`.

### Documentation
In-repo `AGENTS.md` hierarchy, written to the controlled writing standard. See
`docs/agents/documentation.md`.

### Knowledge base
GitHub wiki for players and developers, in English, sent only by `/superdev:publish-kb`. See
`docs/agents/knowledge-base.md`.

### Toolchain
pnpm workspace plus .NET (`dotnet build CitiesSkylinesMods.slnx`), per-mod `publish` and `release`
scripts, Unity 5.6.7f1 for the shader bundle. See `docs/agents/toolchain.md`.

### Verification
No test suite. The verification command is `dotnet build CitiesSkylinesMods.slnx`. See
`docs/agents/verification.md`.

### Deployment
Posture `manual`, 2 targets (`naturallighting`, `removechirper`). See `docs/agents/deployment.md`.

## Invariants

`docs/invariants.md` holds the rules the mods must never break. Read it before you change how a feature
loads, unloads, or finds scene objects.
