# AutoCAD 插件本地 HTTP API 默认端口
$BaseUrl = "http://127.0.0.1:54321"

# 若存在 runtime 文件则优先读取
$RuntimeFile = Join-Path $env:USERPROFILE ".openclaw\kb\shared\wiki\autocad-runtime.md"
if (Test-Path $RuntimeFile) {
    $content = Get-Content $RuntimeFile -Raw
    if ($content -match 'baseUrl:\s*(https?://[^\s]+)') {
        $BaseUrl = $Matches[1]
    }
}

function Invoke-AutoCadApi {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [object]$Body = $null
    )

    $uri = "$BaseUrl$Path"
  try {
        if ($Method -eq "GET") {
            return Invoke-RestMethod -Uri $uri -Method Get
        }

        if ($Body -ne $null) {
            $json = $Body | ConvertTo-Json -Depth 5
            return Invoke-RestMethod -Uri $uri -Method $Method -Body $json -ContentType "application/json"
        }

        return Invoke-RestMethod -Uri $uri -Method $Method
    }
    catch {
        Write-Error "AutoCAD plugin request failed. Run: powershell -File scripts/ensure_autocad_ready.ps1 Details: $($_.Exception.Message)"
        throw
    }
}

Export-ModuleMember -Function Invoke-AutoCadApi -Variable BaseUrl
