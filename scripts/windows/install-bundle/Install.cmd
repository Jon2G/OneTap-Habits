@echo off
setlocal
cd /d "%~dp0"
echo OneTap Habits - Windows installer
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
if errorlevel 1 (
  echo.
  echo Install failed. See the message above.
  pause
  exit /b 1
)
echo.
pause
