#!/usr/bin/env pwsh

. "$PSScriptRoot/lib.ps1"

Log "Setting up development environment"

Log "Installing packages"
SubLog "Installing npm packages"
Run npm install

SubLog "Installing .NET packages"
Run dotnet restore --locked-mode

SubLog "Installing .NET tools"
Run dotnet tool restore