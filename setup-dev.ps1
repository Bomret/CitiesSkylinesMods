#!/usr/bin/env pwsh

<#
.SYNOPSIS
  Install the repository-specified Node.js and .NET SDK versions to user-local locations.

  Requirements/behavior:
  - PowerShell 7+ (pwsh).
  - This script requires both `.nvmrc` (Node.js spec) and `global.json` (dotnet sdk.spec).
    If either file is missing the script exits with an error.
  - Node.js is installed into a user-local node directory and its bin is added to PATH.
  - The .NET SDK is installed into a user-local .dotnet directory. For prerelease SDKs
    (version containing a dash) the script derives a major.minor channel (e.g. 10.0 from
    10.0.100-rc) and installs via the dotnet install script using -Channel and -Quality preview.

  Notes:
  - Installs are performed at user scope (no admin required).
  - On macOS/Linux you can make this script executable and run it directly.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
  [switch]$SkipNode,
  [switch]$SkipDotNet
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:NodeIndexCache = $null

function Write-Info($msg) { Write-Host $msg -ForegroundColor Cyan }
function Write-Err ($msg) { Write-Host $msg -ForegroundColor Red }

# OS/Arch detection
$IsWin = $IsWindows
$IsMac = $IsMacOS
$Arch  = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture

# Repo root (folder containing this script)
$RepoRoot = $PSScriptRoot

Write-Info "OS: $([System.Runtime.InteropServices.RuntimeInformation]::OSDescription) ($Arch)"
Write-Host ""

# Validate required files early to fail fast
function Validate-RequiredFiles {
  param()

  $nvmrcPath = Join-Path $RepoRoot '.nvmrc'
  $globalJsonPath = Join-Path $RepoRoot 'global.json'

  if (-not (Test-Path $nvmrcPath)) {
    Write-Err ".nvmrc not found at $nvmrcPath. This repository requires a .nvmrc to specify the Node.js version."
    throw ".nvmrc missing"
  }

  if (-not (Test-Path $globalJsonPath)) {
    Write-Err "global.json not found at $globalJsonPath. This repository requires global.json to specify the .NET SDK version."
    throw "global.json missing"
  }
}

Validate-RequiredFiles

# Paths
if ($IsWin) {
  $UserRoot         = $env:USERPROFILE
  $NodeInstallDir   = Join-Path $env:LOCALAPPDATA 'node'
  $DotnetInstallDir = Join-Path $env:USERPROFILE '.dotnet'
} else {
  $UserRoot         = $HOME
  $NodeInstallDir   = Join-Path $HOME '.local' 'node'
  $DotnetInstallDir = Join-Path $HOME '.dotnet'
}

# Ensure base dirs
New-Item -ItemType Directory -Path $NodeInstallDir -Force | Out-Null
New-Item -ItemType Directory -Path $DotnetInstallDir -Force | Out-Null

function Add-UserPathEntry {
  param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PathToAdd
  )

  if (-not $PathToAdd) { throw 'PathToAdd is required and cannot be empty' }

  # Normalize for comparison
  $pathSep = [System.IO.Path]::PathSeparator
  $currentParts = $env:PATH -split [regex]::Escape($pathSep)

  if (-not ($currentParts | Where-Object { $_ -eq $PathToAdd })) {
    $env:PATH = ($PathToAdd + $pathSep + $env:PATH)
  }

  if ($IsWin) {
    $currentUserPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    $userParts = @()
    if ($currentUserPath) { $userParts = $currentUserPath -split ';' }
    if (-not ($userParts | Where-Object { $_ -eq $PathToAdd })) {
      $newUserPath = ($userParts + $PathToAdd) -join ';'
      [Environment]::SetEnvironmentVariable('Path', $newUserPath, 'User')
    }
  } else {
    # Persist to shell startup files
    $profileFiles = @([IO.Path]::Combine($UserRoot, '.profile'))
    if ($env:SHELL -like '*zsh*') { $profileFiles += [IO.Path]::Combine($UserRoot, '.zprofile') }
    if ($env:SHELL -like '*bash*') { $profileFiles += [IO.Path]::Combine($UserRoot, '.bash_profile') }

    foreach ($pf in $profileFiles | Select-Object -Unique) {
      if (-not (Test-Path $pf)) { New-Item -ItemType File -Path $pf -Force | Out-Null }
      # Read file content and check for the literal path to avoid Select-String binding issues
      $fileContent = Get-Content -Raw -ErrorAction SilentlyContinue -Path $pf
      $needsAdd = -not ($fileContent -and ($fileContent.IndexOf($PathToAdd, [System.StringComparison]::Ordinal) -ge 0))
      if ($needsAdd) {
        Add-Content -Path $pf -Value ""
        Add-Content -Path $pf -Value "# Added by setup-dev.ps1: $([DateTime]::Now.ToString('u'))"
        $exportLine = 'export PATH="' + $PathToAdd + ':$PATH"'
        Add-Content -Path $pf -Value $exportLine
      }
    }
  }
}

