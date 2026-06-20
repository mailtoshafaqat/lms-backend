# Manual verification: batch cap/window with demo students (student1, student9, safa)
$ErrorActionPreference = "Stop"
$base = "http://localhost:5237"
$tenantHdr = @{ "X-Tenant-Slug" = "demo" }
$results = @()

function Record($name, $pass, $detail) {
    $script:results += [pscustomobject]@{ Test = $name; Pass = $pass; Detail = $detail }
    $icon = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "[$icon] $name - $detail"
}

function Login($email, $passwords) {
    foreach ($pwd in $passwords) {
        try {
            $body = @{ email = $email; password = $pwd } | ConvertTo-Json
            return Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body -Headers $tenantHdr
        } catch { }
    }
    return $null
}

function Get-BundlesJson($headers) {
    $raw = Invoke-WebRequest -Uri "$base/api/v1/bundles" -Headers $headers -UseBasicParsing
    return @($raw.Content | ConvertFrom-Json)
}

function AuthHeaders($token) {
    $t = if ($token.accessToken) { $token.accessToken } else { $token.AccessToken }
    return @{ Authorization = "Bearer $t"; "X-Tenant-Slug" = "demo" }
}

function Get-StudentEnrollments($adminH, $userId) {
    return @(Invoke-RestMethod -Uri "$base/api/v1/admin/students/$userId/enrollments" -Headers $adminH)
}

Write-Host "`n=== Batch enrollment — demo students ===`n"

$admin = Login "admin@demo.com" @("Admin123!", "Dell123#")
if (-not $admin) { Record "Admin login" $false "admin@demo.com"; exit 1 }
Record "Admin login" $true "admin@demo.com"
$adminH = AuthHeaders $admin

$students = Invoke-RestMethod -Uri "$base/api/v1/admin/students?page=1&pageSize=200" -Headers $adminH
$emails = $students.data | ForEach-Object { $_.email }
Record "List students" $true ("count=" + $students.data.Count + " emails=" + ($emails -join ", "))

$targets = @("student1@demo.com", "student9@demo.com", "safa@demo.com")
foreach ($pattern in @("student9", "safa")) {
    $found = $students.data | Where-Object { $_.email -like "*$pattern*" -or $_.fullName -like "*$pattern*" }
    if ($found) {
        foreach ($f in $found) { Record "Found $pattern" $true "$($f.fullName) <$($f.email)>" }
    } else {
        Record "Found $pattern" $false "Not in tenant student list"
    }
}

$studentPwds = @("Student123!", "Dell123#", "Admin123!")
foreach ($email in $targets) {
    $tok = Login $email $studentPwds
    if ($tok) {
        Record "Login $email" $true "OK"
        $sh = AuthHeaders $tok
        try {
            $bundles = Get-BundlesJson $sh
            $open = @($bundles | Where-Object { $_.enrollmentStatus -eq "Open" })
            Record "Bundles for $email" $true "total=$($bundles.Count) open=$($open.Count)"
        } catch {
            Record "Bundles for $email" $false $_.Exception.Message
        }
    } else {
        Record "Login $email" $false "Could not login (account may not exist)"
    }
}

# Pick ECAT Crash Course for cap/window test
$allBundles = Get-BundlesJson $tenantHdr
$testBundle = $allBundles | Where-Object { $_.title -eq "ECAT Crash Course" } | Select-Object -First 1
if (-not $testBundle) { $testBundle = $allBundles[0] }

$origPrice = [decimal]$testBundle.price
$orig = @{
    price = $origPrice
    maxEnrollments = $testBundle.maxEnrollments
    enrollmentOpensAt = $testBundle.enrollmentOpensAt
    enrollmentClosesAt = $testBundle.enrollmentClosesAt
    startsAt = $testBundle.startsAt
    endsAt = $testBundle.endsAt
}

# Set cap=15, future window -> student checkout should fail
$future = (Get-Date).ToUniversalTime().AddDays(14).ToString("o")
try {
    $updated = Invoke-RestMethod -Uri "$base/api/v1/admin/bundles/$($testBundle.id)" -Method PUT `
        -ContentType "application/json" -Headers $adminH -Body (@{
            price = $origPrice
            maxEnrollments = 15
            enrollmentOpensAt = $future
        } | ConvertTo-Json)
    Record "Set NotYetOpen on $($testBundle.title)" ($updated.enrollmentStatus -eq "NotYetOpen") "status=$($updated.enrollmentStatus)"
} catch {
    Record "Set NotYetOpen" $false $_.Exception.Message
}

$s1 = Login "student1@demo.com" $studentPwds
if ($s1) {
    $s1h = AuthHeaders $s1
    try {
        Invoke-RestMethod -Uri "$base/api/v1/payments/available-gateways?bundleId=$($testBundle.id)" -Headers $s1h | Out-Null
        Record "student1 checkout when NotYetOpen" $false "Expected failure but gateways returned"
    } catch {
        $msg = $_.ErrorDetails.Message
        if (-not $msg) { $msg = $_.Exception.Message }
        Record "student1 blocked when NotYetOpen" $true $msg.Substring(0, [Math]::Min(120, $msg.Length))
    }
}

# Admin provision enroll student1 (bypass window)
if ($s1) {
    $s1row = $students.data | Where-Object { $_.email -eq "student1@demo.com" } | Select-Object -First 1
    if ($s1row) {
        try {
            Invoke-RestMethod -Uri "$base/api/v1/admin/students/$($s1row.userId)/enroll" -Method POST `
                -ContentType "application/json" -Headers $adminH -Body (@{ bundleId = $testBundle.id } | ConvertTo-Json) | Out-Null
            Record "Admin provision student1 (bypass window)" $true "POST enroll OK"
        } catch {
            $msg = $_.ErrorDetails.Message
            if ($msg -match "already enrolled") {
                Record "Admin provision student1 (bypass window)" $true "Already enrolled (expected if duplicate)"
            } else {
                Record "Admin provision student1 (bypass window)" $false $msg
            }
        }
    }
}

# Enrollment counts for demo students
foreach ($email in $targets) {
    $row = $students.data | Where-Object { $_.email -eq $email } | Select-Object -First 1
    if ($row) {
        $enr = Get-StudentEnrollments $adminH $row.userId
        $titles = ($enr | ForEach-Object { $_.bundleTitle }) -join ", "
        if (-not $titles) { $titles = "(none)" }
        Record "Enrollments $email" $true "$($enr.Count) batch(es): $titles"
    }
}

# Restore
try {
    Invoke-RestMethod -Uri "$base/api/v1/admin/bundles/$($testBundle.id)" -Method PUT `
        -ContentType "application/json" -Headers $adminH -Body ($orig | ConvertTo-Json) | Out-Null
    Record "Restore $($testBundle.title) settings" $true "OK"
} catch {
    Record "Restore settings" $false $_.Exception.Message
}

$fail = @($results | Where-Object { -not $_.Pass }).Count
$out = Join-Path $PSScriptRoot "test-batch-students-demo-results.json"
$results | ConvertTo-Json | Set-Content $out
Write-Host "`nResults: $out ($fail failures)"
exit $(if ($fail -gt 0) { 1 } else { 0 })
