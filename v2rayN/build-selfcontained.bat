@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo v2rayN Windows Self-Contained Build Script
echo Self-contained version (no .NET Runtime required)
echo ========================================
echo.

set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%"
set "OUTPUT_DIR=%PROJECT_DIR%v2rayN\bin\Release\net8.0-windows10.0.17763\win-x64\publish"

pushd "%PROJECT_DIR%"

echo [1/5] Cleaning previous build files...
dotnet clean v2rayN\v2rayN.csproj -c Release
if errorlevel 1 (
    echo Error: Clean failed
    popd
    pause
    exit /b 1
)

echo [2/5] Restoring NuGet dependencies...
dotnet restore
if errorlevel 1 (
    echo Error: Restore failed
    popd
    pause
    exit /b 1
)

echo [3/5] Building self-contained version...
echo Configuration: Release, win-x64, self-contained, single-file
echo.

dotnet publish v2rayN\v2rayN.csproj -c Release -r win-x64 -p:SelfContained=true -p:EnableWindowsTargeting=true -p:PublishSingleFile=true -p:PublishReadyToRun=false -p:IncludeNativeLibrariesForSelfExtract=true

if errorlevel 1 (
    echo.
    echo Error: Build failed
    popd
    pause
    exit /b 1
)

echo.
echo [4/5] Finding generated files...

if not exist "%OUTPUT_DIR%\v2rayN.exe" (
    echo Error: Generated exe file not found
    echo Expected location: %OUTPUT_DIR%\v2rayN.exe
    popd
    pause
    exit /b 1
)

echo.
echo [5/5] Build completed!
echo ========================================
echo Output directory: %OUTPUT_DIR%
echo.

if exist "%OUTPUT_DIR%\v2rayN.exe" (
    for %%F in ("%OUTPUT_DIR%\v2rayN.exe") do (
        set "FILE_SIZE=%%~zF"
        set /a FILE_SIZE_MB=!FILE_SIZE!/1024/1024
        echo File: v2rayN.exe
        echo Size: !FILE_SIZE_MB! MB (!FILE_SIZE! bytes)
        echo Modified: %%~tF
    )
    
    echo.
    echo Important notes:
    echo 1. This is a self-contained version, no .NET 8.0 Desktop Runtime required
    echo 2. Single-file publish, managed code bundled in v2rayN.exe
    echo 3. Native libraries (DLLs) extracted externally, need to publish together (about 9 files)
    echo 4. Matches official release format
    echo.
    
    set /p OPEN="Open output directory? (Y/N): "
    if /i "!OPEN!"=="Y" (
        explorer "%OUTPUT_DIR%"
    )
) else (
    echo Error: v2rayN.exe not found
)

popd
echo.
echo Build script completed
pause