# -------------------------
# Node.js helpers
# -------------------------
function Get-NodeIndexJson {
  # Cache index.json in memory per run
  if (-not $script:NodeIndexCache) {
    $url = 'https://nodejs.org/dist/index.json'
    $script:NodeIndexCache = Invoke-RestMethod -Uri $url
  }

  return $script:NodeIndexCache
}

function Get-InstalledNodeVersion {
  try {
    $nodeCmd = Get-Command node -ErrorAction Stop
    $v = & $nodeCmd.Source -v 2>$null

    if ($v) {
      return ($v.Trim() -replace '^[vV]', '')
    }
  } catch { }

  return $null
}

function Get-LatestNodeLtsVersion {
  $index = Get-NodeIndexJson
  # First entry with .lts != false is newest LTS
  $lts = $index | Where-Object { $_.lts -ne $false } | Select-Object -First 1
  
  if (-not $lts) {
    throw "Unable to determine latest Node.js LTS from Node index"
  }

  return ($lts.version -replace '^[vV]', '')
}

function Resolve-NodeVersionFromSpec {
  param(
    [Parameter(Mandatory = $true)][string]$Spec
  )

  $spec = $Spec.Trim()
  $index = Get-NodeIndexJson

  if ($spec -match '^(?i)lts/\*$') {
    return Get-LatestNodeLtsVersion
  }

  if ($spec -match '^(?i)lts/([a-z0-9\-]+)$') {
    $code = $Matches[1]
    $cands = $index | Where-Object { $_.lts -is [string] -and ($_.lts -ieq $code) }
    if (-not $cands) { throw "No Node.js versions found for LTS codename '$code'" }
    $ver = ($cands | Sort-Object { [Version]($_.version -replace '^v','') } -Descending | Select-Object -First 1).version
    return ($ver -replace '^v','')
  }

  # Normalize leading 'v'
  if ($spec -match '^[vV]\d') {
    $spec = $spec.Substring(1)
  }

  # Exact x.y.z
  if ($spec -match '^\d+\.\d+\.\d+$') {
    return $spec
  }

  # x.y -> latest patch of that minor
  if ($spec -match '^\d+\.\d+$') {
    $prefix = $spec + '.'
    $match = $index | Where-Object { ($_.version -replace '^v','') -like "$prefix*" }
  
    if (-not $match) {
      throw "No Node.js versions found for minor '$spec'"
    }

    $ver = ($match | Sort-Object { [Version]($_.version -replace '^v','') } -Descending | Select-Object -First 1).version

    return ($ver -replace '^v','')
  }

  # x -> latest patch of that major
  if ($spec -match '^\d+$') {
    $prefix = $spec + '.'
    $match = $index | Where-Object { ($_.version -replace '^v','') -like "$prefix*" }

    if (-not $match) {
      throw "No Node.js versions found for major '$spec'"
    }

    $ver = ($match | Sort-Object { [Version]($_.version -replace '^v','') } -Descending | Select-Object -First 1).version
    
    return ($ver -replace '^v','')
  }

  throw "Unrecognized Node version spec: '$Spec'"
}

function Get-RequestedNodeSpec {
  # Preferred: .nvmrc at repo root. Fallback to latest LTS if missing.
  $nvmrc = Join-Path $RepoRoot '.nvmrc'
  

  if (Test-Path $nvmrc) {
    $content = (Get-Content -Path $nvmrc -Raw).Trim()
    
    if ($content) {
      return $content
    }
  }
  return $null
}

