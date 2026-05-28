. "$PSScriptRoot\cad_api.psm1"
Invoke-CadApi -Method GET -Path "/zoom/extents" | ConvertTo-Json -Depth 8
