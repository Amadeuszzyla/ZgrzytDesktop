#Requires -Version 5.1
<#
.SYNOPSIS
    Generates Windows branding assets (ICO, wizard image) from assets/branding/zgrzyt-logo.png.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

function Save-PngToIco {
    param(
        [Parameter(Mandatory)][string] $InputPath,
        [Parameter(Mandatory)][string] $OutputPath,
        [int[]] $Sizes = @(256, 128, 64, 48, 32, 16)
    )

    $source = [System.Drawing.Image]::FromFile($InputPath)
    $bitmaps = New-Object System.Collections.Generic.List[System.Drawing.Bitmap]
    $pngData = New-Object System.Collections.Generic.List[byte[]]

    try {
        foreach ($size in $Sizes) {
            $bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighSpeed
            $graphics.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            $graphics.DrawImage($source, 0, 0, $size, $size)
            $graphics.Dispose()
            $bitmaps.Add($bitmap)

            $stream = New-Object System.IO.MemoryStream
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $pngData.Add($stream.ToArray())
            $stream.Dispose()
        }
    }
    finally {
        $source.Dispose()
    }

    $output = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($output)

    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$bitmaps.Count)

    $offset = 6 + (16 * $bitmaps.Count)
    for ($i = 0; $i -lt $bitmaps.Count; $i++) {
        $bitmap = $bitmaps[$i]
        $width = if ($bitmap.Width -ge 256) { [byte]0 } else { [byte]$bitmap.Width }
        $height = if ($bitmap.Height -ge 256) { [byte]0 } else { [byte]$bitmap.Height }

        $writer.Write($width)
        $writer.Write($height)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$pngData[$i].Length)
        $writer.Write([UInt32]$offset)
        $offset += $pngData[$i].Length
    }

    foreach ($data in $pngData) {
        $writer.Write($data)
    }

    $writer.Flush()
    [System.IO.File]::WriteAllBytes($OutputPath, $output.ToArray())
    $writer.Dispose()
    $output.Dispose()

    foreach ($bitmap in $bitmaps) {
        $bitmap.Dispose()
    }
}

function Save-ResizedPng {
    param(
        [Parameter(Mandatory)][string] $InputPath,
        [Parameter(Mandatory)][string] $OutputPath,
        [Parameter(Mandatory)][int] $Size
    )

    $source = [System.Drawing.Image]::FromFile($InputPath)
    try {
        $bitmap = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        $graphics.Clear([System.Drawing.Color]::FromArgb(255, 13, 27, 42))
        $graphics.DrawImage($source, 0, 0, $Size, $Size)
        $graphics.Dispose()
        $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $bitmap.Dispose()
    }
    finally {
        $source.Dispose()
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$brandingDir = Join-Path $repoRoot "assets\branding"
$sourcePng = Join-Path $brandingDir "zgrzyt-logo.png"
$icoPath = Join-Path $brandingDir "zgrzyt-logo.ico"
$wizardSmallPath = Join-Path $brandingDir "wizard-small.png"
$appIcoPath = Join-Path $repoRoot "ZgrzytDesktop\Assets\zgrzyt-logo.ico"

if (-not (Test-Path $sourcePng)) {
    throw "Missing source logo: $sourcePng"
}

Save-PngToIco -InputPath $sourcePng -OutputPath $icoPath
Save-ResizedPng -InputPath $sourcePng -OutputPath $wizardSmallPath -Size 55
Copy-Item -Path $icoPath -Destination $appIcoPath -Force

Write-Host "Generated: $icoPath"
Write-Host "Generated: $wizardSmallPath"
Write-Host "Copied:    $appIcoPath"
