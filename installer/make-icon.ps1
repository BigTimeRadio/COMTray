# Generates installer\comtray.ico, a small product icon for the installer and
# Add/Remove Programs entry. The running app draws its own tray icon at runtime;
# this is only the static brand icon.
param(
    [string]$Out = (Join-Path $PSScriptRoot "comtray.ico")
)

Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.Clear([System.Drawing.Color]::Transparent)

$pad = 18
$rect = New-Object System.Drawing.Rectangle($pad, $pad, ($size - 2 * $pad), ($size - 2 * $pad))
$radius = 52
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddArc($rect.X, $rect.Y, $radius, $radius, 180, 90)
$path.AddArc($rect.Right - $radius, $rect.Y, $radius, $radius, 270, 90)
$path.AddArc($rect.Right - $radius, $rect.Bottom - $radius, $radius, $radius, 0, 90)
$path.AddArc($rect.X, $rect.Bottom - $radius, $radius, $radius, 90, 90)
$path.CloseFigure()

$fill = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 30, 41, 59))
$g.FillPath($fill, $path)

$font = New-Object System.Drawing.Font('Segoe UI', 72, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$sf = New-Object System.Drawing.StringFormat
$sf.Alignment = 'Center'
$sf.LineAlignment = 'Center'
$textRect = New-Object System.Drawing.RectangleF($rect.X, $rect.Y, $rect.Width, $rect.Height)
$g.DrawString('COM', $font, [System.Drawing.Brushes]::White, $textRect, $sf)
$g.Dispose()

$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$png = $ms.ToArray()

$ico = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ico)
$bw.Write([uint16]0)            # reserved
$bw.Write([uint16]1)            # type: icon
$bw.Write([uint16]1)            # image count
$bw.Write([byte]0)              # width  (0 = 256)
$bw.Write([byte]0)              # height (0 = 256)
$bw.Write([byte]0)              # palette
$bw.Write([byte]0)              # reserved
$bw.Write([uint16]1)            # color planes
$bw.Write([uint16]32)           # bits per pixel
$bw.Write([uint32]$png.Length)  # image size
$bw.Write([uint32]22)           # offset (6 + 16)
$bw.Write($png)
$bw.Flush()

[System.IO.File]::WriteAllBytes($Out, $ico.ToArray())
Write-Host "Wrote $Out ($([math]::Round((Get-Item $Out).Length / 1KB, 1)) KB)"
