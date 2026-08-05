@echo off
chcp 65001 >nul
setlocal

set "PROJECT=%~dp0v2rayN\v2rayN.csproj"
set "OUTPUT=%~dp0v2rayN\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"

echo Building v2rayN 7.24.4 self-contained for win-x64 on .NET 10...
dotnet publish "%PROJECT%" -c Release -r win-x64 -p:SelfContained=true -p:EnableWindowsTargeting=true -p:PublishSingleFile=true -p:PublishReadyToRun=false -p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 exit /b 1

if not exist "%OUTPUT%\v2rayN.exe" (
    echo Build completed, but v2rayN.exe was not found in:
    echo %OUTPUT%
    exit /b 1
)

echo Build completed:
echo %OUTPUT%
endlocal
