param(
    [Parameter(Mandatory = $true)][string]$Path
)

. "$PSScriptRoot\cad_api.psm1"
$fullPath = (Resolve-Path -LiteralPath $Path).Path
Invoke-CadApi -Method POST -Path "/document/open" -Body @{ path = $fullPath } | ConvertTo-Json -Depth 5
