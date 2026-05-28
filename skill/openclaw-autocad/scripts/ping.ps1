param(
    [string]$BaseUrl = "http://127.0.0.1:54321"
)

. "$PSScriptRoot\AutoCadApi.psm1"
Invoke-AutoCadApi -Method GET -Path "/ping" | ConvertTo-Json -Depth 5
