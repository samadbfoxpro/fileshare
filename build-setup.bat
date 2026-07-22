@echo off
setlocal

cd /d "%~dp0"

echo.
echo === FileShare: build setup installer ===
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File ".\installer\build-installer.ps1"
if errorlevel 1 goto fail

echo.
echo Setup file created:
echo %CD%\dist\FileShareSetup.exe
echo.
goto end

:fail
echo.
echo Setup build failed. Check the message above.
echo If the error is about NuGet or SSL, connect to the internet and run this file again.

:end
echo.
pause
