param(
    [Parameter(Mandatory = $true)][double]$Factor,
    [double]$CenterX,
    [double]$CenterY
)

. "$PSScriptRoot\cad_api.psm1"
$path = "/zoom/by?factor=$Factor"
if ($PSBoundParameters.ContainsKey('CenterX')) { $path += "&centerx=$CenterX" }
if ($PSBoundParameters.ContainsKey('CenterY')) { $path += "&centery=$CenterY" }
Invoke-CadApi -Method POST -Path $path | ConvertTo-Json -Depth 8
