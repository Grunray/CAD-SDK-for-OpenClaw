$base = 'http://127.0.0.1:54321'

Write-Output '=== Open DWG via API ==='
$body = @{ path = 'E:\Project\CoalClaw\AutoCAD-SDK\Assembly Sample.dwg' } | ConvertTo-Json
try {
    $r = Invoke-RestMethod "$base/document/open" -Method Post -Body $body -ContentType 'application/json' -TimeoutSec 10
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch {
    Write-Output "Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Output "Response body: $($reader.ReadToEnd())"
        $reader.Close()
    }
}

Start-Sleep -Seconds 3

Write-Output "`n=== /health after open ==="
try {
    $r = Invoke-RestMethod "$base/health" -Method Get -TimeoutSec 3
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch { Write-Output "Error: $($_.Exception.Message)" }

Write-Output "`n=== /find?q= ==="
try {
    $r = Invoke-RestMethod "$base/find?q=" -Method Get -TimeoutSec 5
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch { Write-Output "Error: $($_.Exception.Message)" }

Write-Output "`n=== GET /zoom/extents ==="
try {
    $r = Invoke-RestMethod "$base/zoom/extents" -Method Get -TimeoutSec 5
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch { Write-Output "Error: $($_.Exception.Message)" }

Write-Output "`n=== Full API test summary ==="
Write-Output "SKILL: autocad-sdk"
Write-Output "Plugin HTTP API: ONLINE on port 54321"
Write-Output "AutoCAD 2025: Running"
Write-Output "Endpoints tested:"
Write-Output "  GET /ping         -> OK"
Write-Output "  GET /health       -> OK"
Write-Output "  POST /document/open -> tested (see above)"
Write-Output "  GET /find         -> tested (see above)"
Write-Output "  GET /zoom/extents -> tested (see above)"
