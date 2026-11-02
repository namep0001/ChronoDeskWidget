param([Parameter(Mandatory=$true)][string]$TargetDirectory)
$ErrorActionPreference = "Stop"
$shell = New-Object -ComObject WScript.Shell
$icon = Join-Path $TargetDirectory "Uninstall-Widget.ico"
$batPath = Join-Path $TargetDirectory "Uninstall-Widget.bat"
$psPath = Join-Path $TargetDirectory "Uninstall-Widget.ps1"
$batShortcut = $shell.CreateShortcut((Join-Path $TargetDirectory "Uninstall ChronoDesk Widget (BAT).lnk"))
$batShortcut.TargetPath = $batPath
$batShortcut.WorkingDirectory = $TargetDirectory
$batShortcut.IconLocation = $icon
$batShortcut.Save()
$psShortcut = $shell.CreateShortcut((Join-Path $TargetDirectory "Uninstall ChronoDesk Widget (PowerShell).lnk"))
$psShortcut.TargetPath = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
$psShortcut.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $psPath + '"'
$psShortcut.WorkingDirectory = $TargetDirectory
$psShortcut.IconLocation = $icon
$psShortcut.Save()