function Get-NodeArchiveInfo {
  param([Parameter(Mandatory = $true)][string]$Version)

  $platform = if ($IsWin) { 'win' } elseif ($IsMac) { 'darwin' } else { 'linux' }
  $archStr = switch ($Arch) {
    'X64'   { 'x64' }
    'Arm64' { 'arm64' }
    'X86'   { if ($IsWin) { 'x86' } else { throw "Unsupported 32-bit arch on $platform for Node.js" } }
    default { throw "Unsupported architecture: $Arch" }
  }
  $ext = if ($IsWin) { 'zip' } else { 'tar.xz' }

  $vTag = "v$Version"
  $file = "node-$vTag-$platform-$archStr.$ext"
  $url  = "https://nodejs.org/dist/$vTag/$file"

  [pscustomobject]@{
    Url          = $url
    FileName     = $file
    Ext          = $ext
    Platform     = $platform
    Arch         = $archStr
    Version      = $Version
    NodeDirName  = "node-$vTag-$platform-$archStr"
  }
}

function Install-NodeVersion {
  param([Parameter(Mandatory = $true)][string]$TargetVersion)

  $info = Get-NodeArchiveInfo -Version $TargetVersion

  Write-Info "Installing Node v$($info.Version) for $($info.Platform)-$($info.Arch)"

  $tmp = New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ("node-install-" + [guid]::NewGuid())) -Force

  try {
    $archive = Join-Path $tmp.FullName $info.FileName

    Invoke-WebRequest -Uri $info.Url -OutFile $archive

    # Extract
    if ($info.Ext -eq 'zip') {
      Expand-Archive -Path $archive -DestinationPath $tmp.FullName -Force
    } else {
      $tarCmd = Get-Command tar -ErrorAction SilentlyContinue

      if (-not $tarCmd) {
        throw "tar not found; required to extract $($info.Ext)"
      }

      & $tarCmd.Source -xf $archive -C $tmp.FullName
    }

    $extractedRoot = Join-Path $tmp.FullName $info.NodeDirName
    if (-not (Test-Path $extractedRoot)) {
      $cand = Get-ChildItem -Path $tmp.FullName -Directory | Where-Object { $_.Name -like 'node-v*' } | Select-Object -First 1

      if ($cand) {
        $extractedRoot = $cand.FullName
      }
    }

    if (-not (Test-Path $extractedRoot)) {
      throw "Could not locate extracted Node directory."
    }

    # Clean target and move contents
  Write-Info "Deploying Node to $NodeInstallDir"
    
    if (Test-Path $NodeInstallDir) {
      Get-ChildItem -Path $NodeInstallDir -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    } else {
      New-Item -ItemType Directory -Path $NodeInstallDir -Force | Out-Null
    }

    Get-ChildItem -Path $extractedRoot -Force | ForEach-Object {
      Move-Item -Path $_.FullName -Destination $NodeInstallDir -Force
    }

    # PATH update
    if ($IsWin) {
      Add-UserPathEntry $NodeInstallDir
    } else {
      $NodeBinDir = Join-Path $NodeInstallDir 'bin'
      Add-UserPathEntry $NodeBinDir
    }

    # Verify
    $nodeExe = if ($IsWin) {
      Join-Path $NodeInstallDir 'node.exe'
    } else {
      Join-Path $NodeInstallDir 'bin/node'
    }

    if (-not (Test-Path $nodeExe)) {
      throw "Node binary not found after install."
    }

    $v = & $nodeExe -v
    
    Write-Info "Installed Node $v"
  }
  finally {
    if (Test-Path $tmp.FullName) {
      Remove-Item -Path $tmp.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
  }
}

