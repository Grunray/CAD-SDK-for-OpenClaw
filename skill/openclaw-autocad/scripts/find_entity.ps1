param(
    [Parameter(Mandatory = $true)][string]$Query,
    [switch]$Exact,
    [string]$Layer
)

. "$PSScriptRoot\AutoCadApi.psm1"

$path = "/find?q=$([uri]::EscapeDataString($Query))"
if ($Exact) { $path += "&exact=true" }
if ($Layer) { $path += "&layer=$([uri]::EscapeDataString($Layer))" }

Invoke-AutoCadApi -Method GET -Path $path | ConvertTo-Json -Depth 8
