# Key Center 一键构建脚本
# 使用 Windows 自带的 .NET Framework 编译器，不需要安装 Visual Studio / .NET SDK。
# 用法：右键本文件 ->「使用 PowerShell 运行」，或在 PowerShell 5.1 中执行：
#   powershell -ExecutionPolicy Bypass -File .\build.ps1

$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $dir

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    throw "找不到 .NET Framework 4.x 编译器：$csc"
}

Add-Type -AssemblyName System.Drawing

# ---------- 1. 生成应用图标 keycenter.ico（多尺寸，PNG 帧） ----------
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = New-Object System.Collections.ArrayList

foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $rect = New-Object System.Drawing.RectangleF(0, 0, $s, $s)
    $rad = [Math]::Max(1.5, $s * 0.22)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $rad * 2
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($s - $d, 0, $d, $d, 270, 90)
    $path.AddArc($s - $d, $s - $d, $d, $d, 0, 90)
    $path.AddArc(0, $s - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $c1 = [System.Drawing.Color]::FromArgb(255, 96, 165, 255)
    $c2 = [System.Drawing.Color]::FromArgb(255, 124, 108, 255)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, 45)
    $g.FillPath($brush, $path)

    # 白色锁孔（圆 + 梯形）
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $cr = $s * 0.16
    $cx = $s / 2.0
    $cy = $s * 0.40
    $g.FillEllipse($white, [float]($cx - $cr), [float]($cy - $cr), [float]($cr * 2), [float]($cr * 2))

    $pts = New-Object 'System.Drawing.PointF[]' 4
    $pts[0] = New-Object System.Drawing.PointF([float]($cx - $s * 0.055), [float]($cy + $cr * 0.5))
    $pts[1] = New-Object System.Drawing.PointF([float]($cx + $s * 0.055), [float]($cy + $cr * 0.5))
    $pts[2] = New-Object System.Drawing.PointF([float]($cx + $s * 0.115), [float]($s * 0.78))
    $pts[3] = New-Object System.Drawing.PointF([float]($cx - $s * 0.115), [float]($s * 0.78))
    $g.FillPolygon($white, $pts)

    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $frames.Add($ms.ToArray()) | Out-Null
    $ms.Dispose()
    $bmp.Dispose()
}

$icoPath = Join-Path $dir 'keycenter.ico'
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $data = $frames[$i]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim)
    $bw.Write([byte]$dim)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $data.Length
}
for ($i = 0; $i -lt $frames.Count; $i++) { $bw.Write($frames[$i]) }
$bw.Flush()
$bw.Dispose()
$fs.Dispose()
Write-Host "图标已生成：keycenter.ico"

# ---------- 2. 编译 ----------
$src = @(
    'Program.cs',
    'Theme.cs',
    'Vault.cs',
    'UiControls.cs',
    'Motion.cs',
    'WheelRouter.cs',
    'Extras.cs',
    'Scroller.cs',
    'Dialogs.cs',
    'MainForm.cs'
)

& $csc /nologo /target:winexe /optimize+ /warn:4 /codepage:65001 `
    /out:KeyCenter.exe `
    /win32icon:keycenter.ico `
    /win32manifest:app.manifest `
    /r:System.Runtime.Serialization.dll `
    /r:System.Security.dll `
    /r:System.Windows.Forms.dll `
    /r:System.Drawing.dll `
    @src

if ($LASTEXITCODE -ne 0) {
    throw "编译失败，退出码 $LASTEXITCODE"
}

Write-Host ""
Write-Host "构建完成：$(Join-Path $dir 'KeyCenter.exe')"
Write-Host "双击 KeyCenter.exe 即可运行（绿色免安装）。"
