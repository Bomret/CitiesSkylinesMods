# Toolchain

## `.` — pnpm workspace + .NET solution

- Package manager: pnpm 10.33.0 (`packageManager` in `package.json`, lockfile `pnpm-lock.yaml`,
  workspace `pnpm-workspace.yaml`: `apps/*`, `libs/*`, `mods/*`). A `package-lock.json` is also present.
- Runtimes: Node.js pinned by `.nvmrc`, .NET SDK pinned by `global.json`. `setup-dev.ps1` (PowerShell 7)
  installs both into user-local directories.
- Commands:
  - Install — `pnpm install`. Its `postinstall` runs `dotnet restore && dotnet tool restore`.
  - Build — `dotnet build CitiesSkylinesMods.slnx`. Needs Cities: Skylines installed through Steam: the
    mods reference the game's managed assemblies from its install path (`mods/Directory.Build.props`).
  - Tests — none; this repository has no test suite.
- Lint: `Microsoft.Extensions.StaticAnalysis` analyzers run on every build (`Directory.Build.props`,
  version in `Directory.Packages.props`). Code style rules: `.editorconfig`.
- Commit messages: commitlint with `@commitlint/config-conventional`, run by the husky `commit-msg` hook.
- Style prose: `CONTRIBUTING.md` §Styleguides (commit messages).

## `mods/*` — C# mods (net35)

One workspace per mod (`NaturalLighting`, `RemoveChirper`), package names `naturallighting` and
`removechirper`.

- Commands:
  - Publish — `pnpm --filter <package> run publish`: `dotnet publish` in Release to `./publish`, then the
    `DeployToLocalModDirectory` target copies the build into the game's local mods directory.
  - Release — `pnpm --filter <package> run release`: semantic-release with `semantic-release-monorepo`
    (`release.config.cjs`). It bumps the version in `package.json` and the `.csproj`, runs `publish`,
    writes `CHANGELOG.md`, and commits. Its last plugin is `@semantic-release/github`, while `origin` is
    Codeberg.

## `libs/Common` — shared C# library

Feature lifecycle, settings store, shader/LUT loading, translations. Built as part of the solution; no
commands of its own.

## `unity/NaturalLighting` — Unity 5.6.7f1 project

Holds the shader sources (`Assets/Shaders/*.shader`) for the NaturalLighting mod.

- Shader bundle build — Unity editor menu **NaturalLighting → Build Natural Lighting Shader Bundle**
  (`Assets/Editor/SunShaftsBuilder.cs`). Output: `Assets/StreamingAssets/<Windows|macOS>/naturallighting`.
  Copy each bundle by hand to `mods/NaturalLighting/Assets/Shaders/<Windows|macOS>/`.
