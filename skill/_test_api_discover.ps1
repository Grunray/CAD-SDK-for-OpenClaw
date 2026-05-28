$baseUrl = 'http://127.0.0.1:54321'

Write-Output "Root endpoint:"
try {
    $r = Invoke-RestMethod -Uri "$baseUrl/" -Method Get -TimeoutSec 3
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch { Write-Output "Error: $($_.Exception.Message)" }

Write-Output "`nFind entity (empty string - list all):"
try {
    $r = Invoke-RestMethod -Uri "$baseUrl/find?q=" -Method Get -TimeoutSec 3
    Write-Output "count=$($r.count)"
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch { Write-Output "Error: $($_.Exception.Message)" }

Write-Output "`nDocument info:"
try {
    $r = Invoke-RestMethod -Uri "$baseUrl/document" -Method Get -TimeoutSec 3
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch { Write-Output "Error: $($_.Exception.Message)" }

Write-Output "`nEntities:"
try {
    $r = Invoke-RestMethod -Uri "$baseUrl/entities" -Method Get -TimeoutSec 3
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch { Write-Output "Error: $($_.Exception.Message)" }

Write-Output "`nZoom endpoints:"
try {
    $r = Invoke-RestMethod -Uri "$baseUrl/zoom/extents" -Method Post -TimeoutSec 3
    Write-Output "zoom/extents: $($r | ConvertTo-Json -Depth 8)"
} catch { Write-Output "zoom/extents error: $($_.Exception.Message)" }
