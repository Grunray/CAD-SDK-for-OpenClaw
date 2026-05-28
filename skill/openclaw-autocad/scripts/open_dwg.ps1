param(
    [Parameter(Mandatory = $true)][string]$Path
)

$fullPath = Resolve-Path -LiteralPath $Path -ErrorAction Stop
if ($fullPath.Extension -notmatch '\.(dwg|dwt|dws)$') {
    Write-Warning "Expected a DWG/DWT/DWS file: $fullPath"
}

# 使用系统默认关联程序打开（Windows 上通常为 AutoCAD，若已关联）
Start-Process -FilePath $fullPath.Path

Write-Output @{
    ok       = $true
    path     = $fullPath.Path
    method   = "Start-Process (system default association)"
    platform = "windows"
}
