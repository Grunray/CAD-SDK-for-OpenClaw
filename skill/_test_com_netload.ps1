$acad = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application.25')
Write-Output ("AutoCAD version: " + $acad.Version)

# Check active document
Write-Output ("Active doc: " + $acad.ActiveDocument.Name)

# Open the DWG file if not already
$dwgPath = 'E:\Project\CoalClaw\AutoCAD-SDK\Assembly Sample.dwg'
try {
    $doc = $acad.Documents.Open($dwgPath)
    Write-Output ("Opened: " + $doc.Name)
} catch {
    Write-Output ("DWg may already be open: " + $_.Exception.Message)
}
Write-Output ("Documents count: " + $acad.Documents.Count)

# NETLOAD the plugin DLL via SendCommand (as if typing in command line)
$pluginDll = 'E:\Project\CoalClaw\AutoCAD-SDK\autocad-sdk-for-openclaw\plugin\src\CoalClaw.AutoCAD.Plugin\bin\Debug\net8.0-windows\CoalClaw.AutoCAD.Plugin.dll'
$pluginForward = $pluginDll -replace '\\', '/'

# SendCommand emulates keyboard input — just send the command string + enter
$cmd = "._NETLOAD `"$pluginForward`" "
Write-Output ("Sending command: " + $cmd.Trim())

$acad.ActiveDocument.SendCommand($cmd)
Write-Output "NETLOAD sent. Waiting for plugin HTTP API..."

# Wait for plugin to come online
$maxWait = 30
for ($i = 0; $i -lt $maxWait; $i++) {
    Start-Sleep -Seconds 1
    try {
        $r = Invoke-RestMethod -Uri 'http://127.0.0.1:54321/ping' -Method Get -TimeoutSec 2
        Write-Output ("Plugin online after " + ($i + 1) + "s")
        Write-Output (ConvertTo-Json $r -Depth 3)
        exit 0
    } catch {
        # still waiting
    }
}
Write-Output "Plugin did not come online within ${maxWait}s."
exit 1
