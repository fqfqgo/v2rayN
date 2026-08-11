# ASCII-only uninstall script for the per-user v2rayN install.
$ErrorActionPreference = 'Stop'

$dest = Join-Path $env:LOCALAPPDATA 'Programs\v2rayN'
$desktop = [Environment]::GetFolderPath('Desktop')
$lnk = Join-Path $desktop 'v2rayN.lnk'
if (Test-Path -LiteralPath $lnk) {
    Remove-Item -LiteralPath $lnk -Force
}

$uninstKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\v2rayN'
if (Test-Path -LiteralPath $uninstKey) {
    Remove-Item -LiteralPath $uninstKey -Recurse -Force
}

if (Test-Path -LiteralPath $dest) {
    # Delay self-delete of this script by cmd after powershell exits.
    $cmd = "ping 127.0.0.1 -n 2 >nul & rmdir /s /q `"$dest`""
    Start-Process -FilePath 'cmd.exe' -ArgumentList @('/c', $cmd) -WindowStyle Hidden
}
