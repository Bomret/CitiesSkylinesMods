# Cities Skylines Mods

[![Test](https://github.com/Bomret/CitiesSkylinesMods/actions/workflows/test.yml/badge.svg?branch=main)](https://github.com/Bomret/CitiesSkylinesMods/actions/workflows/test.yml)

This repository contains the source code of my mods for Cities: Skylines by Colossal Order.

## Content

+ **mods**: Source code for all my mods.
+ **libs**: Shared class libraries used by the mods.

## Development

This is a monorepo containing the source code for all my mods and their dependencies. The build system is based on [Node](https://nodejs.org/en) and [Turbo](https://turbo.build/repo).

You can use either [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) to develop. The latter can be used to contribute to this repository on Windows, macOS and Linux. If you use Visual Studio Code, you should install all the extensions that will be recommended by VS Code for an optimal development experience.

### Prerequisites

Make sure the following Software Development Kits and utility applications are installed on your system or use the provided [Dev Container](https://containers.dev/) in Visual Studio Code.

- [Node LTS](https://nodejs.org/en)
- [.NET SDK LTS](https://dotnet.microsoft.com/en-us/download)
- [Powershell](https://learn.microsoft.com/de-de/powershell/scripting/install/installing-powershell)
- [Git](https://git-scm.com/downloads)

### Initial Development Setup

- execute `./setup-dev.ps1` in the root dir of the repository

### Turbo Repo

- Turbo is an incremental bundle and build system
- for the central management of all projects in the [Monorepo] (https://en.wikipedia.org/wiki/Monorepo)
- [Documentation] (https://turbo.build/repo)

### Turbo Befehle

- `npx turbo publish`
    - kompiliert alle Module/Anwendungen, liest ihre Abhängigkeiten, die in den Projektdateien angegeben sind, und
      veröffentlicht die resultierenden Dateien in einem 'publish' Verzeichnis
- `npx turbo publish --filter 'name_der_anwendung_in_package.json'`
    - kompiliert die Anwendung, liest ihre Abhängigkeiten, die in der Projektdatei angegeben sind, und veröffentlicht
      die resultierenden Dateien in einem `publish`Verzeichnis

# Hilfreiche Links

- [Git erklärt in 100 Sekunden](https://www.youtube.com/watch?v=hwP7WQkmECE)
- [Conventional Commits: Was muss ich beachten wenn ich eine Commit Nachricht verfassse](https://www.conventionalcommits.org/en/v1.0.0/)