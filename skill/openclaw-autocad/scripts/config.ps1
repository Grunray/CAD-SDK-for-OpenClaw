# 路径配置：可通过环境变量覆盖
$_acadExeCandidates = @(
    $env:COALCLAW_ACAD_EXE
    "D:\Program Files\Autodesk\AutoCAD 2025\acad.exe"
    "C:\Program Files\Autodesk\AutoCAD 2025\acad.exe"
) | Where-Object { $_ }
$_acadExe = $_acadExeCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $_acadExe) { $_acadExe = "C:\Program Files\Autodesk\AutoCAD 2025\acad.exe" }

$Script:CoalClawConfig = @{
    AcadExe    = $_acadExe
    PluginDll  = if ($env:COALCLAW_PLUGIN_DLL) {
        $env:COALCLAW_PLUGIN_DLL
    } else {
        $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
        Join-Path $repoRoot "plugin\src\CoalClaw.AutoCAD.Plugin\bin\Debug\net8.0-windows\CoalClaw.AutoCAD.Plugin.dll"
    }
    HttpPort   = if ($env:COALCLAW_HTTP_PORT) { [int]$env:COALCLAW_HTTP_PORT } else { 54321 }
    PingWaitSec = if ($env:COALCLAW_PING_WAIT_SEC) { [int]$env:COALCLAW_PING_WAIT_SEC } else { 90 }
    PingIntervalSec = 2
}

function Get-CoalClawConfig { $Script:CoalClawConfig }
