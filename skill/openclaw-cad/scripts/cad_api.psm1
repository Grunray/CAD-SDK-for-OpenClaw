$BaseUrl = "http://127.0.0.1:54321"

function Get-CadRuntimeBaseUrl {
    $candidates = @(
        (Join-Path $env:USERPROFILE ".openclaw\kb\shared\wiki\cad-runtime.md")
        (Join-Path $env:USERPROFILE ".openclaw\kb\shared\wiki\autocad-runtime.md")
    )
    foreach ($runtimeFile in $candidates) {
        if (-not (Test-Path $runtimeFile)) { continue }
        $content = Get-Content $runtimeFile -Raw
        if ($content -match 'baseUrl:\s*(https?://[^\s]+)') {
            return $Matches[1]
        }
    }
    return $null
}

$resolved = Get-CadRuntimeBaseUrl
if ($resolved) { $BaseUrl = $resolved }

function Invoke-CadApi {
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
            $json = $Body | ConvertTo-Json -Depth 5 -Compress
            return Invoke-RestMethod -Uri $uri -Method $Method -Body $json -ContentType "application/json"
        }

        return Invoke-RestMethod -Uri $uri -Method $Method
    }
    catch {
        Write-Error "CAD plugin request failed. Run: powershell -File scripts/ensure_cad_ready.ps1 Details: $($_.Exception.Message)"
        throw
    }
}

Export-ModuleMember -Function Invoke-CadApi -Variable BaseUrl
