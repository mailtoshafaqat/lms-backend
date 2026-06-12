# Issue and verify a completion certificate for student1@demo.com
$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5237"
$Tenant = "demo"
$results = @()

function Record($name, $pass, $detail) {
    $script:results += [pscustomobject]@{ Test = $name; Pass = $pass; Detail = $detail }
    $icon = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "[$icon] $name - $detail"
}

function Login($email, $password) {
    $body = @{ email = $email; password = $password } | ConvertTo-Json
    $h = @{ "X-Tenant-Slug" = $Tenant }
    return Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body -Headers $h
}

function Try-Login($email, [string[]]$passwords) {
    foreach ($pwd in $passwords) {
        try { return Login $email $pwd } catch { }
    }
    throw "Could not login $email"
}

function Headers($token) {
    return @{ Authorization = "Bearer $($token.accessToken)"; "X-Tenant-Slug" = $Tenant }
}

function Get-BundleTopics($bundleId, $headers) {
    $detail = Invoke-RestMethod -Uri "$BaseUrl/api/v1/bundles/$bundleId" -Headers $headers
    $topics = @()
    foreach ($sub in $detail.subjects) {
        $units = Invoke-RestMethod -Uri "$BaseUrl/api/v1/subjects/$($sub.id)/units" -Headers $headers
        foreach ($u in $units) {
            $rows = Invoke-RestMethod -Uri "$BaseUrl/api/v1/units/$($u.id)/topics" -Headers $headers
            foreach ($t in $rows) { $topics += $t }
        }
    }
    return $topics
}

function Complete-Topic($topicId, $headers) {
    try {
        $quiz = Invoke-RestMethod -Uri "$BaseUrl/api/v1/topics/$topicId/quiz" -Headers $headers
        if ($quiz.questions.Count -gt 0) {
            $answers = @()
            foreach ($q in $quiz.questions) {
                $answers += @{ questionId = $q.id; selectedKey = ($q.options | Select-Object -First 1).key }
            }
            $body = @{ answers = $answers } | ConvertTo-Json -Depth 6
            Invoke-RestMethod -Uri "$BaseUrl/api/v1/quizzes/$($quiz.id)/attempts" -Method POST `
                -Headers $headers -ContentType "application/json" -Body $body | Out-Null
            return "quiz"
        }
    } catch { }

    try {
        $content = Invoke-RestMethod -Uri "$BaseUrl/api/v1/topics/$topicId/content" -Headers $headers
        if ($content.lectures.Count -gt 0) {
            foreach ($lec in $content.lectures) {
                $body = @{ progressPercent = 100; positionSec = 999; topicId = $topicId } | ConvertTo-Json
                Invoke-RestMethod -Uri "$BaseUrl/api/v1/me/lectures/$($lec.id)/progress" -Method PUT `
                    -Headers $headers -ContentType "application/json" -Body $body | Out-Null
            }
            return "video"
        }
    } catch { }

    return $null
}

Write-Host "`n=== Certificate test for student1 ===`n"

