param(
    [string] $OutputPath = "assets\app.ico",
    [int] $Size = 256
)

Add-Type -AssemblyName System.Drawing

$bitmap = New-Object System.Drawing.Bitmap $Size, $Size
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$padding = [Math]::Max(8, [int]($Size / 12))
$rect = New-Object System.Drawing.Rectangle $padding, $padding, ($Size - ($padding * 2)), ($Size - ($padding * 2))
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(13, 148, 136)), ([System.Drawing.Color]::FromArgb(79, 70, 229)), 35

$radius = [int]($Size / 5)
$diameter = $radius * 2
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddArc($rect.X, $rect.Y, $diameter, $diameter, 180, 90)
$path.AddArc(($rect.Right - $diameter), $rect.Y, $diameter, $diameter, 270, 90)
$path.AddArc(($rect.Right - $diameter), ($rect.Bottom - $diameter), $diameter, $diameter, 0, 90)
$path.AddArc($rect.X, ($rect.Bottom - $diameter), $diameter, $diameter, 90, 90)
$path.CloseFigure()
$graphics.FillPath($brush, $path)

$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), ([Math]::Max(10, [int]($Size / 14)))
$pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

$y1 = $Size * 0.34
$y2 = $Size * 0.66
$graphics.DrawLine($pen, ($Size * 0.25), $y1, ($Size * 0.74), $y1)
$graphics.DrawLine($pen, ($Size * 0.26), $y2, ($Size * 0.75), $y2)

$white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$graphics.FillEllipse($white, ($Size * 0.58), ($y1 - ($Size * 0.11)), ($Size * 0.22), ($Size * 0.22))
$graphics.FillEllipse($white, ($Size * 0.20), ($y2 - ($Size * 0.11)), ($Size * 0.22), ($Size * 0.22))

$pngStream = New-Object System.IO.MemoryStream
$bitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $pngStream.ToArray()

$directory = Split-Path $OutputPath -Parent
if ($directory) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

$file = [System.IO.File]::Create($OutputPath)
$writer = New-Object System.IO.BinaryWriter $file
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]1)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]32)
$writer.Write([UInt32]$pngBytes.Length)
$writer.Write([UInt32]22)
$writer.Write($pngBytes)
$writer.Dispose()

$white.Dispose()
$pen.Dispose()
$brush.Dispose()
$path.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
$pngStream.Dispose()
