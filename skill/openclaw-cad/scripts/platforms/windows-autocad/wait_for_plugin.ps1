param(
    [int]$TimeoutSec = 90,
    [int]$IntervalSec = 2
)

. "$PSScriptRoot\config.ps1"
$cfg = Get-CoalClawConfig
$baseUrl = "http://127.0.0.1:$($cfg.HttpPort)"
$deadline = (Get-Date).AddSeconds($TimeoutSec)

while ((Get-Date) -lt $deadline) {
    try {
        $r = Invoke-RestMethod -Uri "$baseUrl/ping" -Method Get -TimeoutSec 3
        if ($r.ok) {
            Write-Output $r
            return
        }
    }
    catch {
        Start-Sleep -Seconds $IntervalSec
    }
}

throw "Plugin HTTP not ready after ${TimeoutSec}s at $baseUrl/ping"
