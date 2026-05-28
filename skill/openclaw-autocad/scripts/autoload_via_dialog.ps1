<#
.SYNOPSIS
  通过 COM 打开 NETLOAD 文件对话框，并用 Win32 API 填入 DLL 路径、处理 SECURELOAD 安全弹窗。
  仅 Windows + 本机 AutoCAD。OpenClaw 在 Linux 上不可运行此脚本。

  优先仍建议使用 ensure_autocad_ready.ps1（无弹窗 COM LISP）。
  当 LISP NETLOAD 被 SECURELOAD 拦截、必须走文件对话框时使用本脚本。
#>
param(
    [switch]$SkipPingCheck,
    [switch]$WaitForPlugin
)

. "$PSScriptRoot\config.ps1"
. "$PSScriptRoot\Win32DialogHelpers.ps1"
$cfg = Get-CoalClawConfig
$baseUrl = "http://127.0.0.1:$($cfg.HttpPort)"

if (-not $SkipPingCheck) {
    try {
        $r = Invoke-RestMethod -Uri "$baseUrl/ping" -Method Get -TimeoutSec 3
        if ($r.ok) {
            Write-Output @{ ok = $true; skipped = $true; message = 'Plugin already online.' }
            return
        }
    }
    catch { }
}

if (-not (Test-Path -LiteralPath $cfg.PluginDll)) {
    throw "Plugin DLL not found: $($cfg.PluginDll). Build plugin or set COALCLAW_PLUGIN_DLL."
}

function Get-RunningAcadApplication {
    foreach ($id in @('AutoCAD.Application.25', 'AutoCAD.Application.24', 'AutoCAD.Application.23', 'AutoCAD.Application')) {
        try { return [Runtime.InteropServices.Marshal]::GetActiveObject($id) }
        catch { continue }
    }
    return $null
}

$acad = Get-RunningAcadApplication
if (-not $acad) {
    throw 'No running AutoCAD (COM). Start AutoCAD first, or use start_autocad_with_plugin.ps1.'
}

$doc = $acad.ActiveDocument
if (-not $doc) {
    throw 'AutoCAD has no active document. Open a drawing first.'
}

$dllPath = $cfg.PluginDll

# SendCommand("NETLOAD\n") 会阻塞直到对话框关闭；并行 Job 用 Win32 填表 + 点安全弹窗
$dialogJob = Start-Job -ArgumentList $dllPath -ScriptBlock {
    param($Path)
    . "$using:PSScriptRoot\Win32DialogHelpers.ps1"
    Invoke-NetloadFileDialogAutomation -DllPath $Path
}

try {
    # Step 1 — COM 发送 NETLOAD，打开模态文件对话框（阻塞至对话框流程结束）
    [void]$doc.SendCommand("NETLOAD`n")

    $jobResult = Wait-Job -Job $dialogJob -Timeout 60
    if (-not $jobResult) {
        Stop-Job -Job $dialogJob -Force
        throw 'Dialog automation timed out after 60s.'
    }

    $automation = Receive-Job -Job $dialogJob
    if ($automation -is [System.Management.Automation.ErrorRecord]) {
        throw $automation
    }
}
finally {
    Remove-Job -Job $dialogJob -Force -ErrorAction SilentlyContinue
}

$result = @{
    ok          = $true
    method      = 'COM NETLOAD + Win32 dialog'
    pluginDll   = ([System.IO.Path]::GetFullPath($dllPath))
    automation  = $automation
    message     = 'NETLOAD dialog automation finished. Verify with ping.ps1.'
}
Write-Output $result

if ($WaitForPlugin) {
    . "$PSScriptRoot\wait_for_plugin.ps1" -TimeoutSec $cfg.PingWaitSec
}