function Install-Pnpm {
  param()

  # If pnpm is already installed (on PATH), skip installation
  try {
    $pnpmCmd = Get-Command pnpm -ErrorAction SilentlyContinue
  } catch {
    $pnpmCmd = $null
  }

  if ($pnpmCmd) {
    try {
      $ver = & $pnpmCmd.Source -v 2>$null
    } catch {
      $ver = $null
    }
    if ($ver) { Write-Info "Found pnpm $ver on PATH — skipping installation" } else { Write-Info "Found pnpm on PATH — skipping installation" }
    return
  }

  # Install pnpm using the official PowerShell installer so pnpm is available in this session.
  # Non-fatal: if this fails, the script continues.
  try {
    Write-Info "Installing pnpm via PowerShell installer"
    Invoke-WebRequest https://get.pnpm.io/install.ps1 -UseBasicParsing | Invoke-Expression
  } catch {
    Write-Err "pnpm install step failed: $($_.Exception.Message)"
  }
}

# -------------------------
# .NET helpers
# -------------------------
function Get-RequestedDotnetSdk {
  # Reads global.json at repo root and returns sdk.version (string) + rollForward
  $globalJsonPath = Join-Path $RepoRoot 'global.json'

  if (-not (Test-Path $globalJsonPath)) {
    return $null
  }
  
  try {
    $json = Get-Content -Path $globalJsonPath -Raw | ConvertFrom-Json
    if ($json.sdk.version) {
      return [pscustomobject]@{
        Version     = [string]$json.sdk.version
        RollForward = [string]$json.sdk.rollForward
      }
    }
  } catch {
    Write-Err "Failed to parse global.json: $($_.Exception.Message)"
  }
  
  return $null
}

function Get-InstalledDotnetSdks {
  try {
    $dotnetExe = $null
    try {
      $cmd = Get-Command dotnet -ErrorAction Stop
      $dotnetExe = $cmd.Source
    } catch {
      # dotnet not on PATH — try the user-local install dir as a fallback
      $candidate = if ($IsWin) { Join-Path $DotnetInstallDir 'dotnet.exe' } else { Join-Path $DotnetInstallDir 'dotnet' }
      if (Test-Path $candidate) { $dotnetExe = $candidate }
    }

    if (-not $dotnetExe) { return @() }

    $list = & $dotnetExe --list-sdks 2>$null

    if (-not $list) {
      return @()
    }

    # Accept prerelease versions (e.g. 10.0.100-rc.2.25502.107)
    return @($list | ForEach-Object { ($_ -split '\s+')[0] } | Where-Object { $_ -match '^\d+\.\d+\.\d+' })
  } catch {
    return @()
  }
}

function Install-Dotnet {
  param(
    [Parameter(Mandatory = $true)][string]$TargetSdkVersion
  )

  # Install message will be printed after we determine the channel

  $tmp = New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ("dotnet-install-" + [guid]::NewGuid())) -Force

  try {
    $scriptUrl  = 'https://dot.net/v1/dotnet-install.ps1'
    $scriptPath = Join-Path $tmp.FullName 'dotnet-install.ps1'
    Invoke-WebRequest -Uri $scriptUrl -OutFile $scriptPath

    # Select architecture explicitly
    $archParam = switch ($Arch) {
      'Arm64' { 'arm64' }
      'X64'   { 'x64' }
      default { 'x64' }
    }

    # Always install by Channel (major.minor). For prerelease targets append -Quality preview.
    $channel = $null

    if ($TargetSdkVersion -match '^(\d+)\.(\d+)') {
      $channel = "$($Matches[1]).$($Matches[2])"
    } else {
      throw "Invalid sdk.version in global.json. Expected a full SDK version like 'X.Y.Z' optionally with a prerelease suffix (e.g. '-rc' or '-preview'). Got: '$TargetSdkVersion'"
    }

    # Now that we have the channel, print the install message
    Write-Info "Installing .NET SDK $TargetSdkVersion (channel $channel)"

    $installerArgs = "-Channel $channel -InstallDir $DotnetInstallDir -Architecture $archParam"
    $isPreview = $TargetSdkVersion -like '*-*'
    if ($isPreview) { $installerArgs += ' -Quality preview' }

    # Invoke installer using named parameters (preview adds -Quality)
    if ($isPreview) {
      & $scriptPath -Channel $channel -InstallDir $DotnetInstallDir -Architecture $archParam -Quality preview | Out-Null
    } else {
      & $scriptPath -Channel $channel -InstallDir $DotnetInstallDir -Architecture $archParam | Out-Null
    }

    # PATH updates
    $toolsDir = Join-Path $DotnetInstallDir 'tools'
    New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
  Add-UserPathEntry $DotnetInstallDir
  Add-UserPathEntry $toolsDir

    # Verify
    $dotnetCmd = if ($IsWin) { Join-Path $DotnetInstallDir 'dotnet.exe' } else { Join-Path $DotnetInstallDir 'dotnet' }
    
    if (-not (Test-Path $dotnetCmd)) {
      throw "dotnet binary not found after install."
    }

  $dv = & $dotnetCmd --version
    
  Write-Info "Installed .NET SDK $dv"
  }
  finally {
    if (Test-Path $tmp.FullName) {
      Remove-Item -Path $tmp.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
  }
}

