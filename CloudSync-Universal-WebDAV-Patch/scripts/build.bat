@echo off
REM ============================================================
REM  CloudSync build script (Windows .bat)
REM  Usage: build.bat [Release|Debug]
REM ============================================================
setlocal

set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Release"

set "ROOT=%~dp0.."
cd /d "%ROOT%"

REM Check dependencies
if not exist "StardewModdingAPI.dll" (
    echo [ERROR] StardewModdingAPI.dll not found in project root.
    echo Please copy the 5 dependency DLLs from your Stardew Valley game folder.
    echo See README.md for details.
    exit /b 1
)

echo Building CloudSync (%CONFIG%)...
dotnet build CloudSync.csproj -c %CONFIG% -v minimal
if errorlevel 1 (
    echo [FAILED] Build failed.
    exit /b 1
)

echo.
echo [OK] Build succeeded.
echo Output: bin\%CONFIG%\net6.0\CloudSync.dll
endlocal
