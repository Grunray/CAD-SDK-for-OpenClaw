# Open DWG via COM directly, then test the API again
$acad = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application.25')
Write-Output "Active doc before: $($acad.ActiveDocument.Name)"

$dwgPath = 'E:\Project\CoalClaw\AutoCAD-SDK\Assembly Sample.dwg'

try {
    $doc = $acad.Documents.Open($dwgPath)
    Write-Output "Opened DWG: $($doc.Name)"
} catch {
    Write-Output "Could not open via COM: $($_.Exception.Message)"
}

Start-Sleep -Seconds 2
Write-Output "Active doc after: $($acad.ActiveDocument.Name)"

# Now test find with some common entities
$base = 'http://127.0.0.1:54321'

Write-Output "`n=== /health ==="
try {
    $r = Invoke-RestMethod "$base/health" -Method Get -TimeoutSec 3
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch { Write-Output "Error: $($_.Exception.Message)" }

Write-Output "`n=== /find ==="
try {
    $r = Invoke-RestMethod "$base/find?q=" -Method Get -TimeoutSec 5
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch { Write-Output "Error: $($_.Exception.Message)" }

Write-Output "`n=== /zoom/extents ==="
try {
    $r = Invoke-RestMethod "$base/zoom/extents" -Method Get -TimeoutSec 5
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch { Write-Output "Error: $($_.Exception.Message)" }

Write-Output "`n=== /find?q=Assembly ==="
try {
    $r = Invoke-RestMethod "$base/find?q=Assembly" -Method Get -TimeoutSec 5
    Write-Output ($r | ConvertTo-Json -Depth 8)
} catch { Write-Output "Error: $($_.Exception.Message)" }
