# KeyCenter UI 自动化测试工具

这是专门用来测试 KeyCenter 界面和弹窗的**可重复使用**工具，放在仓库里，不再依赖临时脚本。

## 文件

| 文件 | 用途 |
|---|---|
| `KcUi.ps1` | 启动/关闭测试进程、查找窗口、枚举输入框、填数据、点击按钮、`PrintWindow` 截图 |
| `smoke-test.ps1` | 最小 UI 冒烟测试：启动一个隔离的测试实例，确认主窗口出现后自动强制关闭 |
| `README.md` | 本说明 |

## 安全保证

这个工具最重要的一条规则：

> **测试进程必须在 `finally` 中强制结束并复检。**

`Stop-KcUiTestApp` 会：

1. 对本次启动的进程执行 `Kill()`；
2. `WaitForExit(5000)`；
3. 再检查该 PID 是否还存在；
4. 最后删除隔离测试目录。

所以即使测试脚本中途报错，只要使用标准模板，就不会再留下需要你手动关闭的窗口。

## 快速使用

```powershell
. .\tests\ui\KcUi.ps1

$ctx = $null
try {
    $ctx = Start-KcUiTestApp -Exe .\KeyCenter.exe
    $win = Wait-KcWindow -ProcessId $ctx.Process.Id -TimeoutMs 15000

    # 枚举输入框并填测试密码
    $edits = Get-KcChildWindows -Hwnd $win.Handle |
        Where-Object { $_.Class -match 'EDIT' }
    if ($edits.Count -ge 2) {
        Set-KcText -Hwnd $edits[0].Handle -Text 'test-password-123'
        Set-KcText -Hwnd $edits[1].Handle -Text 'test-password-123'
    }

    # 按文字找按钮并点击
    $buttons = Get-KcChildWindows -Hwnd $win.Handle |
        Where-Object { $_.Class -match 'BUTTON' }
    $create = $buttons | Where-Object { $_.Text -match '创建|Create' } | Select-Object -First 1
    if ($create) { Invoke-KcButton -Hwnd $create.Handle }

    # 截图
    Save-KcWindowShot -Hwnd $win.Handle -Path .\smoke.png
}
finally {
    Stop-KcUiTestApp -Context $ctx
}
```

## 快速冒烟测试

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\ui\smoke-test.ps1
```

它会在临时目录复制一份 `KeyCenter.exe`，使用独立配置和独立测试数据，不触碰真实保险箱；测试结束后自动关闭并清理。

## 函数一览

| 函数 | 说明 |
|---|---|
| `Start-KcUiTestApp` | 复制 exe 到隔离临时目录，写测试配置，启动进程 |
| `Stop-KcUiTestApp` | 强制结束本次测试进程、等待退出、复检 PID、清理目录 |
| `Wait-KcWindow` | 等待目标进程的可见顶层窗口 |
| `Get-KcTopWindows` | 枚举某进程的所有顶层窗口 |
| `Get-KcChildWindows` | 递归枚举子控件（输入框、按钮等） |
| `Set-KcText` | 向控件发送 `WM_SETTEXT` 填值 |
| `Invoke-KcButton` | 向按钮发送 `BM_CLICK` |
| `Save-KcWindowShot` | 用 `PrintWindow` 截图，不依赖当前桌面是否可见 |

## 注意

- 只使用隔离的临时测试数据，不要把真实 `keycenter.dat` 传进来；
- 测试过程和截图只用于验证，不要把测试数据提交到仓库；
- 如果新增了窗口/控件，优先复用 `KcUi.ps1`，不要再写一次性 C# 小工具；
- 任何直接 `Start-Process KeyCenter.exe` 的测试都必须套用同样的 `try/finally` 清理模板。
