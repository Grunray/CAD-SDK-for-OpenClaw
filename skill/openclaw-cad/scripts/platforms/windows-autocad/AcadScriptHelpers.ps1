# AutoCAD .scr 中不要用「NETLOAD + 第二行带引号路径」——第二行会被当成未知命令。
# 应使用 LISP (command "._NETLOAD" "path")，路径用正斜杠。

function Convert-ToAcadPath {
    param([string]$Path)
    return ([System.IO.Path]::GetFullPath($Path) -replace '\\', '/')
}

function New-AcadNetloadScrLine {
    param([string]$DllPath)
    $p = Convert-ToAcadPath $DllPath
    return "(command ""._NETLOAD"" ""$p"")"
}

function New-AcadOpenScrLine {
    param([string]$DwgPath)
    $p = Convert-ToAcadPath $DwgPath
    return "(command ""._OPEN"" ""$p"")"
}

Export-ModuleMember -Function Convert-ToAcadPath, New-AcadNetloadScrLine, New-AcadOpenScrLine
