# Per-tenant storage quota API tests
$ErrorActionPreference = "Stop"
$base = "http://localhost:5237"
$tenantHdr = @{ "X-Tenant-Slug" = "demo" }
$results = @()

function Record($name, $pass, $detail) {
    $script:results += [pscustomobject]@{ Test = $name; Pass = $pass; Detail = $detail }
    $icon = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "[$icon] $name - $detail"
}

function Login-Platform($email, $password) {
    $body = @{ email = $email; password = $password } | ConvertTo-Json
    return Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body
}

function Login-Tenant($email, $password) {
    $body = @{ email = $email; password = $password } | ConvertTo-Json
    return Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body -Headers $tenantHdr
}

function Try-Login-Tenant($email, [string[]]$passwords) {
    foreach ($pwd in $passwords) {
        try { return Login-Tenant $email $pwd } catch { }
    }
    throw "Could not login $email"
}

function AuthHeaders($token, $slug = "demo") {
    return @{ Authorization = "Bearer $($token.accessToken)"; "X-Tenant-Slug" = $slug }
}

function Post-FileUploadStatus($headers, $folder, $filePath) {
    $auth = $headers.Authorization
    $slug = $headers["X-Tenant-Slug"]
    $mime = if ($filePath -match '\.pdf$') { "application/pdf" } else { "image/png" }
    $code = & curl.exe -s -o NUL -w "%{http_code}" `
        -X POST "$base/api/v1/admin/files?folder=$folder" `
        -H "Authorization: $auth" `
        -H "X-Tenant-Slug: $slug" `
        -F "file=@$filePath;type=$mime"
    return [int]$code
}

Write-Host "`n=== Storage quota tests ===`n"

try {
    $sa = Login-Platform "superadmin@platform.com" "SuperAdmin123!"
    $saH = @{ Authorization = "Bearer $($sa.accessToken)" }
    $admin = Try-Login-Tenant "admin@demo.com" @("Dell123#", "Admin123!")
    $adminH = AuthHeaders $admin

    $tenants = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants" -Headers $saH
    $demo = $tenants | Where-Object { $_.slug -eq "demo" } | Select-Object -First 1
    if (-not $demo) { throw "demo tenant not found" }

    # Ensure clean quota state from prior runs
    Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$($demo.id)/storage" -Method PUT -ContentType "application/json" -Headers $saH -Body (@{
        quotaBytesOverride = $null
        quotaBypass = $false
    } | ConvertTo-Json) | Out-Null

    $usage = Invoke-RestMethod -Uri "$base/api/v1/admin/storage" -Headers $adminH
    Record "Admin storage usage" ($null -ne $usage.quotaBytes) "used=$($usage.usedBytes) quota=$($usage.quotaBytes)"

    $allStorage = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/storage" -Headers $saH
    $demoStorage = $allStorage | Where-Object { $_.tenantId -eq $demo.id } | Select-Object -First 1
    Record "SuperAdmin list storage" ($null -ne $demoStorage) "plan=$($demoStorage.plan)"

    $listRow = $tenants | Where-Object { $_.id -eq $demo.id } | Select-Object -First 1
    Record "Tenant list includes storage fields" ($listRow.storageQuotaBytes -gt 0) "quota=$($listRow.storageQuotaBytes)"

    # Tight quota for block test (1 KB)
    $saved = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$($demo.id)/storage" -Method PUT -ContentType "application/json" -Headers $saH -Body (@{
        quotaBytesOverride = 1024
        quotaBypass = $false
    } | ConvertTo-Json)

    $tmp = Join-Path $env:TEMP ("quota-test-{0}.pdf" -f [guid]::NewGuid())
    [System.IO.File]::WriteAllBytes($tmp, (New-Object byte[] 2048))

    $blockCode = Post-FileUploadStatus $adminH "notes" $tmp
    Record "Upload blocked over quota (413)" ($blockCode -eq 413) "status=$blockCode"

    $bypass = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$($demo.id)/storage" -Method PUT -ContentType "application/json" -Headers $saH -Body (@{
        quotaBytesOverride = 1024
        quotaBypass = $true
    } | ConvertTo-Json)
    Record "Enable quota bypass" ($bypass.quotaBypassEnabled -eq $true) "bypass on"

    # Restore defaults
    Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$($demo.id)/storage" -Method PUT -ContentType "application/json" -Headers $saH -Body (@{
        quotaBytesOverride = $null
        quotaBypass = $false
    } | ConvertTo-Json) | Out-Null
    Record "Restore plan default quota" $true "override cleared"

    Remove-Item $tmp -Force -ErrorAction SilentlyContinue
} catch {
    Record "Storage quota suite" $false $_.Exception.Message
}

$passed = ($results | Where-Object { $_.Pass }).Count
$total = $results.Count
Write-Host "`n$passed / $total passed`n"
$results | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $PSScriptRoot "test-storage-quota-results.json")
if ($passed -lt $total) { exit 1 }