# -------------------------
# Ensure Node.js (from repo spec)
# -------------------------

Write-Info "<==== node.js ====>"

if (-not $SkipNode) {
  try {
    $installedNode = Get-InstalledNodeVersion

    if (!$installedNode) {
      Write-Info "Node not found; will install requested version"
    }

    $nodeSpec = Get-RequestedNodeSpec

    if (-not $nodeSpec) {
      throw ".nvmrc nicht gefunden oder leer. Dieses Repository erfordert eine .nvmrc zur Festlegung der Node.js-Version."
    } else {
      $targetNode = Resolve-NodeVersionFromSpec -Spec $nodeSpec
    }

    $installedMajor = if ($installedNode) { ($installedNode -split '\.')[0] } else { $null }
    $targetMajor    = ($targetNode -split '\.')[0]

    if ($installedMajor -and ($installedMajor -eq $targetMajor)) {
      Write-Info "Found v$installedNode which matches the requested major version. Skipping..."
    } else {
      if ($PSCmdlet.ShouldProcess("Node.js v$targetNode", "Install/Update")) {
        Install-NodeVersion -TargetVersion $targetNode
      }
    }

    Install-Pnpm
  } catch {
    Write-Err "Node.js step failed: $($_.Exception.Message)"
    throw
  }
}

# -------------------------
# Ensure .NET SDK (from global.json)
# -------------------------
if (-not $SkipDotNet) {
  Write-Host ""
  Write-Info "<==== .NET ====>"
  try {
    $installedSdks = Get-InstalledDotnetSdks

    if (@($installedSdks).Count -eq 0) {
      Write-Info "No .NET SDKs detected; will install the requested SDK"
    }

    $requested = Get-RequestedDotnetSdk

    if (-not $requested) {
      $globalJsonPath = Join-Path $RepoRoot 'global.json'
      if (Test-Path $globalJsonPath) {
        throw "global.json gefunden, aber sdk.version konnte nicht ermittelt werden. Bitte sdk.version in global.json definieren."
      } else {
        throw "global.json nicht gefunden. Dieses Repository erfordert eine global.json zur Festlegung der .NET SDK-Version."
      }
    } else {
      $targetSdk = $requested.Version

      # Check if any installed SDK matches the requested channel (major.minor)
      $parts = ($targetSdk -split '\.')
      $targetSdkChannel = "$($parts[0]).$($parts[1])"

      $hasChannelInstalled = $installedSdks | Where-Object {
        $instParts = ($_ -split '\.')
        ("$($instParts[0]).$($instParts[1])") -eq $targetSdkChannel
      } | Select-Object -First 1

      if ($hasChannelInstalled) {
        # $hasChannelInstalled contains an installed SDK version string
        Write-Info "Found $hasChannelInstalled which matches the requested channel $targetSdkChannel. Skipping..."
      } else {
        # Install latest patch for requested channel using channel installs
        if ($PSCmdlet.ShouldProcess(".NET SDK $targetSdk", "Install/Update")) {
          Install-Dotnet -TargetSdkVersion $targetSdk
        }
      }
    }
  } catch {
    Write-Err ".NET step failed: $($_.Exception.Message)"
    throw
  }
}

Write-Host ""
Write-Host "✅ Done." -ForegroundColor Green
Write-Host "If this is your first run, open a new shell to ensure updated PATH is picked up."