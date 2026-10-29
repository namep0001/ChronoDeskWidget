@echo off
cd /d "%~dp0"
if exist "bin\Release\ChronoDeskWidget.exe" (
  start "" "bin\Release\ChronoDeskWidget.exe"
) else (
  echo The app has not been built yet. Run build.bat first.
  pause
)