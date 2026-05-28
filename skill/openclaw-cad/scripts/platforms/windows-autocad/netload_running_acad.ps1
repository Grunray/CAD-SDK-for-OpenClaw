<#
.SYNOPSIS
  向已运行的 AutoCAD 通过 COM 发送 LISP NETLOAD，不打开「选择 .NET 程序集」对话框。
  供 OpenClaw 等无法操作原生文件选择框的自动化场景使用。
#>
param(
    [switch]$Quiet
)

. "$PSScriptRoot\config.ps1"
. "$PSScriptRoot\AcadScriptHelpers.ps1"
$cfg = Get-CoalClawConfig

if (-not (Test-Path -LiteralPath $cfg.PluginDll)) {
    if (-not $Quiet) {
        throw "Plugin DLL not found: $($cfg.PluginDll). Build plugin or set COALCLAW_PLUGIN_DLL."
    }
    return $false
}

function Get-RunningAcadApplication {
    $progIds = @(
        'AutoCAD.Application.25',
        'AutoCAD.Application.24',
        'AutoCAD.Application.23',
        'AutoCAD.Application'
    )
    foreach ($id in $progIds) {
        try {
            return [Runtime.InteropServices.Marshal]::GetActiveObject($id)
        }
        catch {
            continue
        }
    }
    return $null
}

$acad = Get-RunningAcadApplication
if (-not $acad) {
    if (-not $Quiet) {
        Write-Warning "No running AutoCAD instance (COM). Start AutoCAD first or use start_autocad_with_plugin.ps1."
    }
    return $false
}

$lisp = (New-AcadNetloadScrLine -DllPath $cfg.PluginDll)
# SendCommand 需要行尾空格/换行，表示命令结束
$send = "$lisp "

try {
    $doc = $acad.ActiveDocument
    if (-not $doc) {
        if (-not $Quiet) { Write-Warning "AutoCAD has no active document." }
        return $false
    }

    [void]$doc.SendCommand($send)

    if (-not $Quiet) {
        Write-Output @{
            ok        = $true
            method    = 'COM SendCommand'
            pluginDll = $cfg.PluginDll
            lisp      = $lisp
            message   = 'NETLOAD sent to running AutoCAD. Verify with ping.ps1.'
        }
    }
    return $true
}
catch {
    if (-not $Quiet) {
        Write-Warning "COM NETLOAD failed: $($_.Exception.Message)"
    }
    return $false
}
