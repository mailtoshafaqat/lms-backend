# Exports OpenAPI 3 spec from a running Lms.Api instance (Swagger).
# Usage: powershell -File backend/postman/export-openapi.ps1
#        powershell -File backend/postman/export-openapi.ps1 -BaseUrl http://localhost:5237

param(
    [string]$BaseUrl = "http://localhost:5237",
    [string]$OutDir = $PSScriptRoot
)

$ErrorActionPreference = "Stop"
$swaggerUrl = "$BaseUrl/swagger/v1/swagger.json"
$outFile = Join-Path $OutDir "openapi.json"

Write-Host "Fetching $swaggerUrl ..."
$spec = Invoke-RestMethod -Uri $swaggerUrl -Method GET

# Postman import works better when servers/base URL is present.
if (-not $spec.servers) {
    $spec | Add-Member -NotePropertyName servers -NotePropertyValue @(
        @{ url = $BaseUrl; description = "Local API" }
    ) -Force
}

$json = $spec | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText($outFile, $json)
Write-Host "Wrote $outFile ($([math]::Round((Get-Item $outFile).Length / 1KB, 1)) KB)"
