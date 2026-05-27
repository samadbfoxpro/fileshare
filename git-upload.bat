@echo off
setlocal

cd /d "%~dp0"

echo.
echo === FileShare: upload local changes to GitHub ===
echo.

git status --short
echo.

set "MSG=%~1"
if "%MSG%"=="" set "MSG=update fileshare"

git add .
if errorlevel 1 goto fail

git diff --cached --quiet
if not errorlevel 1 (
  echo No staged changes to commit.
) else (
  git commit -m "%MSG%"
  if errorlevel 1 goto fail
)

git push
if errorlevel 1 goto fail

echo.
echo Upload complete.
goto end

:fail
echo.
echo Upload failed. Check the message above.

:end
echo.
pause
