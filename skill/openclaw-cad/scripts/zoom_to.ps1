param(
    [Parameter(Mandatory = $true)][string]$Handle
)

. "$PSScriptRoot\cad_api.psm1"
Invoke-CadApi -Method GET -Path "/zoom/to?handle=$([uri]::EscapeDataString($Handle))" | ConvertTo-Json -Depth 8
