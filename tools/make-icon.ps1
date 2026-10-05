# Renders package\icon.png (256x256, required by Thunderstore) with System.Drawing.
param([string]$Out = (Join-Path $PSScriptRoot '..\package\icon.png'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.Clear([System.Drawing.Color]::Transparent)

function New-RoundedRect([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}
function C([int]$a, [int]$r, [int]$gr, [int]$b) { [System.Drawing.Color]::FromArgb($a, $r, $gr, $b) }
function P([float]$x, [float]$y) { New-Object System.Drawing.PointF $x, $y }

# Background (same palette as LocalChatRange)
$bg = New-RoundedRect 0 0 256 256 44
$bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0, 0), (New-Object System.Drawing.Point 0, 256), (C 255 46 62 92), (C 255 22 30 48)
$g.FillPath($bgBrush, $bg)

# Chat bubble with a tail
$cream = New-Object System.Drawing.SolidBrush (C 255 250 244 232)
$g.FillPath($cream, (New-RoundedRect 26 50 156 116 34))
$tail = New-Object System.Drawing.Drawing2D.GraphicsPath
$tail.AddPolygon(@((P 56 150), (P 96 158), (P 40 204)))
$g.FillPath($cream, $tail)

# Bell inside the bubble
$ink = New-Object System.Drawing.SolidBrush (C 255 34 44 68)
$cx = 104; $cy = 106
$bell = New-Object System.Drawing.Drawing2D.GraphicsPath
$bell.AddBezier((P ($cx - 30) ($cy + 20)), (P ($cx - 30) ($cy - 8)), (P ($cx - 24) ($cy - 34)), (P $cx ($cy - 34)))
$bell.AddBezier((P $cx ($cy - 34)), (P ($cx + 24) ($cy - 34)), (P ($cx + 30) ($cy - 8)), (P ($cx + 30) ($cy + 20)))
$bell.CloseFigure()
$g.FillPath($ink, $bell)
$g.FillPath($ink, (New-RoundedRect ($cx - 40) ($cy + 14) 80 13 6.5))
$g.FillEllipse($ink, ($cx - 7), ($cy - 45), 14, 14)
$g.FillEllipse($ink, ($cx - 10), ($cy + 24), 20, 15)

# Sound waves
$wave = New-Object System.Drawing.Pen (C 255 140 217 255), 11
$wave.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$wave.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawArc($wave, (196 - 26), (108 - 26), 52, 52, -50, 100)
$g.DrawArc($wave, (196 - 50), (108 - 50), 100, 100, -48, 96)

# Notification dot
$g.FillEllipse((New-Object System.Drawing.SolidBrush (C 255 22 30 48)), 156, 30, 44, 44)
$g.FillEllipse((New-Object System.Drawing.SolidBrush (C 255 255 120 110)), 161, 35, 34, 34)

$g.Dispose()
$full = [System.IO.Path]::GetFullPath($Out)
$bmp.Save($full, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "Icon: $full"
