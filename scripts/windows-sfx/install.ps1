# Copies extracted files to the current user's App directory and creates a desktop shortcut.
# Invoked by the 7zSD SFX stub from a temporary extract folder.
$ErrorActionPreference = 'Stop'

$src = Split-Path -Parent $MyInvocation.MyCommand.Path
$dest = Join-Path $env:LOCALAPPDATA 'Programs\v2rayN'
New-Item -ItemType Directory -Path $dest -Force | Out-Null

$skip = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('install.ps1', 'install.cmd'),
    [StringComparer]::OrdinalIgnoreCase)

Get-ChildItem -LiteralPath $src -Force | Where-Object { -not $skip.Contains($_.Name) } | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $dest $_.Name) -Recurse -Force
}

$exe = Join-Path $dest 'v2rayN.exe'
if (-not (Test-Path -LiteralPath $exe)) {
    throw "v2rayN.exe not found after install: $exe"
}

$desktop = [Environment]::GetFolderPath('Desktop')
$ws = New-Object -ComObject WScript.Shell
$shortcut = $ws.CreateShortcut((Join-Path $desktop 'v2rayN.lnk'))
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $dest
$shortcut.IconLocation = $exe
$shortcut.Description = 'v2rayN'
$shortcut.Save()

Start-Process -FilePath $exe -WorkingDirectory $dest
