# Toolchain

## `.` — mise tasks + .NET solution

- Tool and task runner: [mise](https://mise.jdx.dev/), configured in `mise.toml`. Its `[tools]` pin
  Node.js (`lts`) and the .NET SDK (`10`); `global.json` pins the SDK feature band for `dotnet`. Its
  `[env]` puts `node_modules/.bin` on `PATH` for every task.
- npm (bundled with Node.js) installs the root `package.json` dev dependencies (semantic-release,
  commitlint, husky) from `package-lock.json`. The repository has no npm workspaces; the `mods/*/package.json`
  files carry only the name and version that semantic-release reads and bumps.
- Commands:
  - Tools — `mise install`. Installs Node.js and the .NET SDK. Enough for build and publish.
  - Setup — `mise run setup`: `npm ci` (its `prepare` script installs the husky hook), `dotnet restore`,
    `dotnet tool restore`. Needed for commits (commitlint hook) and releases.
  - Build — `dotnet build CitiesSkylinesMods.slnx`. Needs Cities: Skylines installed through Steam: the
    mods reference the game's managed assemblies from its install path (`mods/Directory.Build.props`).
  - Tests — none; this repository has no test suite. The verification command is the build, see
    `docs/agents/verification.md`.
- Lint: `Microsoft.Extensions.StaticAnalysis` analyzers run on every build (`Directory.Build.props`,
  version in `Directory.Packages.props`). Code style rules: `.editorconfig`.
- Commit messages: commitlint with `@commitlint/config-conventional`, run by the husky `commit-msg` hook.
- Style prose: `CONTRIBUTING.md` §Styleguides (commit messages).

## `mods/*` — C# mods (net35)

One folder per mod (`NaturalLighting`, `RemoveChirper`), package names `naturallighting` and
`removechirper`. Each mod has a `publish:<package>` and a `release:<package>` task in `mise.toml`.

- Commands:
  - Publish — `mise run publish:<package>`: `dotnet publish` in Release to `./publish`, then the
    `DeployToLocalModDirectory` target copies the build into the game's local mods directory.
  - Release — `mise run release:<package>`: runs `setup`, then semantic-release with
    `semantic-release-monorepo` (`release.config.cjs`). It bumps the version in `package.json` and the
    `.csproj`, runs `mise run publish:<package>`,
    writes `CHANGELOG.md`, and commits. Its last plugin is `@semantic-release/github`, which publishes the
    GitHub release on `origin`.
  - Choose between them: run `publish` to test a build in the game, and run `release` only to ship a
    version. `release` creates a tagged commit and a GitHub release on `origin`, so it needs push access to
    the repository. Players receive the mod only after the manual Steam Workshop upload that follows.

## `libs/Common` — shared C# library

Feature lifecycle, settings store, shader/LUT loading, translations. Built as part of the solution; no
commands of its own.

## `unity/NaturalLighting` — Unity 5.6.7f1 project

Holds the shader sources (`Assets/Shaders/*.shader`) for the NaturalLighting mod.

- Shader bundle build — Unity editor menu **NaturalLighting**, entry **Build Natural Lighting Shader Bundle (All Platforms)**, or the entry
  **Build Natural Lighting Shader Bundle (Current Platform Only)**
  (`Assets/Editor/SunShaftsBuilder.cs`). Output: `Assets/StreamingAssets/<Windows|macOS>/naturallighting`.
  Copy each bundle by hand to `mods/NaturalLighting/Assets/Shaders/<Windows|macOS>/`.
