param(
    [string]$DwgPath,
    [switch]$OpenDwgOnly
)

$platformRoot = Join-Path $PSScriptRoot "platforms\windows-autocad"
. "$platformRoot\config.ps1"
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
        $body = @{ path = (Resolve-Path -LiteralPath $DwgPath).Path } | ConvertTo-Json -Compress
        Invoke-RestMethod -Uri "$baseUrl/document/open" -Method Post -Body $body -ContentType "application/json" | ConvertTo-Json -Depth 5
    }
    else {
        Invoke-RestMethod -Uri "$baseUrl/ping" -Method Get | ConvertTo-Json -Depth 5
    }
    return
}

if ($OpenDwgOnly -and $DwgPath) {
    & "$platformRoot\open_dwg.ps1" -Path $DwgPath
    Write-Warning "Opened DWG with system default app only. Plugin may still be offline; run without -OpenDwgOnly to load plugin."
    return
}

$netloaded = & "$platformRoot\netload_running_acad.ps1" -Quiet
if ($netloaded) {
    . "$platformRoot\wait_for_plugin.ps1" -TimeoutSec $cfg.PingWaitSec
    if ($DwgPath) {
        $body = @{ path = (Resolve-Path -LiteralPath $DwgPath).Path } | ConvertTo-Json -Compress
        Invoke-RestMethod -Uri "$baseUrl/document/open" -Method Post -Body $body -ContentType "application/json" | ConvertTo-Json -Depth 5
    }
    return
}

& "$platformRoot\start_autocad_with_plugin.ps1" -DwgPath $DwgPath -WaitForPlugin:$true

if ($DwgPath -and (Test-PluginOnline)) {
    $body = @{ path = (Resolve-Path -LiteralPath $DwgPath).Path } | ConvertTo-Json -Compress
    Invoke-RestMethod -Uri "$baseUrl/document/open" -Method Post -Body $body -ContentType "application/json" | ConvertTo-Json -Depth 5
}
