param(
    [string]$DwgPath,
    [switch]$OpenDwgOnly
)

. "$PSScriptRoot\config.ps1"
$cfg = Get-CoalClawConfig
$baseUrl = "http://127.0.0.1:$($cfg.HttpPort)"

function Test-PluginOnline {
    try {
        $r = Invoke-RestMethod -Uri "$baseUrl/ping" -Method Get -TimeoutSec 3
        return [bool]$r.ok
    }
    catch { return $false }
}

if (Test-PluginOnline) {
    if ($DwgPath) {
        Write-Warning "Plugin already online. DWG was not re-opened. Use open_dwg.ps1 or restart AutoCAD if needed."
    }
    Invoke-RestMethod -Uri "$baseUrl/ping" -Method Get | ConvertTo-Json -Depth 5
    return
}

if ($OpenDwgOnly -and $DwgPath) {
    & "$PSScriptRoot\open_dwg.ps1" -Path $DwgPath
    Write-Warning "Opened DWG with system default app only. Plugin may still be offline; run without -OpenDwgOnly to load plugin."
    return
}

# AutoCAD 已打开但插件未加载：COM 发 NETLOAD（OpenClaw 无法操作 NETLOAD 文件对话框）
$netloaded = & "$PSScriptRoot\netload_running_acad.ps1" -Quiet
if ($netloaded) {
    . "$PSScriptRoot\wait_for_plugin.ps1" -TimeoutSec $cfg.PingWaitSec
    if ($DwgPath) {
        Write-Warning "Plugin loaded via COM. DWG was not opened; use open_dwg.ps1 if needed."
    }
    return
}

& "$PSScriptRoot\start_autocad_with_plugin.ps1" -DwgPath $DwgPath -WaitForPlugin:$true
