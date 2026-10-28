#requires -Version 5.1
# 最小 UI 冒烟测试：启动隔离测试实例，等待主窗口，截图，然后强制关闭。
# 该脚本会短暂弹出测试窗口；结束时无论成功失败都会关闭并清理。

param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\..\KeyCenter.exe'),
    [string]$Screenshot = ''
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'KcUi.ps1')

$ctx = $null
try {
    if (-not (Test-Path $Exe)) {
        throw "找不到 KeyCenter.exe：$Exe"
    }
    $ctx = Start-KcUiTestApp -Exe $Exe
    $win = Wait-KcWindow -ProcessId $ctx.Process.Id -TimeoutMs 15000
    Write-Output ("UI_SMOKE_WINDOW_OK title=" + $win.Title)

    if ($Screenshot) {
        Save-KcWindowShot -Hwnd $win.Handle -Path $Screenshot
        Write-Output ("UI_SMOKE_SCREENSHOT " + (Resolve-Path $Screenshot).Path)
    }

    Write-Output 'UI_SMOKE PASSED'
}
finally {
    if ($ctx) { Stop-KcUiTestApp -Context $ctx }
    $running = @(Get-Process KeyCenter -ErrorAction SilentlyContinue)
    Write-Output ("UI_SMOKE_PROCESSES=" + $running.Count)
}
