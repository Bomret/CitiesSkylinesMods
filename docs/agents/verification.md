# Verification

```sh
dotnet build CitiesSkylinesMods.slnx
```

```sh
dotnet build CitiesSkylinesMods.slnx
```

**No test suite, so the build is the check, and there is no cheaper focused command.** The mods run only
inside Cities: Skylines, and Unity's engine semantics have no test seam outside the game, so the
repository holds no tests. The solution build compiles every mod and runs the
`Microsoft.Extensions.StaticAnalysis` analyzers, which is the only automated check. It needs the game's
managed assemblies (`mods/Directory.Build.props`). Behavior is verified by hand in game.
