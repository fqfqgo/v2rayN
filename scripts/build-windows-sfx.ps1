# Build a per-user Windows SFX installer from an unpacked v2rayN publish folder.
# Usage: .\build-windows-sfx.ps1 -PayloadDir <dir> -OutputExe <path>
param(
    [Parameter(Mandatory = $true)][string]$PayloadDir,
    [Parameter(Mandatory = $true)][string]$OutputExe
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $PayloadDir -PathType Container)) {
    throw "payload dir not found: $PayloadDir"
}
$exePath = Join-Path $PayloadDir 'v2rayN.exe'
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "v2rayN.exe not found in $PayloadDir"
}

$sevenZipCmd = Get-Command 7z -ErrorAction SilentlyContinue
if (-not $sevenZipCmd) {
    $sevenZipCmd = Get-Command 7z.exe -ErrorAction SilentlyContinue
}
if (-not $sevenZipCmd) {
    throw '7z is required on PATH'
}
$sevenZip = $sevenZipCmd.Source

$rootDir = Split-Path -Parent $PSScriptRoot
$sfxDir = Join-Path $rootDir 'scripts\windows-sfx'
$sfxModule = Join-Path $sfxDir '7zSD-v2rayN.sfx'
if (-not (Test-Path -LiteralPath $sfxModule)) {
    throw "missing branded SFX module: $sfxModule"
}

$workDir = Join-Path ([System.IO.Path]::GetTempPath()) ("v2rayn-sfx-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $workDir | Out-Null
try {
    $payloadCopy = Join-Path $workDir 'payload'
    New-Item -ItemType Directory -Path $payloadCopy | Out-Null
    Copy-Item -Path (Join-Path $PayloadDir '*') -Destination $payloadCopy -Recurse -Force
    Copy-Item -Path (Join-Path $sfxDir 'install.ps1') -Destination (Join-Path $payloadCopy 'install.ps1') -Force
    Copy-Item -Path (Join-Path $sfxDir 'install.cmd') -Destination (Join-Path $payloadCopy 'install.cmd') -Force
    Copy-Item -Path (Join-Path $sfxDir 'uninstall.ps1') -Destination (Join-Path $payloadCopy 'uninstall.ps1') -Force

    $payload7z = Join-Path $workDir 'payload.7z'
    Push-Location $payloadCopy
    try {
        & $sevenZip a -t7z -mx=7 -m0=lzma2 $payload7z . | Out-Null
    }
    finally {
        Pop-Location
    }

    $outDir = Split-Path -Parent $OutputExe
    if ($outDir -and -not (Test-Path -LiteralPath $outDir)) {
        New-Item -ItemType Directory -Path $outDir | Out-Null
    }

    $config = Join-Path $sfxDir 'config.txt'
    $outStream = [System.IO.File]::Create($OutputExe)
    try {
        foreach ($part in @($sfxModule, $config, $payload7z)) {
            $bytes = [System.IO.File]::ReadAllBytes($part)
            $outStream.Write($bytes, 0, $bytes.Length)
        }
    }
    finally {
        $outStream.Dispose()
    }

    Write-Host "Created $OutputExe ($((Get-Item -LiteralPath $OutputExe).Length) bytes)"
}
finally {
    Remove-Item -LiteralPath $workDir -Recurse -Force -ErrorAction SilentlyContinue
}
