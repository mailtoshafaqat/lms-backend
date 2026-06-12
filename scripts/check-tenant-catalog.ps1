# Report subject catalog + batch links for a tenant slug (default: vuprep)
param(
    [string]$Slug = "vuprep",
    [string]$Base = "http://localhost:5237",
    [string]$AdminEmail = "",
    [string]$AdminPassword = "Dell123#"
)

$tenantHdr = @{ "X-Tenant-Slug" = $Slug }

if (-not $AdminEmail) {
    $AdminEmail = switch ($Slug) {
        "demo" { "admin@demo.com" }
        default { "$Slug@test.com" }
    }
}

function Login($email, $password) {
    $body = @{ email = $email; password = $password } | ConvertTo-Json
    return Invoke-RestMethod -Uri "$Base/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body -Headers $tenantHdr
}

Write-Host "`n=== Catalog check: $Slug ===`n"

$login = $null
foreach ($pwd in @($AdminPassword, "Admin123!")) {
    try {
        $login = Login $AdminEmail $pwd
        break
    } catch { }
}
if (-not $login) {
    Write-Host "Could not login as $AdminEmail on tenant $Slug"
    exit 1
}

$token = if ($login.accessToken) { $login.accessToken } else { $login.AccessToken }
$h = @{ Authorization = "Bearer $token"; "X-Tenant-Slug" = $Slug }

$defs = Invoke-RestMethod -Uri "$Base/api/v1/admin/subject-definitions" -Headers $h
Write-Host "Catalog subjects ($($defs.Count)):"
foreach ($d in ($defs | Sort-Object sortOrder)) {
    Write-Host ("  - {0} ({1}) linked={2} library={3}" -f $d.displayName, $d.code, $d.linkedBatchCount, $d.libraryUnitCount)
}

$bundles = Invoke-RestMethod -Uri "$Base/api/v1/bundles" -Headers $h
$unlinked = @()
foreach ($b in $bundles) {
    $detail = Invoke-RestMethod -Uri "$Base/api/v1/bundles/$($b.id)" -Headers $h
    foreach ($s in $detail.subjects) {
        $flag = if ($s.linkedToCatalog) { "catalog" } else { "FREE TEXT" }
        Write-Host ("  [{0}] {1} / {2} -> {3}" -f $flag, $b.title, $s.title, $(if ($s.subjectDefinitionId) { "linked" } else { "unlinked" }))
        if (-not $s.linkedToCatalog) { $unlinked += "$($b.title) / $($s.title)" }
    }
}

if ($unlinked.Count -eq 0) {
    Write-Host "`nAll batch subjects are linked to the catalog (or no subjects yet)."
} else {
    Write-Host "`nUnlinked subjects (fix in Content admin - pick from catalog):"
    $unlinked | ForEach-Object { Write-Host "  - $_" }
}

Write-Host ""
