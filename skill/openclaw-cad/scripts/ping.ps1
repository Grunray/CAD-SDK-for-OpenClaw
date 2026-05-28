. "$PSScriptRoot\cad_api.psm1"
Invoke-CadApi -Method GET -Path "/ping" | ConvertTo-Json -Depth 5
