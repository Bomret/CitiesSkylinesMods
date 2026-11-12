# Cities: Skylines Mods

A monorepo of C# mods for Cities: Skylines. The game runs on Windows and macOS only. The mods target .NET
Framework 3.5 and compile against the game's managed assemblies. Each mod has a `GameMod` class that
implements the game's `IUserMod` interface, which is the entry point the game loads.

## Structure

- `mods/NaturalLighting`: the Natural Lighting mod. Each effect is a `Feature<ModSettings>` subclass in
  `Features/`. The settings are in `Settings/` and the translations are in `Assets/Locales/`.
  The prebuilt shader bundles are in `Assets/Shaders/<Windows|macOS>/`.
- `mods/RemoveChirper`: the Remove Chirper mod. It consists of a single `GameMod` class.
- `libs/Common`: the shared library. It holds the `Feature<TSettings>` base class, which defines the
  lifecycle of one effect. It also holds the settings store, the service provider, the translations, and
  the providers for shaders, color lookup tables (LUTs), and mods.
- `unity/NaturalLighting`: the Unity 5.6.7f1 project that compiles the shaders into the bundles.
- `docs/agents/`: the superdev configuration files that the sections below index.
- `docs/invariants.md`: the register of rules the mods must never break.

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
mise (`mise.toml`) for the Node.js and .NET SDK versions and the tasks, .NET
(`dotnet build CitiesSkylinesMods.slnx`), per-mod `publish:<mod>` and `release:<mod>` mise tasks, npm for
the release and commit-hook dev dependencies, Unity 5.6.7f1 for the shader bundle. See `docs/agents/toolchain.md`.

### Verification
No test suite. The verification command is `dotnet build CitiesSkylinesMods.slnx`. See
`docs/agents/verification.md`.

### Deployment
Posture `manual`, 2 targets (`naturallighting`, `removechirper`). See `docs/agents/deployment.md`.

## Invariants

`docs/invariants.md` holds the rules the mods must never break. Read it before you change how a feature
loads, unloads, or finds scene objects.
