# Key Center regression gate.
# Run: powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\regression.ps1
# Steps: build -> encryption self-test -> localization/metadata audit -> binary version/privacy check.
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$fail = $false

function Fail([string]$message) {
    Write-Host ('FAIL: ' + $message) -ForegroundColor Red
    $script:fail = $true
}

if (-not $SkipBuild) {
    Write-Host '== Build =='
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'build.ps1')
    if ($LASTEXITCODE -ne 0) { Fail 'build.ps1 failed'; exit 1 }
}

$exe = Join-Path $root 'KeyCenter.exe'
if (-not (Test-Path $exe)) { Fail 'KeyCenter.exe was not produced'; exit 1 }

Write-Host '== Encryption / file I/O self-test =='
$log = Join-Path $env:TEMP ('kc-selftest-' + [guid]::NewGuid().ToString('N') + '.log')
$p = Start-Process -FilePath $exe -ArgumentList '--selftest', $log -PassThru -Wait
if ($p.ExitCode -ne 0) { Fail ('selftest exit code ' + $p.ExitCode) }
$logText = ''
if (Test-Path $log) { $logText = Get-Content $log -Raw -Encoding UTF8; Remove-Item $log -Force }
if ($logText -notmatch 'SELFTEST OK') { Fail 'SELFTEST OK marker missing' }
else { Write-Host 'SELFTEST OK' -ForegroundColor Green }

Write-Host '== Localization / metadata audit =='
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'audit-localization.ps1')
if ($LASTEXITCODE -ne 0) { Fail 'localization/metadata audit failed' }

Write-Host '== Binary version and privacy scan =='
$vi = (Get-Item $exe).VersionInfo
if ($vi.FileVersion -ne '1.8.6.0') { Fail ('unexpected FileVersion: ' + $vi.FileVersion) }
$bytes = [System.IO.File]::ReadAllBytes($exe)
$ascii = [System.Text.Encoding]::ASCII.GetString($bytes)
$uni = [System.Text.Encoding]::Unicode.GetString($bytes)
foreach ($needle in @('lvshu', 'C:\Users', 'D:\Others')) {
    if ($ascii.Contains($needle) -or $uni.Contains($needle)) { Fail ('private string found in binary: ' + $needle) }
}
if ($ascii.Contains('sk-test-') -or $ascii.Contains('sk-demo-')) { Fail 'secret-looking demo token found in binary' }

Write-Host '== Git worktree =='
if (Test-Path (Join-Path $root '.git')) {
    $status = (& git -C $root status --short) -join "`n"
    if ($status.Trim().Length -ne 0) { Write-Host ('WARN: uncommitted changes:' + "`n" + $status) -ForegroundColor Yellow }
}

if ($fail) {
    Write-Host 'REGRESSION FAILED' -ForegroundColor Red
    exit 1
}
Write-Host 'REGRESSION PASSED' -ForegroundColor Green
exit 0
