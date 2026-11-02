@echo off
setlocal
cd /d "%~dp0"
if not exist "%~dp0Uninstall-Widget.ps1" (
  echo ERROR: Uninstall-Widget.ps1 is missing.
  pause
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall-Widget.ps1"
