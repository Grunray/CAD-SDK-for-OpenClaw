param(
    [string]$DwgPath,
    [switch]$WaitForPlugin
)

. "$PSScriptRoot\config.ps1"
. "$PSScriptRoot\AcadScriptHelpers.ps1"
$cfg = Get-CoalClawConfig

if (-not (Test-Path -LiteralPath $cfg.AcadExe)) {
    throw "AutoCAD not found: $($cfg.AcadExe). Set COALCLAW_ACAD_EXE."
}
if (-not (Test-Path -LiteralPath $cfg.PluginDll)) {
    throw "Plugin DLL not found: $($cfg.PluginDll). Build plugin first or set COALCLAW_PLUGIN_DLL."
}

$lines = New-Object System.Collections.Generic.List[string]
# 先 NETLOAD 插件，再打开图纸
$lines.Add((New-AcadNetloadScrLine -DllPath $cfg.PluginDll))

if ($DwgPath) {
    $dwgFull = (Resolve-Path -LiteralPath $DwgPath).Path
    $lines.Add((New-AcadOpenScrLine -DwgPath $dwgFull))
}

$scrPath = Join-Path ([System.IO.Path]::GetTempPath()) "coalclaw-start-$(Get-Date -Format 'yyyyMMddHHmmss').scr"
# UTF-8 无 BOM，避免 AutoCAD 脚本解析异常
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllLines($scrPath, $lines, $utf8NoBom)

$args = @('/nologo', '/b', "`"$scrPath`"")
Start-Process -FilePath $cfg.AcadExe -ArgumentList $args -WorkingDirectory (Split-Path $cfg.AcadExe)

Write-Output @{
    ok         = $true
    acadExe    = $cfg.AcadExe
    pluginDll  = $cfg.PluginDll
    scriptPath = $scrPath
    scriptBody = ($lines -join [Environment]::NewLine)
    dwgPath    = if ($DwgPath) { $dwgFull } else { $null }
    message    = "AutoCAD started with LISP NETLOAD in script"
}

if ($WaitForPlugin) {
    . "$PSScriptRoot\wait_for_plugin.ps1" -TimeoutSec $cfg.PingWaitSec
}
