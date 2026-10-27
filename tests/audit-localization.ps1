# Static checks for localization and release metadata.
# Run: powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\audit-localization.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$errors = New-Object System.Collections.Generic.List[string]

$cjk = [regex]'[\u4e00-\u9fff]'
$lit = [regex]'"(?:\\.|[^"\\\r\n])*"'
$files = Get-ChildItem (Join-Path $root '*.cs') | Where-Object { $_.Name -ne 'I18n.cs' }

foreach ($f in $files) {
    $lines = [System.IO.File]::ReadAllLines($f.FullName, [System.Text.Encoding]::UTF8)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line.TrimStart().StartsWith('//')) { continue }
        foreach ($m in $lit.Matches($line)) {
            if (-not $cjk.IsMatch($m.Value)) { continue }
            $before = $line.Substring(0, $m.Index)
            if ($before.EndsWith('L.T(')) { continue }
            if ($line -like '*AssemblyDescription*') { continue }
            $errors.Add(('hard-coded UI text in {0}:{1}: {2}' -f $f.Name, ($i + 1), $line.Trim()))
        }
    }
}

$source = Get-Content (Join-Path $root '*.cs') -Raw -Encoding UTF8
$keys = [regex]::Matches($source, 'L\.T\("((?:\\.|[^"\\])*)"\)') |
    ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
$dictLines = [System.IO.File]::ReadAllLines((Join-Path $root 'I18n.cs'), [System.Text.Encoding]::UTF8)
$dict = @{}
$entry = [regex]'^\s*\{\s*"((?:\\.|[^"\\])*)"\s*,\s*"(?:\\.|[^"\\])*"\s*\},?\s*$'
foreach ($line in $dictLines) {
    $m = $entry.Match($line)
    if ($m.Success) { $dict[$m.Groups[1].Value] = $true }
}
$missing = @($keys | Where-Object { -not $dict.ContainsKey($_) })
foreach ($k in $missing) { $errors.Add('missing English translation: ' + $k) }

# Version consistency
$prog = Get-Content (Join-Path $root 'Program.cs') -Raw -Encoding UTF8
$manifest = Get-Content (Join-Path $root 'app.manifest') -Raw -Encoding UTF8
$pm = [regex]::Match($prog, 'AssemblyVersion\("([0-9.]+)"\)')
$fm = [regex]::Match($prog, 'AssemblyFileVersion\("([0-9.]+)"\)')
$mm = [regex]::Match($manifest, 'assemblyIdentity version="([0-9.]+)"')
if (-not $pm.Success -or -not $fm.Success -or -not $mm.Success) {
    $errors.Add('cannot read version metadata')
} elseif ($pm.Groups[1].Value -ne $fm.Groups[1].Value -or $pm.Groups[1].Value -ne $mm.Groups[1].Value) {
    $errors.Add('version mismatch: Program.cs / app.manifest differ')
}

$dialogs = Get-Content (Join-Path $root 'Dialogs.cs') -Raw -Encoding UTF8
if ($dialogs -notmatch 'FormBorderStyle\s*=\s*FormBorderStyle\.None') {
    $errors.Add('DarkForm must be borderless so no native duplicate dialog caption is shown')
}

if ($errors.Count -gt 0) {
    Write-Host ('LOCALIZATION/METADATA AUDIT FAILED (' + $errors.Count + ')') -ForegroundColor Red
    $errors | ForEach-Object { Write-Host ('  - ' + $_) -ForegroundColor Red }
    exit 1
}

Write-Host ('LOCALIZATION/METADATA AUDIT OK (' + $keys.Count + ' keys, version ' + $pm.Groups[1].Value + ')') -ForegroundColor Green
exit 0
