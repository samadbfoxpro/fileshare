@echo off
setlocal
cd /d "%~dp0"

echo.
echo === FileShare v1.5 - Debug Build ===
echo.

dotnet build FileShareVisualStudio.csproj -c Debug --no-restore
if errorlevel 1 goto fail

echo.
echo Build complete:
echo %CD%\bin\Debug\net9.0-windows\FileShare.exe
echo.
goto end

:fail
echo.
echo Build failed. Check the message above.

:end
echo.
pause
