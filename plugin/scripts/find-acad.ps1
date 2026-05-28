# 在本机查找 acdbmgd.dll 并可选写入 Directory.Build.props.user
param(
    [switch]$WriteProps,
    [switch]$Quiet
)

function Write-Info($msg) {
    if (-not $Quiet) { Write-Host $msg }
}

$candidates = [System.Collections.Generic.List[string]]::new()

# 1. 环境变量
if ($env:AcadInstallDir) {
    $dir = $env:AcadInstallDir.TrimEnd('\') + '\'
    if (Test-Path (Join-Path $dir 'acdbmgd.dll')) { $candidates.Add($dir) }
}

# 2. 注册表 AcadLocation（R24/R25/R26 等）
$regRoots = @(
    'HKLM:\SOFTWARE\Autodesk\AutoCAD',
    'HKLM:\SOFTWARE\WOW6432Node\Autodesk\AutoCAD'
)
foreach ($root in $regRoots) {
    if (-not (Test-Path $root)) { continue }
    Get-ChildItem $root -ErrorAction SilentlyContinue | ForEach-Object {
        Get-ChildItem $_.PSPath -ErrorAction SilentlyContinue | ForEach-Object {
            try {
                $loc = (Get-ItemProperty -Path $_.PSPath -Name 'AcadLocation' -ErrorAction Stop).AcadLocation
                if ($loc) {
                    $dir = $loc.TrimEnd('\') + '\'
                    if (Test-Path (Join-Path $dir 'acdbmgd.dll')) { $candidates.Add($dir) }
                }
            }
            catch { }
        }
    }
}

# 3. Program Files 常见目录
$pf = @(
    ${env:ProgramFiles},
    ${env:ProgramFiles(x86)},
    'D:\Program Files',
    'E:\Program Files'
) | Where-Object { $_ -and (Test-Path $_) }

foreach ($base in $pf) {
    $autodesk = Join-Path $base 'Autodesk'
    if (-not (Test-Path $autodesk)) { continue }
    Get-ChildItem $autodesk -Directory -Filter 'AutoCAD*' -ErrorAction SilentlyContinue | ForEach-Object {
        $dir = $_.FullName.TrimEnd('\') + '\'
        if (Test-Path (Join-Path $dir 'acdbmgd.dll')) { $candidates.Add($dir) }
    }
}

$unique = $candidates | Select-Object -Unique

if ($unique.Count -eq 0) {
    Write-Error @"
未找到 acdbmgd.dll。请确认已安装 AutoCAD 2024/2025/2026，然后任选：
  1) `$env:AcadInstallDir = '你的路径\AutoCAD 2025\'; dotnet build`
  2) 手动创建 plugin\Directory.Build.props.user（见 Directory.Build.props.user.example）
"@
    exit 1
}

$chosen = $unique[0]
Write-Info "找到 AutoCAD 互操作 DLL 目录:"
$unique | ForEach-Object { Write-Info "  $_" }
Write-Info "将使用: $($chosen)"

if ($WriteProps) {
    $propsPath = Join-Path $PSScriptRoot '..\Directory.Build.props.user'
    $content = @"
<Project>
  <PropertyGroup>
    <AcadInstallDir>{0}</AcadInstallDir>
  </PropertyGroup>
</Project>
"@ -f $chosen
    [System.IO.File]::WriteAllText($propsPath, $content, [System.Text.UTF8Encoding]::new($false))
    Write-Info "已写入 $propsPath"
}

# 供脚本调用方读取（避免路径中的 \A 等在双引号中被转义）
Write-Output ([string]$chosen)
