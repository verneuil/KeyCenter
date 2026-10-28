#requires -Version 5.1
# KeyCenter UI test helper.
# 使用方法见同目录 README.md；测试进程必须在 finally 中调用 Stop-KcUiTestApp。

$ErrorActionPreference = 'Stop'

if (-not ('KcUiNative' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;

public static class KcUiNative
{
    public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumProc cb, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumChildWindows(IntPtr parent, EnumProc cb, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int max);
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, string lParam);
    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
}
'@
}

function Get-KcWindowText {
    param([IntPtr]$Hwnd)
    $sb = New-Object System.Text.StringBuilder 512
    [void][KcUiNative]::GetWindowText($Hwnd, $sb, $sb.Capacity)
    return $sb.ToString()
}

function Get-KcWindowClass {
    param([IntPtr]$Hwnd)
    $sb = New-Object System.Text.StringBuilder 256
    [void][KcUiNative]::GetClassName($Hwnd, $sb, $sb.Capacity)
    return $sb.ToString()
}

function Get-KcWindowRect {
    param([IntPtr]$Hwnd)
    $r = New-Object KcUiNative+RECT
    if (-not [KcUiNative]::GetWindowRect($Hwnd, [ref]$r)) { return $null }
    return [pscustomobject]@{
        Left = $r.Left; Top = $r.Top; Right = $r.Right; Bottom = $r.Bottom
        Width = $r.Right - $r.Left; Height = $r.Bottom - $r.Top
    }
}

function Get-KcTopWindows {
    param([int]$ProcessId)
    $result = New-Object System.Collections.ArrayList
    $callback = [KcUiNative+EnumProc]{
        param([IntPtr]$hWnd, [IntPtr]$lParam)
        [uint32]$pid = 0
        [void][KcUiNative]::GetWindowThreadProcessId($hWnd, [ref]$pid)
        if ($pid -eq $ProcessId -and [KcUiNative]::IsWindowVisible($hWnd)) {
            $rect = Get-KcWindowRect -Hwnd $hWnd
            [void]$result.Add([pscustomobject]@{
                Handle = $hWnd
                ProcessId = [int]$pid
                Title = Get-KcWindowText -Hwnd $hWnd
                Class = Get-KcWindowClass -Hwnd $hWnd
                Rect = $rect
            })
        }
        return $true
    }
    [void][KcUiNative]::EnumWindows($callback, [IntPtr]::Zero)
    return @($result)
}

function Wait-KcWindow {
    param(
        [Parameter(Mandatory = $true)][int]$ProcessId,
        [int]$TimeoutMs = 15000
    )
    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $proc = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
        if (-not $proc) { throw "KeyCenter 测试进程已退出（PID $ProcessId）。" }
        $wins = @(Get-KcTopWindows -ProcessId $ProcessId | Where-Object { $_.Title -ne '' })
        if ($wins.Count -gt 0) { return $wins | Select-Object -First 1 }
        Start-Sleep -Milliseconds 150
    }
    throw "等待 KeyCenter 窗口超时（PID $ProcessId）。"
}

function Get-KcChildWindows {
    param([Parameter(Mandatory = $true)][IntPtr]$Hwnd)
    $result = New-Object System.Collections.ArrayList
    $callback = [KcUiNative+EnumProc]{
        param([IntPtr]$child, [IntPtr]$lParam)
        [void]$result.Add([pscustomobject]@{
            Handle = $child
            Class = Get-KcWindowClass -Hwnd $child
            Text = Get-KcWindowText -Hwnd $child
            Rect = Get-KcWindowRect -Hwnd $child
        })
        return $true
    }
    [void][KcUiNative]::EnumChildWindows($Hwnd, $callback, [IntPtr]::Zero)
    return @($result)
}

function Set-KcText {
    param(
        [Parameter(Mandatory = $true)][IntPtr]$Hwnd,
        [Parameter(Mandatory = $true)][string]$Text
    )
    [void][KcUiNative]::SendMessage($Hwnd, 0x000C, [IntPtr]::Zero, $Text) # WM_SETTEXT
    Start-Sleep -Milliseconds 60
}

function Invoke-KcButton {
    param([Parameter(Mandatory = $true)][IntPtr]$Hwnd)
    [void][KcUiNative]::SendMessage($Hwnd, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) # BM_CLICK
    Start-Sleep -Milliseconds 120
}

function Save-KcWindowShot {
    param(
        [Parameter(Mandatory = $true)][IntPtr]$Hwnd,
        [Parameter(Mandatory = $true)][string]$Path
    )
    Add-Type -AssemblyName System.Drawing
    $rect = Get-KcWindowRect -Hwnd $Hwnd
    if (-not $rect -or $rect.Width -le 0 -or $rect.Height -le 0) {
        throw "无法获取窗口尺寸。"
    }
    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $bmp = New-Object System.Drawing.Bitmap $rect.Width, $rect.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $hdc = $g.GetHdc()
        try { [void][KcUiNative]::PrintWindow($Hwnd, $hdc, 2) }
        finally { $g.ReleaseHdc($hdc) }
        $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $g.Dispose()
        $bmp.Dispose()
    }
}

function Start-KcUiTestApp {
    param(
        [Parameter(Mandatory = $true)][string]$Exe,
        [string]$DataPath,
        [switch]$KeepTemp
    )
    $exePath = (Resolve-Path $Exe).Path
    $tempDir = Join-Path $env:TEMP ('kc-ui-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    try {
        $localExe = Join-Path $tempDir 'KeyCenter.exe'
        Copy-Item $exePath $localExe -Force
        Set-Content -Path (Join-Path $tempDir 'keycenter.cfg') -Value "theme=light`r`nlanguage=zh" -Encoding ASCII
        if (-not $DataPath) { $DataPath = Join-Path $tempDir 'test.dat' }
        $proc = Start-Process -FilePath $localExe `
            -ArgumentList @('--data', $DataPath, '--no-glass') `
            -WorkingDirectory $tempDir -PassThru
        return [pscustomobject]@{
            Process = $proc
            Exe = $localExe
            DataPath = $DataPath
            TempDir = $tempDir
            KeepTemp = [bool]$KeepTemp
        }
    }
    catch {
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        throw
    }
}

function Stop-KcUiTestApp {
    param(
        [Parameter(Mandatory = $false)]$Context,
        [switch]$KeepTemp
    )
    if ($null -eq $Context) { return }
    $proc = $Context.Process
    if ($proc) {
        try {
            if (-not $proc.HasExited) {
                $proc.Kill()
                [void]$proc.WaitForExit(5000)
            }
        }
        catch { }
        Start-Sleep -Milliseconds 250
        $left = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
        if ($left) {
            throw "测试进程仍存在（PID $($proc.Id)），请检查。"
        }
    }
    if (-not $KeepTemp -and -not $Context.KeepTemp -and $Context.TempDir) {
        Remove-Item $Context.TempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
