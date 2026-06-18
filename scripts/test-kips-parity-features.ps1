# E2E API tests: My Program, Stats, Notifications, Topic navigation
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

Write-Host "`n=== KIPS parity features (program, stats, nav, notifications) ===`n"

if (-not (Wait-Api)) {
    Record "API reachable" $false "Start Lms.Api on port 5237"
    exit 1
}
Record "API reachable" $true $base

$student = $null
foreach ($pwd in @("Dell123#", "Student123!", "Admin123!")) {
    try { $student = Login "student1@demo.com" $pwd; break } catch { }
}
if (-not $student) { Record "Student login" $false "student1@demo.com"; exit 1 }
Record "Student login" $true "student1@demo.com"
$h = AuthHeaders $student

# My Program
try {
    $program = Invoke-RestMethod -Uri "$base/api/v1/me/program" -Headers $h
    $bundleCount = @($program.bundles).Count
    $hasSubjects = $bundleCount -gt 0 -and @($program.bundles[0].subjects).Count -gt 0
    $hasContinue = $null -ne $program.continueTopic -and $program.continueTopic.topicId
    Record "GET /me/program" ($bundleCount -gt 0) "bundles=$bundleCount subjects=$hasSubjects continue=$hasContinue"
} catch {
    Record "GET /me/program" $false $_.Exception.Message
}

# Stats
try {
    $stats = Invoke-RestMethod -Uri "$base/api/v1/me/stats" -Headers $h
    $subjCount = @($stats.subjectCompletion).Count
    Record "GET /me/stats" ($null -ne $stats.overallAccuracy) "subjects=$subjCount accuracy=$($stats.overallAccuracy)%"
} catch {
    Record "GET /me/stats" $false $_.Exception.Message
}

# Notifications
try {
    $notifs = Invoke-RestMethod -Uri "$base/api/v1/me/notifications" -Headers $h
    $count = @($notifs.items).Count
    $unread = $notifs.unreadCount
    Record "GET /me/notifications" ($null -ne $notifs.items) "items=$count unread=$unread"
    if ($count -gt 0) {
        $id = $notifs.items[0].id
        $read = Invoke-RestMethod -Uri "$base/api/v1/me/notifications/$id/read" -Method POST -Headers $h
        Record "POST notification read" ($read.read -eq $true) "id=$id"
    } else {
        Record "POST notification read" $true "skipped (no notifications yet)"
    }
    $all = Invoke-RestMethod -Uri "$base/api/v1/me/notifications/read-all" -Method POST -Headers $h
    Record "POST notifications read-all" ($null -ne $all.marked) "marked=$($all.marked)"
} catch {
    Record "Notifications API" $false $_.Exception.Message
}

# Topic navigation
$topicId = $null
try {
    $bundles = Invoke-RestMethod -Uri "$base/api/v1/bundles" -Headers $h
    if ($bundles.Count -gt 0) {
        $detail = Invoke-RestMethod -Uri "$base/api/v1/bundles/$($bundles[0].id)" -Headers $h
        foreach ($sub in $detail.subjects) {
            $units = Invoke-RestMethod -Uri "$base/api/v1/subjects/$($sub.id)/units" -Headers $h
            foreach ($u in $units) {
                $topics = Invoke-RestMethod -Uri "$base/api/v1/units/$($u.id)/topics" -Headers $h
                if ($topics.Count -ge 2) {
                    $topicId = $topics[1].id
                    break
                } elseif ($topics.Count -eq 1) {
                    $topicId = $topics[0].id
                }
            }
            if ($topicId) { break }
        }
    }
    if ($topicId) {
        $nav = Invoke-RestMethod -Uri "$base/api/v1/topics/$topicId/navigation" -Headers $h
        $hasNav = ($null -ne $nav.previous) -or ($null -ne $nav.next)
        Record "GET topic navigation" $true "topic=$topicId prev=$($nav.previous.topicId) next=$($nav.next.topicId)"
    } else {
        Record "GET topic navigation" $false "no topics in catalog"
    }
} catch {
    Record "GET topic navigation" $false $_.Exception.Message
}

$pass = ($results | Where-Object { $_.Pass }).Count
$fail = ($results | Where-Object { -not $_.Pass }).Count
Write-Host "`n=== Summary: $pass passed, $fail failed ===`n"
if ($fail -gt 0) { exit 1 }
