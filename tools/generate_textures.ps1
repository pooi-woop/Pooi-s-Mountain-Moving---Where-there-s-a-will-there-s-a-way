# Generate the two PNG icons for the mod (no external art needed).
# Run: powershell -NoProfile -ExecutionPolicy Bypass -File tools\generate_textures.ps1
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

# Resolve project root from this script's location (tools\ -> project root).
$root = $PSScriptRoot
if ([string]::IsNullOrEmpty($root)) { $root = Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrEmpty($root)) { $root = (Get-Location).Path }
# If we are inside tools\, go up one level; otherwise assume current dir is project root.
if ((Split-Path -Leaf $root) -ieq 'tools') { $base = Split-Path -Parent $root } else { $base = $root }

$uiDir  = Join-Path $base 'Textures\PMM\UI'
$desDir = Join-Path $base 'Textures\PMM\Designations'
New-Item -ItemType Directory -Force -Path $uiDir  | Out-Null
New-Item -ItemType Directory -Force -Path $desDir | Out-Null

function New-Graphics($bmp) {
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    return $g
}

# ---------- 1) Button icon 128x128: gray mountains + red X ----------
$bmp = New-Object System.Drawing.Bitmap 128, 128
$g = New-Graphics $bmp
$g.Clear([System.Drawing.Color]::Transparent)

$rock     = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(235, 150, 155, 165))
$rockDark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(235, 108, 113, 124))
$peak1 = [System.Drawing.PointF[]]@( [System.Drawing.PointF]::new(16,106), [System.Drawing.PointF]::new(52,34), [System.Drawing.PointF]::new(86,106) )
$peak2 = [System.Drawing.PointF[]]@( [System.Drawing.PointF]::new(60,106), [System.Drawing.PointF]::new(92,52), [System.Drawing.PointF]::new(118,106) )
$g.FillPolygon($rock, $peak1)
$g.FillPolygon($rockDark, $peak2)

$xpen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 224, 60, 60)), 15
$xpen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$xpen.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawLine($xpen, 34, 44, 96, 106)
$g.DrawLine($xpen, 96, 44, 34, 106)

$out1 = Join-Path $uiDir 'RemoveThickRoof.png'
$bmp.Save($out1, [System.Drawing.Imaging.ImageFormat]::Png)
$xpen.Dispose(); $rock.Dispose(); $rockDark.Dispose(); $g.Dispose(); $bmp.Dispose()

# ---------- 2) Map designation overlay 64x64: translucent amber X ----------
$bmp2 = New-Object System.Drawing.Bitmap 64, 64
$g2 = New-Graphics $bmp2
$g2.Clear([System.Drawing.Color]::Transparent)

$open2 = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(205, 255, 176, 32)), 9
$open2.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$open2.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
$g2.DrawLine($open2, 14, 14, 50, 50)
$g2.DrawLine($open2, 50, 14, 14, 50)

$out2 = Join-Path $desDir 'RemoveThickRoof.png'
$bmp2.Save($out2, [System.Drawing.Imaging.ImageFormat]::Png)
$open2.Dispose(); $g2.Dispose(); $bmp2.Dispose()

Write-Output ("OK: " + $out1)
Write-Output ("OK: " + $out2)
