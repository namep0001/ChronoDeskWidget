@echo off
setlocal
cd /d "%~dp0"
if not exist "Source\MainForm.cs" (
  echo Restoring MainForm.cs from the verified compressed source...
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0restore-mainform.ps1"
  if errorlevel 1 goto :fail
)
set "CSC="
if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not defined CSC if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not defined CSC (
  where msbuild.exe >nul 2>nul
  if not errorlevel 1 (
    echo Building with MSBuild...
    msbuild ChronoDeskWidget.csproj /p:Configuration=Release /v:minimal
    if errorlevel 1 goto :fail
    if exist "bin\Release\ChronoDeskWidget.exe" goto :ok
  )
  echo ERROR: A C# compiler was not found.
  echo Install Visual Studio Build Tools with the .NET desktop build tools, then run build.bat again.
  goto :fail
)
if not exist "bin\Release" mkdir "bin\Release"
echo Compiling ChronoDesk Widget...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu32bitpreferred /codepage:65001 /out:"bin\Release\ChronoDeskWidget.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll Source\Program.cs Source\LocalStore.cs Source\TgjuClient.cs Source\MainForm.cs
if errorlevel 1 goto :fail
:ok
echo.
echo Build succeeded: %CD%\bin\Release\ChronoDeskWidget.exe
echo Run the EXE to start the widget. Use the tray menu to hide or exit.
exit /b 0
:fail
echo.
echo Build failed. See the error output above.
exit /b 1