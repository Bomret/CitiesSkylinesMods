<!-- omit in toc -->

# Contributing to Cities Skylines Mods

First off, thanks for taking the time to contribute! ❤️

All types of contributions are encouraged and valued. See the [Table of Contents](#table-of-contents) for different ways to help and details about how this project handles them. Please make sure to read the relevant section before making your contribution. It will make it a lot easier for us maintainers and smooth out the experience for all involved. The community looks forward to your contributions. 🎉

> And if you like the project, but just don't have time to contribute, that's fine. There are other easy ways to support the project and show your appreciation, which we would also be very happy about:
>
> - Star the project
> - Tweet about it
> - Refer this project in your project's readme
> - Mention the project at local meetups and tell your friends/colleagues

<!-- omit in toc -->

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [I Have a Question](#i-have-a-question)
- [I Want To Contribute](#i-want-to-contribute)
- [Reporting Bugs](#reporting-bugs)
- [Suggesting Enhancements](#suggesting-enhancements)
- [Your First Code Contribution](#your-first-code-contribution)
- [Improving The Documentation](#improving-the-documentation)
- [Styleguides](#styleguides)
- [Commit Messages](#commit-messages)
- [Join The Project Team](#join-the-project-team)

## Code of Conduct

This project and everyone participating in it is governed by the
[Cities Skylines Mods Code of Conduct](https://github.com/Bomret/CitiesSkylinesModsblob/master/CODE_OF_CONDUCT.md).
By participating, you are expected to uphold this code. Please report unacceptable behavior
to .

## I Have a Question

> If you want to ask a question, we assume that you have read the available [Documentation]().

Before you ask a question, it is best to search for existing [Issues](https://github.com/Bomret/CitiesSkylinesMods/issues) that might help you. In case you have found a suitable issue and still need clarification, you can write your question in this issue. It is also advisable to search the internet for answers first.

If you then still feel the need to ask a question and need clarification, we recommend the following:

- Open an [Issue](https://github.com/Bomret/CitiesSkylinesMods/issues/new).
- Provide as much context as you can about what you're running into.
- Provide project and platform versions (nodejs, npm, etc), depending on what seems relevant.

We will then take care of the issue as soon as possible.

## I Want To Contribute

> ### Legal Notice <!-- omit in toc -->
>
> When contributing to this project, you must agree that you have authored 100% of the content, that you have the necessary rights to the content and that the content you contribute may be provided under the project license.

### Reporting Bugs

<!-- omit in toc -->

#### Before Submitting a Bug Report

A good bug report shouldn't leave others needing to chase you up for more information. Therefore, we ask you to investigate carefully, collect information and describe the issue in detail in your report. Please complete the following steps in advance to help us fix any potential bug as fast as possible.

- Make sure that you are using the latest version.
- Determine if your bug is really a bug and not an error on your side e.g. using incompatible environment components/versions (Make sure that you have read the [documentation](). If you are looking for support, you might want to check [this section](#i-have-a-question)).
- To see if other users have experienced (and potentially already solved) the same issue you are having, check if there is not already a bug report existing for your bug or error in the [bug tracker](https://github.com/Bomret/CitiesSkylinesModsissues?q=label%3Abug).
- Also make sure to search the internet (including Stack Overflow) to see if users outside of the GitHub community have discussed the issue.
- Collect information about the bug:
- Stack trace (Traceback)
- OS, Platform and Version (Windows or macOS, x86, ARM - Cities: Skylines is only available on Windows and macOS)
- Version of the interpreter, compiler, SDK, runtime environment, package manager, depending on what seems relevant.
- Possibly your input and the output
- Can you reliably reproduce the issue? And can you also reproduce it with older versions?

<!-- omit in toc -->

#### How Do I Submit a Good Bug Report?

> You must never report security related issues, vulnerabilities or bugs including sensitive information to the issue tracker, or elsewhere in public. Instead sensitive bugs must be sent by email to dev-csl@bomret.com.

We use GitHub issues to track bugs and errors. If you run into an issue with the project:

- Open an [Issue](https://github.com/Bomret/CitiesSkylinesMods/issues/new). (Since we can't be sure at this point whether it is a bug or not, we ask you not to talk about a bug yet and not to label the issue.)
- Explain the behavior you would expect and the actual behavior.
- Please provide as much context as possible and describe the _reproduction steps_ that someone else can follow to recreate the issue on their own. This usually includes your code. For good bug reports you should isolate the problem and create a reduced test case.
- Provide the information you collected in the previous section.

Once it's filed:

- The project team will label the issue accordingly.
- A team member will try to reproduce the issue with your provided steps. If there are no reproduction steps or no obvious way to reproduce the issue, the team will ask you for those steps and mark the issue as `needs-repro`. Bugs with the `needs-repro` tag will not be addressed until they are reproduced.
- If the team is able to reproduce the issue, it will be marked `needs-fix`, as well as possibly other tags (such as `critical`), and the issue will be left to be [implemented by someone](#your-first-code-contribution).

### Suggesting Enhancements

This section guides you through submitting an enhancement suggestion for Cities Skylines Mods, **including completely new features and minor improvements to existing functionality**. Following these guidelines will help maintainers and the community to understand your suggestion and find related suggestions.

<!-- omit in toc -->

#### Before Submitting an Enhancement

- Make sure that you are using the latest version.
- Read the [documentation]() carefully and find out if the functionality is already covered, maybe by an individual configuration.
- Perform a [search](https://github.com/Bomret/CitiesSkylinesMods/issues) to see if the enhancement has already been suggested. If it has, add a comment to the existing issue instead of opening a new one.
- Find out whether your idea fits with the scope and aims of the project. It's up to you to make a strong case to convince the project's developers of the merits of this feature. Keep in mind that we want features that will be useful to the majority of our users and not just a small subset. If you're just targeting a minority of users, consider writing an add-on/plugin library.

<!-- omit in toc -->

#### How Do I Submit a Good Enhancement Suggestion?

Enhancement suggestions are tracked as [GitHub issues](https://github.com/Bomret/CitiesSkylinesMods/issues).

- Use a **clear and descriptive title** for the issue to identify the suggestion.
- Provide a **step-by-step description of the suggested enhancement** in as many details as possible.
- **Describe the current behavior** and **explain which behavior you expected to see instead** and why. At this point you can also tell which alternatives do not work for you.
- You may want to **include screenshots and animated GIFs** which help you demonstrate the steps or point out the part which the suggestion is related to. You can use [this tool](https://www.cockos.com/licecap/) to record GIFs on macOS and Windows, and [this tool](https://github.com/colinkeenan/silentcast) or [this tool](https://github.com/GNOME/byzanz) on Linux.
- **Explain why this enhancement would be useful** to most Cities Skylines Mods users. You may also want to point out the other projects that solved it better and which could serve as inspiration.

### Your First Code Contribution

If you want to contribute to this repository, please [fork it](https://github.com/Bomret/CitiesSkylinesMods/fork), apply your features or fixes and send in a Pull Request.

You can use either [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) to develop. The latter can be used to contribute to this repository on Windows, macOS and Linux. If you use Visual Studio Code, you should install all the extensions that will be recommended by VS Code for an optimal development experience.

**Note:** While development can be done on any platform, Cities: Skylines is only available on Windows and macOS, so mods can only be tested on these platforms.

The CLI build system is based on [Node](https://nodejs.org/en) and [Turbo](https://turbo.build/repo).

#### Folders

- **mods**: Source code for all my mods.
- **libs**: Shared class libraries used by the mods.

#### Prerequisites for Visual Studio Code

> ⚠️ If you're using Visual Studio on Windows, you don't need these prerequisites. Just make sure you have .NET Framework 3.5 installed on your machine.

Make sure the following Software Development Kits and utility applications are installed on your system or use the provided [Dev Container](https://containers.dev/) in Visual Studio Code.

- [Node LTS](https://nodejs.org/en)
- [.NET SDK LTS](https://dotnet.microsoft.com/en-us/download)
- [Powershell](https://learn.microsoft.com/de-de/powershell/scripting/install/installing-powershell)
- [Git](https://git-scm.com/downloads)

##### Initial Development Setup

- execute `./setup-dev.ps1` in the root dir of the repository

#### Turbo Repo

- Turbo is an incremental bundle and build system
- for the central management of all projects in the [Monorepo] (https://en.wikipedia.org/wiki/Monorepo)
- [Documentation] (https://turbo.build/repo)

#### npm Scripts

- `npm run build`
  - Compiles all projects
- `npm run publish`
  - Compiles all projects in Release mode and packs them neatly into .zip files in a `publish` folder inside the project's folder (requires [7-zip](https://7-zip.org/) to be installed).

### Improving The Documentation

Please open an [Issue](https://github.com/Bomret/CitiesSkylinesMods/issues) on GitHub or send in a Pull Request.

## Styleguides

### Commit Messages

This monorepo uses [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) for automatic changelog generation and versioning. If your commit message does not conform to this, `git` will tell you and reject it.

<!-- omit in toc -->

## Attribution

This guide is based on the **contributing-gen**. [Make your own](https://github.com/bttger/contributing-gen)!
