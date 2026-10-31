Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class IconNativeMethods {
    [DllImport("user32.dll", SetLastError=true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
"@
$bitmap = New-Object System.Drawing.Bitmap 32, 32
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)
$blue = [System.Drawing.Color]::FromArgb(0, 120, 215)
$white = [System.Drawing.Color]::White
$blueBrush = New-Object System.Drawing.SolidBrush $blue
$whiteBrush = New-Object System.Drawing.SolidBrush $white
$handPen = New-Object System.Drawing.Pen $blue, 2.2
$rimPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(0, 83, 155)), 1
$graphics.FillEllipse($blueBrush, 1, 1, 30, 30)
$graphics.DrawEllipse($rimPen, 1, 1, 30, 30)
$graphics.FillEllipse($whiteBrush, 5, 5, 22, 22)
$graphics.DrawLine($handPen, 16, 8, 16, 16)
$graphics.DrawLine($handPen, 16, 16, 22, 19)
$graphics.FillEllipse($blueBrush, 14.5, 14.5, 3, 3)
$handle = $bitmap.GetHicon()
try {
    $icon = [System.Drawing.Icon]::FromHandle($handle)
    $stream = [System.IO.File]::Create((Join-Path $PSScriptRoot "ChronoDeskWidget.ico"))
    try { $icon.Save($stream) } finally { $stream.Dispose(); $icon.Dispose() }
}
finally {
    [void][IconNativeMethods]::DestroyIcon($handle)
    $handPen.Dispose(); $rimPen.Dispose(); $blueBrush.Dispose(); $whiteBrush.Dispose()
    $graphics.Dispose(); $bitmap.Dispose()
}
