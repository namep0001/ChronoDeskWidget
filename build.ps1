$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$sourceFiles = @(
    'Source\Program.cs',
    'Source\LocalStore.cs',
    'Source\TgjuClient.cs',
    'Source\MainForm.cs'
)
$outDir = Join-Path $PSScriptRoot 'bin\Release'
$outExe = Join-Path $outDir 'ChronoDeskWidget.exe'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$candidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$csc = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if ($csc) {
    Write-Host 'Compiling ChronoDesk Widget for Windows 10/11...' -ForegroundColor Cyan
    $compilerArgs = @(
        '/nologo', '/target:winexe', '/optimize+', '/platform:anycpu32bitpreferred', '/codepage:65001', "/out:$outExe",
        '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll',
        '/reference:System.Windows.Forms.dll', '/reference:System.Xml.dll'
    ) + $sourceFiles
    & $csc @compilerArgs
    if ($LASTEXITCODE -ne 0) { throw "C# compilation failed with exit code $LASTEXITCODE." }
} else {
    $msbuild = Get-Command 'msbuild.exe' -ErrorAction SilentlyContinue
    if (-not $msbuild) {
        throw 'No .NET Framework C# compiler was found. Install Visual Studio Build Tools with the .NET desktop build tools, then rerun build.ps1 or build.bat.'
    }
    Write-Host 'Building through MSBuild...' -ForegroundColor Cyan
    & $msbuild.Source 'ChronoDeskWidget.csproj' '/p:Configuration=Release' '/v:minimal'
    if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE." }
}
if (-not (Test-Path -LiteralPath $outExe)) { throw 'Build ended without producing bin\Release\ChronoDeskWidget.exe.' }
Write-Host "Build succeeded: $outExe" -ForegroundColor Green
Write-Host 'Run the EXE to start. Use tray > Hide widget to hide it, or tray > Exit to quit.'