$base = 'http://127.0.0.1:54321'

# Get COM objects
$acad = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application.25')
$doc = $acad.ActiveDocument

Write-Output ("=== Current drawing ===")
Write-Output ("Name: " + $doc.Name)

# List entities via COM for reference
Write-Output ("`n=== Entity list (COM) ===")
$model = $doc.ModelSpace
$count = $model.Count
Write-Output ("Total entities: " + $count)

$limit = [Math]::Min($count, 20)
for ($i = 0; $i -lt $limit; $i++) {
    try {
        $ent = $model.Item($i)
        Write-Output ("  #" + $i + ": Handle=" + $ent.Handle + " Type=" + $ent.ObjectName + " Layer=" + $ent.Layer)
    } catch {
        Write-Output ("  #" + $i + ": Error getting entity")
    }
}

# Find tests with ASCII-only queries
Write-Output ("`n=== /find tests ===")
$queries = @('', 'a', 'A', 'text', 'assembly', 'part', 'sample')
foreach ($q in $queries) {
    try {
        $enc = [uri]::EscapeDataString($q)
        $r = Invoke-RestMethod ($base + "/find?q=" + $enc) -Method Get -TimeoutSec 5
        if ($r.count -gt 0) {
            Write-Output ("find(" + $q + "): " + $r.count + " matches")
        } else {
            Write-Output ("find(" + $q + "): 0 matches")
        }
    } catch { 
        Write-Output ("find(" + $q + "): " + $_.Exception.Message)
    }
}

# Test /zoom/to with first entity handle
Write-Output ("`n=== /zoom/to test ===")
if ($count -gt 0) {
    $firstEnt = $model.Item(0)
    $handle = $firstEnt.Handle
    Write-Output ("Zooming to handle=" + $handle)
    try {
        $r = Invoke-RestMethod ($base + "/zoom/to?handle=" + $handle) -Method Get -TimeoutSec 5
        Write-Output ($r | ConvertTo-Json -Depth 8)
    } catch { Write-Output ("Error: " + $_.Exception.Message) }
}
