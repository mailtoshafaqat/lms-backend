# Roadmap features: video progress, certificates, MCQ search, cohort analytics
$BaseUrl = "http://localhost:5237"
$passed = 0
$failed = 0
$results = @()

function Test-Step($name, $scriptBlock) {
    try {
        & $scriptBlock
        $script:passed++
        $script:results += [pscustomobject]@{ Name = $name; Status = "Pass" }
        Write-Host "PASS $name" -ForegroundColor Green
    } catch {
        $script:failed++
        $msg = $_.Exception.Message
        $script:results += [pscustomobject]@{ Name = $name; Status = "Fail"; Error = $msg }
        Write-Host "FAIL $name - $msg" -ForegroundColor Red
    }
}

function Login($email, $password, $tenantSlug = "demo") {
    $headers = @{ "X-Tenant-Slug" = $tenantSlug }
    $body = @{ email = $email; password = $password } | ConvertTo-Json
    $r = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body -Headers $headers
    return @{ Authorization = "Bearer $($r.accessToken)"; Session = $r }
}

function Try-Login($email, [string[]]$passwords) {
    foreach ($pwd in $passwords) {
        try { return Login $email $pwd } catch { }
    }
    throw "Could not login $email"
}

$admin = Try-Login "admin@demo.com" @("Dell123#", "Admin123!")
$student = $null
foreach ($cred in @(
    @{ email = "student1@demo.com"; passwords = @("Student123!", "Dell123#", "Admin123!") },
    @{ email = "e2e.student1@demo.com"; passwords = @("E2eStudent1!") }
)) {
    try { $student = Try-Login $cred.email $cred.passwords; break } catch { }
}
if (-not $student) { throw "No student login available" }

$ah = $admin.Authorization
$sh = $student.Authorization
$demoHeaders = @{ Authorization = $sh; "X-Tenant-Slug" = "demo" }
$adminHeaders = @{ Authorization = $ah; "X-Tenant-Slug" = "demo" }

Test-Step "Video library loads" {
    $lib = Invoke-RestMethod -Uri "$BaseUrl/api/v1/me/video-library" -Headers $demoHeaders
    if ($null -eq $lib.items) { throw "No items property" }
}

$script:lectureId = $null
$script:topicId = $null
Test-Step "Save lecture progress" {
    $lib = Invoke-RestMethod -Uri "$BaseUrl/api/v1/me/video-library" -Headers $demoHeaders
    if ($lib.items.Count -eq 0) { throw "No lectures to test" }
    $script:lectureId = $lib.items[0].lectureId
    $script:topicId = $lib.items[0].topicId
    $body = @{ progressPercent = 42; positionSec = 120; topicId = $script:topicId } | ConvertTo-Json
    $p = Invoke-RestMethod -Uri "$BaseUrl/api/v1/me/lectures/$($script:lectureId)/progress" -Method PUT -Headers $demoHeaders -ContentType "application/json" -Body $body
    if ($p.progressPercent -lt 42) { throw "Progress not saved" }
}

Test-Step "Get lecture progress" {
    if (-not $script:lectureId) { throw "No lecture" }
    $p = Invoke-RestMethod -Uri "$BaseUrl/api/v1/me/lectures/$($script:lectureId)/progress" -Headers $demoHeaders
    if ($p.progressPercent -lt 42) { throw "Expected >= 42%" }
}

Test-Step "Bulk lecture progress" {
    if (-not $script:lectureId) { throw "No lecture" }
    $rows = Invoke-RestMethod -Uri "$BaseUrl/api/v1/me/lectures/progress?lectureIds=$($script:lectureId)" -Headers $demoHeaders
    if ($rows.Count -lt 1) { throw "No bulk rows" }
}

Test-Step "Student certificates list" {
    $certs = Invoke-RestMethod -Uri "$BaseUrl/api/v1/me/certificates" -Headers $demoHeaders
    if ($null -eq $certs) { throw "No response" }
}

Test-Step "Admin certificates list" {
    $r = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/certificates?page=1&pageSize=10" -Headers $adminHeaders
    if ($null -eq $r.data) { throw "Missing data" }
}

Test-Step "MCQ search (admin)" {
    $uri = '{0}/api/v1/admin/questions/search?q=which&page=1&pageSize=5' -f $BaseUrl
    $r = Invoke-RestMethod -Uri $uri -Headers $adminHeaders
    if ($null -eq $r.data) { throw "Missing data" }
}

Test-Step "MCQ search short query empty" {
    $uri = '{0}/api/v1/admin/questions/search?q=a&page=1' -f $BaseUrl
    $r = Invoke-RestMethod -Uri $uri -Headers $adminHeaders
    if ($r.total -ne 0) { throw "Expected empty for short query" }
}

Test-Step "Cohort analytics overview" {
    $bundles = Invoke-RestMethod -Uri "$BaseUrl/api/v1/bundles" -Headers $adminHeaders
    if ($bundles.Count -eq 0) { throw "No bundles" }
    $bid = $bundles[0].id
    $ov = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/analytics/cohort?bundleId=$bid" -Headers $adminHeaders
    if ($null -eq $ov.bundleTitle) { throw "No overview" }
}

Test-Step "Cohort students + CSV export" {
    $bundles = Invoke-RestMethod -Uri "$BaseUrl/api/v1/bundles" -Headers $adminHeaders
    $bid = $bundles[0].id
    $rows = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/analytics/cohort/students?bundleId=$bid" -Headers $adminHeaders
    if ($null -eq $rows) { throw "No student rows" }
    $csv = Invoke-WebRequest -Uri "$BaseUrl/api/v1/admin/analytics/cohort/export?bundleId=$bid" -Headers $adminHeaders -UseBasicParsing
    if ($csv.Content -notmatch "StudentName") { throw "Invalid CSV" }
}

Test-Step "Dashboard bundle progress" {
    $dash = Invoke-RestMethod -Uri "$BaseUrl/api/v1/me/dashboard" -Headers $demoHeaders
    if ($null -eq $dash.bundleProgress) { throw "No bundleProgress" }
}

Write-Host "`nRoadmap tests: $passed passed, $failed failed" -ForegroundColor Cyan
$results | Format-Table -AutoSize
$results | ConvertTo-Json | Out-File "$PSScriptRoot\test-roadmap-features-results.json"
if ($failed -gt 0) { exit 1 }
