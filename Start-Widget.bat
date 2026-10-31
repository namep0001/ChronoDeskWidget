@echo off
setlocal
cd /d "%~dp0"
set "EXE=bin\Release\ChronoDeskWidget.exe"
set "NEED_BUILD=0"

if not exist "%EXE%" set "NEED_BUILD=1"
if "%NEED_BUILD%"=="0" (
  powershell -NoProfile -ExecutionPolicy Bypass -Command "$paths=@('Source\*.cs','Source\Generate-AppIcon.ps1','ChronoDeskWidget.csproj','build.bat'); $latest=(Get-ChildItem -Path $paths -ErrorAction SilentlyContinue | Measure-Object -Property LastWriteTimeUtc -Maximum).Maximum; $exe=(Get-Item -LiteralPath 'bin\Release\ChronoDeskWidget.exe').LastWriteTimeUtc; if ($latest -gt $exe) { exit 1 } else { exit 0 }"
  if errorlevel 1 set "NEED_BUILD=1"
)

if "%NEED_BUILD%"=="1" (
  call build.bat
  if errorlevel 1 (
    echo.
    echo Build failed. Resolve the error above, then run this launcher again.
    pause
    exit /b 1
  )
)

start "" "%CD%\%EXE%"
exit /b 0
