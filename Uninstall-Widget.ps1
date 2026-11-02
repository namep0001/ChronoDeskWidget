param(
    [switch]$Confirmed,
    [int]$AppProcessId = 0
)

$ErrorActionPreference = "SilentlyContinue"
$installDir = [System.IO.Path]::GetFullPath((Split-Path -Parent $MyInvocation.MyCommand.Path))
$appName = "ChronoDesk Widget"

if (-not $Confirmed) {
    Add-Type -AssemblyName System.Windows.Forms
    $answer = [System.Windows.Forms.MessageBox]::Show(
        "Uninstall ChronoDesk Widget? This removes its startup entries, saved settings, and the entire application folder.",
        $appName,
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Warning,
        [System.Windows.Forms.MessageBoxDefaultButton]::Button2)
    if ($answer -ne [System.Windows.Forms.DialogResult]::Yes) { exit 0 }
}

# Remove current and known legacy Run entries so an old build cannot launch later.
$runPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$runNames = @("ChronoDeskWidget", "ChronoDesk Widget", "ChronoDeskWidget.exe")
foreach ($name in $runNames) {
    Remove-ItemProperty -Path $runPath -Name $name -Force
    Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run" -Name $name -Force
}

# Remove matching per-user and all-user Startup shortcuts.
$startupDirs = @(
    [Environment]::GetFolderPath("Startup"),
    [Environment]::GetFolderPath("CommonStartup")
) | Where-Object { $_ -and (Test-Path $_) }
foreach ($dir in $startupDirs) {
    Get-ChildItem -LiteralPath $dir -Force | Where-Object {
        $_.Name -like "*ChronoDesk*" -or $_.Name -like "*Chrono Desk*"
    } | Remove-Item -Force -Recurse
}

# Remove a scheduled task only if it uses this application's name.
Get-ScheduledTask | Where-Object {
    $_.TaskName -like "*ChronoDeskWidget*" -or $_.TaskName -like "*ChronoDesk Widget*"
} | Unregister-ScheduledTask -Confirm:$false

# Stop only application processes whose executable is inside this installation.
$appPath = Join-Path $installDir "ChronoDeskWidget.exe"
$appProcesses = @()
Get-Process -Name "ChronoDeskWidget" -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        if ($_.Path -and ([System.IO.Path]::GetFullPath($_.Path) -ieq [System.IO.Path]::GetFullPath($appPath))) {
            $appProcesses += $_.Id
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        }
    } catch { }
}

# Delete settings, notes, tasks, and logs from the user's profile.
$localData = Join-Path $env:LOCALAPPDATA "ChronoDeskWidget"
if (Test-Path -LiteralPath $localData) {
    Remove-Item -LiteralPath $localData -Force -Recurse
}

# A separate temporary command file removes the installation folder after this
# script and the application have exited. This supports self-deletion on Windows.
$tempCmd = Join-Path $env:TEMP ("ChronoDeskWidget-Uninstall-" + [guid]::NewGuid().ToString("N") + ".cmd")
$waitPids = @($appProcesses)
if ($AppProcessId -gt 0 -and $waitPids -notcontains $AppProcessId) { $waitPids += $AppProcessId }
$lines = @("@echo off", ":wait")
foreach ($pid in $waitPids) {
    $lines += "tasklist /FI `"PID eq $pid`" 2>NUL | findstr `"$pid`" >NUL"
    $lines += "if not errorlevel 1 (timeout /t 1 /nobreak >NUL & goto wait)"
}
$escapedDir = $installDir.Replace('"','""')
$lines += "timeout /t 2 /nobreak >NUL"
$lines += "rmdir /S /Q `"$escapedDir`""
$lines += "del `"%~f0`""
Set-Content -LiteralPath $tempCmd -Value $lines -Encoding ASCII
Start-Process -FilePath "$env:ComSpec" -ArgumentList @("/c", "`"$tempCmd`"") -WindowStyle Hidden
exit 0
