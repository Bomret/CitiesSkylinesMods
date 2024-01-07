
function Run {
  $exe, $argsForExe = $Args

  $ErrorActionPreference = 'Continue'

  try {
    & $exe $argsForExe
  } catch {
    Throw
  } # catch is triggered ONLY if $exe can't be found, never for errors reported by $exe itself

  if ($LASTEXITCODE) {
    Throw "$exe indicated failure (exit code $LASTEXITCODE; full command: $Args)."
  }
}

function Log {
  $msg = $Args

  Write-Host "`r`n> $msg" -ForegroundColor Green
}

function SubLog {
  $msg = $Args

  Write-Host "⠿ " -ForegroundColor Green -NoNewline;
  Write-Host "$msg" -ForegroundColor White
}