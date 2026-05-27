@echo off
setlocal

cd /d "%~dp0"

echo.
echo === FileShare: download latest changes from GitHub ===
echo.

git status --short
echo.

git pull --rebase
if errorlevel 1 goto fail

echo.
echo Download complete.
goto end

:fail
echo.
echo Download failed. If you have local changes, commit or stash them first.

:end
echo.
pause
