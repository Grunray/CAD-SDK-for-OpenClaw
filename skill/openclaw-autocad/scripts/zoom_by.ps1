param(
    [Parameter(Mandatory = $true)][double]$Factor,
    [double]$CenterX,
    [double]$CenterY
)

. "$PSScriptRoot\AutoCadApi.psm1"

$path = "/zoom/by?factor=$Factor"
if ($PSBoundParameters.ContainsKey("CenterX") -and $PSBoundParameters.ContainsKey("CenterY")) {
    $path += "&centerX=$CenterX&centerY=$CenterY"
}

Invoke-AutoCadApi -Method POST -Path $path | ConvertTo-Json -Depth 5
