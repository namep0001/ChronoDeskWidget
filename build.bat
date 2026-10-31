@echo off
setlocal
cd /d "%~dp0"

if not exist "Source\MainForm.cs" (
  echo ERROR: Source\MainForm.cs is missing. Download the complete repository source.
  goto :fail
)

if not exist "bin\Release" mkdir "bin\Release"

echo Generating the application icon...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Source\Generate-AppIcon.ps1"
if errorlevel 1 goto :fail

set "CSC="
if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not defined CSC if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if defined CSC (
  echo Compiling ChronoDesk Widget...
  "%CSC%" /nologo /target:winexe /win32icon:"Source\ChronoDeskWidget.ico" /optimize+ /platform:anycpu32bitpreferred /codepage:65001 /out:"bin\Release\ChronoDeskWidget.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll Source\AssemblyInfo.cs Source\Program.cs Source\LocalStore.cs Source\TgjuClient.cs Source\MainForm.cs Source\WidgetControls.cs Source\SystemWidgets.cs
  if errorlevel 1 goto :fail
) else (
  where msbuild.exe >nul 2>nul
  if errorlevel 1 (
    echo ERROR: No C# compiler or MSBuild was found.
    echo Install Visual Studio Build Tools with the .NET desktop build tools.
    goto :fail
  )
  echo Building with MSBuild...
  msbuild ChronoDeskWidget.csproj /p:Configuration=Release /v:minimal
  if errorlevel 1 goto :fail
)

if not exist "bin\Release\ChronoDeskWidget.exe" (
  echo ERROR: The build did not produce the executable.
  goto :fail
)

echo.
echo Build succeeded: %CD%\bin\Release\ChronoDeskWidget.exe
exit /b 0

:fail
echo.
echo Build failed. See the error output above.
exit /b 1
