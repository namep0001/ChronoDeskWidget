@echo off
setlocal
cd /d "%~dp0"
if not exist "%~dp0Create-UninstallerShortcuts.ps1" (
  echo ERROR: Create-UninstallerShortcuts.ps1 is missing.
  pause
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Create-UninstallerShortcuts.ps1" -TargetDirectory "%~dp0"
if errorlevel 1 (
  echo Failed to create the uninstall shortcuts.
  pause
  exit /b 1
)
echo Uninstall shortcuts created successfully.
exit /b 0
