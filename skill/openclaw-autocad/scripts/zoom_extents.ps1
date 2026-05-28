. "$PSScriptRoot\AutoCadApi.psm1"
Invoke-AutoCadApi -Method GET -Path "/zoom/extents" | ConvertTo-Json -Depth 5
