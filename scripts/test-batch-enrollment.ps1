# Smoke tests: per-batch enrollment cap + enrollment window (KIPS Phase 1)
$ErrorActionPreference = "Stop"
$base = "http://localhost:5237"
$tenantHdr = @{ "X-Tenant-Slug" = "demo" }
$results = @()

function Record($name, $pass, $detail) {
    $script:results += [pscustomobject]@{ Test = $name; Pass = $pass; Detail = $detail }
    $icon = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "[$icon] $name - $detail"
}

function Login($email, $password) {
    $body = @{ email = $email; password = $password } | ConvertTo-Json
    return Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body -Headers $tenantHdr
}

function AuthHeaders($token) {
    $t = if ($token.accessToken) { $token.accessToken } else { $token.AccessToken }
    return @{ Authorization = "Bearer $t"; "X-Tenant-Slug" = "demo" }
}

function Wait-Api($maxSec = 120) {
    $deadline = (Get-Date).AddSeconds($maxSec)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-WebRequest -Uri "$base/swagger/index.html" -UseBasicParsing -TimeoutSec 3 | Out-Null
            return $true
        } catch {
            Start-Sleep -Seconds 3
        }
    }
    return $false
}

Write-Host "`n=== Batch enrollment cap + window ===`n"

if (-not (Wait-Api)) {
    Record "API reachable" $false "Start Lms.Api on port 5237"
    exit 1
}
Record "API reachable" $true $base

$admin = $null
foreach ($pwd in @("Admin123!", "Dell123#")) {
    try { $admin = Login "admin@demo.com" $pwd; break } catch { }
}
if (-not $admin) { Record "Admin login" $false "admin@demo.com"; exit 1 }
Record "Admin login" $true "admin@demo.com"
$adminH = AuthHeaders $admin

try {
    $raw = Invoke-RestMethod -Uri "$base/api/v1/bundles" -Headers $tenantHdr
    $bundles = if ($raw -is [System.Array]) { @($raw) } else { @($raw) }
    $first = $bundles[0]
    $hasFields = $bundles.Count -gt 0 -and ($null -ne $first.enrollmentStatus) -and ($null -ne $first.activeEnrollments)
    Record "GET /bundles enrollment fields" $hasFields "count=$($bundles.Count) status=$($first.enrollmentStatus) active=$($first.activeEnrollments)"
} catch {
    Record "GET /bundles enrollment fields" $false $_.Exception.Message
    exit 1
}

$target = $null
foreach ($b in $bundles) {
    if ($b.price -eq 0) { $target = $b; break }
}
if (-not $target) { $target = $bundles[0] }

$original = @{
    price = [double]$target.price
    maxEnrollments = $target.maxEnrollments
    enrollmentOpensAt = $target.enrollmentOpensAt
    enrollmentClosesAt = $target.enrollmentClosesAt
    startsAt = $target.startsAt
    endsAt = $target.endsAt
}

try {
    $future = (Get-Date).ToUniversalTime().AddDays(30).ToString("o")
    $body = @{
        price = $original.price
        maxEnrollments = 1
        enrollmentOpensAt = $future
    } | ConvertTo-Json
    $updated = Invoke-RestMethod -Uri "$base/api/v1/admin/bundles/$($target.id)" -Method PUT `
        -ContentType "application/json" -Headers $adminH -Body $body
    $notYet = $updated.enrollmentStatus -eq "NotYetOpen"
    Record "PUT batch window -> NotYetOpen" $notYet "status=$($updated.enrollmentStatus) cap=$($updated.maxEnrollments)"
} catch {
    Record "PUT batch window -> NotYetOpen" $false $_.Exception.Message
}

try {
    $restore = @{
        price = $original.price
        maxEnrollments = $original.maxEnrollments
        enrollmentOpensAt = $original.enrollmentOpensAt
        enrollmentClosesAt = $original.enrollmentClosesAt
        startsAt = $original.startsAt
        endsAt = $original.endsAt
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$base/api/v1/admin/bundles/$($target.id)" -Method PUT `
        -ContentType "application/json" -Headers $adminH -Body $restore | Out-Null
    Record "Restore batch settings" $true $target.title
} catch {
    Record "Restore batch settings" $false $_.Exception.Message
}

$fail = @($results | Where-Object { -not $_.Pass }).Count
$out = Join-Path $PSScriptRoot "test-batch-enrollment-results.json"
$results | ConvertTo-Json | Set-Content $out
Write-Host "`nResults saved to $out"
exit $(if ($fail -gt 0) { 1 } else { 0 })
