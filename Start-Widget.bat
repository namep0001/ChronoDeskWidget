@echo off
setlocal
cd /d "%~dp0"
set "EXE=bin\Release\ChronoDeskWidget.exe"
set "NEED_BUILD=0"
if not exist "%EXE%" set "NEED_BUILD=1"
if "%NEED_BUILD%"=="0" (
  powershell -NoProfile -ExecutionPolicy Bypass -Command "$exe=(Get-Item -LiteralPath 'bin\Release\ChronoDeskWidget.exe').LastWriteTimeUtc; $latest=(Get-ChildItem -Path 'Source\*.cs','ChronoDeskWidget.csproj' | Measure-Object -Property LastWriteTimeUtc -Maximum).Maximum; if ($latest -gt $exe) { exit 1 } else { exit 0 }"
  if errorlevel 1 set "NEED_BUILD=1"
)
if "%NEED_BUILD%"=="1" (
  call build.bat
  if errorlevel 1 (
    echo.
    echo Build failed. Please resolve the error above.
    pause
    exit /b 1
  )
)
start "" "%CD%\%EXE%"
exit /b 0