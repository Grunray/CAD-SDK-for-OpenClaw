$acadExe = "D:\Program Files\Autodesk\AutoCAD 2025\acad.exe"
$pluginDll = "E:\Project\CoalClaw\AutoCAD-SDK\autocad-sdk-for-openclaw\plugin\src\CoalClaw.AutoCAD.Plugin\bin\Debug\net8.0-windows\CoalClaw.AutoCAD.Plugin.dll"
$dwgPath = "E:\Project\CoalClaw\AutoCAD-SDK\Assembly Sample.dwg"

$pluginForward = ($pluginDll -replace '\\', '/')
$dwgForward = ($dwgPath -replace '\\', '/')

$scrLines = @(
    '(command "._NETLOAD" "' + $pluginForward + '")',
    '(command "._OPEN" "' + $dwgForward + '")',
    ''
)

$scrPath = Join-Path ([System.IO.Path]::GetTempPath()) "coalclaw-auto-$(Get-Date -Format 'yyyyMMddHHmmss').scr"
[System.IO.File]::WriteAllText($scrPath, ($scrLines -join "`r`n"), [System.Text.UTF8Encoding]::new($false))

Write-Output "=== SCR script ==="
Get-Content $scrPath -Raw
Write-Output "=================="

Write-Output "Starting AutoCAD..."
Start-Process -FilePath $acadExe -ArgumentList @('/nologo', '/b', "`"$scrPath`"") -WorkingDirectory (Split-Path $acadExe)
Write-Output "AutoCAD process launched. Waiting 15s for plugin HTTP API..."
Start-Sleep -Seconds 15
Write-Output "Done waiting."
