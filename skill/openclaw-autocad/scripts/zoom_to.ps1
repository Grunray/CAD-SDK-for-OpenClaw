param(
    [Parameter(Mandatory = $true)][string]$Handle
)

. "$PSScriptRoot\AutoCadApi.psm1"
Invoke-AutoCadApi -Method GET -Path "/zoom/to?handle=$([uri]::EscapeDataString($Handle))" | ConvertTo-Json -Depth 5
