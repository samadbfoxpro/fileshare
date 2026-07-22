@echo off
setlocal
cd /d "%~dp0"

echo.
echo === FileShare v1.5 - Release Build ===
echo.

dotnet build FileShareVisualStudio.csproj -c Release --no-restore
if errorlevel 1 goto fail

echo.
echo ================================================
echo  Release Build Succeeded!
echo  Executable:
echo  %CD%\bin\Release\net9.0-windows\FileShare.exe
echo ================================================
echo.
goto end

:fail
echo.
echo ================================================
echo  Release Build Failed. Check errors above.
echo ================================================

:end
echo.
pause
