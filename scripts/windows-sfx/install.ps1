# ASCII-only script. UI strings are UTF-16LE Base64 to avoid ANSI mojibake on English Windows.
$ErrorActionPreference = 'Stop'

function Decode-Ui([string]$Base64) {
    return [Text.Encoding]::Unicode.GetString([Convert]::FromBase64String($Base64))
}

$src = Split-Path -Parent $MyInvocation.MyCommand.Path
$dest = Join-Path $env:LOCALAPPDATA 'Programs\v2rayN'

# 将安装到： / 并创建桌面快捷方式。 / 仅登记当前用户卸载信息，不修改系统目录。 / 是否继续？
$lineInstall = Decode-Ui 'BlyJW8WIMFIa/w=='
$lineShortcut = Decode-Ui 'dl4bUvpeTGhil+tfd2O5ZQ9fAjA='
$linePolicy = Decode-Ui 'xU57drCLU19NUih1N2J4U32P4U9vYAz/DU7uTzll+3zffu52VV8CMA=='
$lineConfirm = Decode-Ui 'L2YmVOd+7X4f/w=='
$title = 'v2rayN'
$prompt = "$lineInstall`r`n$dest`r`n`r`n$lineShortcut`r`n$linePolicy`r`n$lineConfirm"

Add-Type -AssemblyName System.Windows.Forms
$answer = [System.Windows.Forms.MessageBox]::Show(
    $prompt,
    $title,
    [System.Windows.Forms.MessageBoxButtons]::YesNo,
    [System.Windows.Forms.MessageBoxIcon]::Question)
if ($answer -ne [System.Windows.Forms.DialogResult]::Yes) {
    exit 1
}

Get-Process -Name 'v2rayN' -ErrorAction SilentlyContinue | Stop-Process -Force
if (Test-Path -LiteralPath $dest) {
    $prefix = [IO.Path]::GetFullPath($dest).TrimEnd('\') + '\'
    Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.Path -and $_.Path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
    } | Stop-Process -Force
}
$until = [datetime]::UtcNow.AddSeconds(5)
while ([datetime]::UtcNow -lt $until -and (Get-Process -Name 'v2rayN' -ErrorAction SilentlyContinue)) {
    Start-Sleep -Milliseconds 200
}

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

# Per-user uninstall entry silences Program Compatibility Assistant for SFX installers.
$uninstPs1 = Join-Path $dest 'uninstall.ps1'
$uninstKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\v2rayN'
New-Item -Path $uninstKey -Force | Out-Null
New-ItemProperty -Path $uninstKey -Name 'DisplayName' -Value 'v2rayN' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstKey -Name 'DisplayIcon' -Value $exe -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstKey -Name 'InstallLocation' -Value $dest -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstKey -Name 'Publisher' -Value 'fqfqgo' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstKey -Name 'NoModify' -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstKey -Name 'NoRepair' -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstKey -Name 'UninstallString' -Value ("powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstPs1`"") -PropertyType String -Force | Out-Null

Start-Process -FilePath $exe -WorkingDirectory $dest
