Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class UninstallerIconNativeMethods {
    [DllImport("user32.dll", SetLastError=true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
"@
$bitmap = New-Object System.Drawing.Bitmap 32, 32
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)
$navy = [System.Drawing.Color]::FromArgb(35, 48, 67)
$red = [System.Drawing.Color]::FromArgb(210, 55, 65)
$white = [System.Drawing.Color]::White
$navyBrush = New-Object System.Drawing.SolidBrush $navy
$redBrush = New-Object System.Drawing.SolidBrush $red
$whiteBrush = New-Object System.Drawing.SolidBrush $white
$outline = New-Object System.Drawing.Pen $navy, 1.6
$arrow = New-Object System.Drawing.Pen $white, 2.4
try {
    $graphics.FillEllipse($navyBrush, 1, 1, 30, 30)
    $graphics.DrawEllipse($outline, 1, 1, 30, 30)
    $graphics.FillRectangle($redBrush, 7, 7, 11, 18)
    $graphics.DrawRectangle($outline, 7, 7, 11, 18)
    $graphics.DrawLine($arrow, 14, 16, 25, 16)
    $graphics.DrawLine($arrow, 21, 12, 25, 16)
    $graphics.DrawLine($arrow, 21, 20, 25, 16)
    $handle = $bitmap.GetHicon()
    try {
        $icon = [System.Drawing.Icon]::FromHandle($handle)
        $stream = [System.IO.File]::Create((Join-Path $PSScriptRoot "Uninstall-Widget.ico"))
        try { $icon.Save($stream) } finally { $stream.Dispose(); $icon.Dispose() }
    } finally {
        [void][UninstallerIconNativeMethods]::DestroyIcon($handle)
    }
} finally {
    $arrow.Dispose(); $outline.Dispose()
    $navyBrush.Dispose(); $redBrush.Dispose(); $whiteBrush.Dispose()
    $graphics.Dispose(); $bitmap.Dispose()
}