try {
    $admin = Try-Login "admin@demo.com" @("Dell123#", "Admin123!")
    $adminH = Headers $admin

    $studentEmail = "student1@demo.com"
    $student = $null
    foreach ($cred in @(
        @{ email = "student1@demo.com"; passwords = @("Dell123#", "Student123!", "Admin123!") },
        @{ email = "e2e.student1@demo.com"; passwords = @("E2eStudent1!") }
    )) {
        try {
            $student = Try-Login $cred.email $cred.passwords
            $studentEmail = $cred.email
            break
        } catch { }
    }
    if (-not $student) { throw "Could not login student1" }
    $studentH = Headers $student
    Record "Student login" $true $studentEmail

    $tpl = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/certificate-template" -Headers $adminH
    if (-not $tpl.enabled) {
        $tpl = Invoke-RestMethod -Uri "$BaseUrl/api/v1/admin/certificate-template" -Method PUT `
            -ContentType "application/json" -Headers $adminH -Body (@{
            title = $tpl.title; subtitle = $tpl.subtitle
            backgroundUrl = $tpl.backgroundUrl; logoUrl = $tpl.logoUrl; signatureUrl = $tpl.signatureUrl
            signatureLabel = $tpl.signatureLabel; primaryColor = $tpl.primaryColor
            showQrCode = $true; enabled = $true
        } | ConvertTo-Json)
    }
    Record "Certificate template enabled" $tpl.enabled "version=$($tpl.version)"

    $dash = Invoke-RestMethod -Uri "$BaseUrl/api/v1/me/dashboard" -Headers $studentH
    if ($dash.bundleProgress.Count -eq 0) { throw "Student has no bundle progress" }

    # Prefer bundle closest to completion, else first enrolled
    $target = $dash.bundleProgress | Sort-Object -Property percentComplete -Descending | Select-Object -First 1
    $bundleId = $target.bundleId
    $bundleTitle = $target.bundleTitle
    Record "Target bundle" $true "$bundleTitle ($($target.percentComplete)% before)"

    $topics = @(Get-BundleTopics $bundleId $studentH)
    $completed = 0
    if ($topics.Count -gt 0) {
        foreach ($t in $topics) {
            $mode = Complete-Topic $t.id $studentH
            if ($mode) { $completed++ }
        }
        Record "Topics completed" ($completed -eq $topics.Count) "$completed / $($topics.Count)"
    } else {
        Record "Topics completed" $true "bundle already complete (catalog empty)"
    }

    Start-Sleep -Seconds 1
    $dash2 = Invoke-RestMethod -Uri "$BaseUrl/api/v1/me/dashboard" -Headers $studentH
    $prog = $dash2.bundleProgress | Where-Object { $_.bundleId -eq $bundleId } | Select-Object -First 1
    Record "Bundle 100% complete" ($prog.percentComplete -eq 100) "$($prog.topicsCompleted)/$($prog.topicsTotal)"

    $certs = Invoke-RestMethod -Uri "$BaseUrl/api/v1/me/certificates" -Headers $studentH
    $cert = $certs | Where-Object { $_.bundleId -eq $bundleId } | Select-Object -First 1
    if (-not $cert) { throw "Certificate not issued for $bundleTitle" }
    Record "Certificate issued" $true $cert.certificateNumber

    $pdf = Invoke-WebRequest -Uri "$BaseUrl/api/v1/me/certificates/$($cert.id)/pdf" -Headers $studentH -UseBasicParsing
    $pdfOk = $pdf.StatusCode -eq 200 -and $pdf.Headers["Content-Type"] -match "pdf" -and $pdf.RawContentLength -gt 500
    Record "Student PDF download" $pdfOk "bytes=$($pdf.RawContentLength)"

    $verify = Invoke-RestMethod -Uri "$BaseUrl/api/v1/public/certificates/verify/$($cert.certificateNumber)?tenant=$Tenant"
    Record "Public verify" ($verify.valid -eq $true) "$($verify.studentName) - $($verify.courseName)"

    $adminUri = "$BaseUrl/api/v1/admin/certificates?bundleId=$bundleId" + "&page=1&pageSize=20"
    $adminList = Invoke-RestMethod -Uri $adminUri -Headers $adminH
    $adminRow = $adminList.data | Where-Object { $_.certificateNumber -eq $cert.certificateNumber } | Select-Object -First 1
    Record "Admin sees certificate" ($null -ne $adminRow) $(if ($adminRow) { $adminRow.studentName } else { "missing" })

} catch {
    Record "Certificate flow" $false $_.Exception.Message
}

$passed = ($results | Where-Object { $_.Pass }).Count
$total = $results.Count
Write-Host "`n$passed / $total passed`n"
$results | Format-Table -AutoSize
if ($passed -lt $total) { exit 1 }
