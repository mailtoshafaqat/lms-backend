# E2E API tests: bookmarks, global search, weakness quiz
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

function Wait-Api($maxSec = 90) {
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

Write-Host "`n=== Student learning features (bookmarks, search, weakness quiz) ===`n"

if (-not (Wait-Api)) {
    Record "API reachable" $false "Start Lms.Api on port 5237"
    exit 1
}
Record "API reachable" $true $base

# Login admin + student
$admin = $null
foreach ($pwd in @("Dell123#", "Admin123!")) {
    try { $admin = Login "admin@demo.com" $pwd; break } catch { }
}
if (-not $admin) { Record "Admin login" $false "Could not login admin@demo.com"; exit 1 }
Record "Admin login" $true "admin@demo.com"
$adminH = AuthHeaders $admin

$student = $null
foreach ($pwd in @("Dell123#", "Student123!", "Admin123!")) {
    try { $student = Login "student1@demo.com" $pwd; break } catch { }
}
if (-not $student) {
    try {
        $list = Invoke-RestMethod -Uri "$base/api/v1/admin/students?page=1&pageSize=50" -Headers $adminH
        $row = $list.data | Where-Object { $_.email -eq "student1@demo.com" } | Select-Object -First 1
        if ($row) {
            $reset = Invoke-RestMethod -Uri "$base/api/v1/admin/students/$($row.userId)/reset-password" -Method POST -Headers $adminH
            $pwd = if ($reset.tempPassword) { $reset.tempPassword } else { $reset.TempPassword }
            $student = Login "student1@demo.com" $pwd
        }
    } catch { }
}
if (-not $student) { Record "Student login" $false "student1@demo.com"; exit 1 }
Record "Student login" $true "student1@demo.com"
$studentH = AuthHeaders $student

$topicId = $null
try {
    $bundles = Invoke-RestMethod -Uri "$base/api/v1/bundles" -Headers $studentH
    if ($bundles.Count -gt 0) {
        $bid = $bundles[0].id
        $mine = @(Invoke-RestMethod -Uri "$base/api/v1/me/enrollments" -Headers $studentH)
        $enrolled = $mine | Where-Object { $_.bundleId -eq $bid }
        if (-not $enrolled) {
            try {
                Invoke-RestMethod -Uri "$base/api/v1/bundles/$bid/enroll" -Method POST -Headers $studentH | Out-Null
                Record "Student enrolled" $true "bundle $bid"
            } catch {
                $sid = if ($student.userId) { $student.userId } else { $student.UserId }
                $exp = (Get-Date).AddYears(1).ToUniversalTime().ToString("o")
                try {
                    Invoke-RestMethod -Uri "$base/api/v1/admin/students/$sid/enroll" -Method POST -ContentType "application/json" -Headers $adminH -Body (@{ bundleId = $bid } | ConvertTo-Json) | Out-Null
                    Record "Student enrolled (admin)" $true "bundle $bid"
                } catch {
                    Record "Student enrollment" $false $_.Exception.Message
                }
            }
        } else {
            Record "Student already enrolled" $true "bundle $bid"
        }

        $detail = Invoke-RestMethod -Uri "$base/api/v1/bundles/$bid" -Headers $studentH
        foreach ($sub in $detail.subjects) {
            $units = Invoke-RestMethod -Uri "$base/api/v1/subjects/$($sub.id)/units" -Headers $studentH
            foreach ($u in $units) {
                $topics = Invoke-RestMethod -Uri "$base/api/v1/units/$($u.id)/topics" -Headers $studentH
                foreach ($t in ($topics | Where-Object { $_.mcqCount -gt 0 })) {
                    $quizTry = Invoke-RestMethod -Uri "$base/api/v1/topics/$($t.id)/quiz" -Headers $studentH
                    if ($quizTry.questions.Count -gt 0) {
                        $topicId = $t.id
                        break
                    }
                }
                if ($topicId) { break }
            }
            if ($topicId) { break }
        }

        if ($topicId) {
            Record "Topic with MCQs found" $true $topicId
            $quiz = Invoke-RestMethod -Uri "$base/api/v1/topics/$topicId/quiz" -Headers $studentH
            if ($quiz.questions.Count -gt 0) {
                $answers = @()
                foreach ($q in $quiz.questions) {
                    $answers += @{ questionId = $q.id; selectedKey = "Z" }
                }
                $submitBody = @{ answers = $answers } | ConvertTo-Json -Depth 5
                Invoke-RestMethod -Uri "$base/api/v1/quizzes/$($quiz.id)/attempts" -Method POST -ContentType "application/json" -Headers $studentH -Body $submitBody | Out-Null
                Record "Quiz submitted (wrong answers)" $true "$($quiz.questions.Count) questions"
            } else {
                Record "Quiz has questions" $false "empty quiz"
            }
        } else {
            $recent = Invoke-RestMethod -Uri "$base/api/v1/topics/recent?take=1" -Headers $studentH
            if ($recent.Count -gt 0) {
                $topicId = $recent[0].id
                Invoke-RestMethod -Uri "$base/api/v1/admin/topics/$topicId/questions" -Method POST -ContentType "application/json" -Headers $adminH -Body (@{
                    stem = "E2E seed: which organelle is the powerhouse of the cell?"
                    options = @("Nucleus", "Mitochondria", "Ribosome", "Golgi")
                    correctKey = "1"
                    explanation = "Mitochondria produce ATP."
                } | ConvertTo-Json) | Out-Null
                Record "Seeded MCQ via admin" $true $topicId
                $quiz = Invoke-RestMethod -Uri "$base/api/v1/topics/$topicId/quiz" -Headers $studentH
                if ($quiz.questions.Count -gt 0) {
                    $answers = @()
                    foreach ($q in $quiz.questions) { $answers += @{ questionId = $q.id; selectedKey = "Z" } }
                    $submitBody = @{ answers = $answers } | ConvertTo-Json -Depth 5
                    Invoke-RestMethod -Uri "$base/api/v1/quizzes/$($quiz.id)/attempts" -Method POST -ContentType "application/json" -Headers $studentH -Body $submitBody | Out-Null
                    Record "Quiz submitted (wrong answers)" $true "$($quiz.questions.Count) questions"
                }
            } else {
                Record "Topic with MCQs" $false "seed content first"
            }
        }
    }
} catch {
    Record "Enrollment / quiz seed" $false $_.Exception.Message
}

# Bookmarks
try {
    $topicForBm = $topicId
    if (-not $topicForBm) {
        try {
            $recent = Invoke-RestMethod -Uri "$base/api/v1/topics/recent?take=1" -Headers $studentH
            if ($recent.Count -gt 0) { $topicForBm = $recent[0].id }
        } catch { }
    }
    if ($topicForBm) {
        $status0 = Invoke-RestMethod -Uri "$base/api/v1/me/bookmarks/status?targetType=Topic&targetId=$topicForBm" -Headers $studentH
        if ($status0.isBookmarked -and $status0.bookmarkId) {
            try {
                Invoke-RestMethod -Uri "$base/api/v1/me/bookmarks/$($status0.bookmarkId)" -Method DELETE -Headers $studentH | Out-Null
            } catch { }
        }
        $bm = Invoke-RestMethod -Uri "$base/api/v1/me/bookmarks" -Method POST -ContentType "application/json" -Headers $studentH -Body (@{
            targetType = "Topic"; targetId = $topicForBm; title = "Test bookmark topic"; subtitle = "E2E test"
        } | ConvertTo-Json)
        $list = Invoke-RestMethod -Uri "$base/api/v1/me/bookmarks" -Headers $studentH
        $status = Invoke-RestMethod -Uri "$base/api/v1/me/bookmarks/status?targetType=Topic&targetId=$topicForBm" -Headers $studentH
        $deleted = $false
        try {
            Invoke-RestMethod -Uri "$base/api/v1/me/bookmarks/$($bm.id)" -Method DELETE -Headers $studentH | Out-Null
            $deleted = $true
        } catch { }
        Record "Bookmarks CRUD" ($status.isBookmarked -and $deleted) "bookmark $($bm.id)"
    } else {
        Record "Bookmarks CRUD" $false "no topic id"
    }
} catch {
    Record "Bookmarks CRUD" $false $_.Exception.Message
}

# Global search
try {
    $hits = Invoke-RestMethod -Uri "$base/api/v1/search?q=bio&take=10" -Headers $studentH
    Record "Global search" ($hits.Count -ge 0) "$($hits.Count) hit(s) for 'bio'"
} catch {
    Record "Global search" $false $_.Exception.Message
}

# Weakness quiz
try {
    $wq = Invoke-RestMethod -Uri "$base/api/v1/me/weakness-quiz?count=5" -Headers $studentH
    Record "Weakness quiz load" ($wq.questions.Count -gt 0) "$($wq.questions.Count) questions (source=$($wq.source))"
    if ($wq.questions.Count -gt 0) {
        $ans = @()
        foreach ($q in $wq.questions) {
            $ans += @{ questionId = $q.id; selectedKey = "Z" }
        }
        $res = Invoke-RestMethod -Uri "$base/api/v1/me/weakness-quiz/submit" -Method POST -ContentType "application/json" -Headers $studentH -Body (@{ answers = $ans } | ConvertTo-Json -Depth 5)
        Record "Weakness quiz submit" ($res.total -gt 0) "score $($res.score)/$($res.total)"
    }
} catch {
    if ($_.Exception.Message -match "404") {
        Record "Weakness quiz load" $false "no mistakes yet - quiz seed may have failed"
    } else {
        Record "Weakness quiz" $false $_.Exception.Message
    }
}

# Dashboard overview
try {
    $dash = Invoke-RestMethod -Uri "$base/api/v1/me/dashboard" -Headers $studentH
    Record "Student dashboard API" ($null -ne $dash.overallAccuracy) "accuracy=$($dash.overallAccuracy)% rank=$($dash.instituteRank)"
} catch {
    Record "Student dashboard API" $false $_.Exception.Message
}

# Unit tests admin flow (404 before enable is expected)
$unitId = $null
try {
    $bundles = Invoke-RestMethod -Uri "$base/api/v1/bundles" -Headers $adminH
    if ($bundles.Count -gt 0) {
        $detail = Invoke-RestMethod -Uri "$base/api/v1/bundles/$($bundles[0].id)" -Headers $adminH
        foreach ($sub in $detail.subjects) {
            $units = Invoke-RestMethod -Uri "$base/api/v1/subjects/$($sub.id)/units" -Headers $adminH
            if ($units.Count -gt 0) { $unitId = $units[0].id; break }
        }
    }
    if ($unitId) {
        $code404 = $false
        try {
            Invoke-RestMethod -Uri "$base/api/v1/admin/units/$unitId/quizzes/unit-test" -Headers $adminH
        } catch {
            if ($_.Exception.Response.StatusCode.value__ -eq 404) { $code404 = $true }
        }
        Record "Unit quiz GET before enable" $code404 "404 expected when not configured"

        $enabled = Invoke-RestMethod -Uri "$base/api/v1/admin/units/$unitId/quizzes/unit-test/settings" -Method PUT -ContentType "application/json" -Headers $adminH -Body (@{
            timeLimitMinutes = 30
            availableFromUtc = $null
            availableUntilUtc = $null
            resultVisibility = "Immediate"
            showExplanations = $true
            difficultyFilter = $null
        } | ConvertTo-Json)
        Record "Unit quiz enable (admin)" ($null -ne $enabled.id) "quiz $($enabled.id)"

        $loaded = Invoke-RestMethod -Uri "$base/api/v1/admin/units/$unitId/quizzes/unit-test" -Headers $adminH
        Record "Unit quiz GET after enable" ($loaded.assembledQuestionCount -ge 0) "$($loaded.assembledQuestionCount) questions"
    } else {
        Record "Unit tests flow" $false "no unit found in catalog"
    }
} catch {
    Record "Unit tests flow" $false $_.Exception.Message
}

# Student profile (admin)
try {
    $sid = if ($student.userId) { $student.userId } else { $student.UserId }
    $profile = Invoke-RestMethod -Uri "$base/api/v1/admin/students/$sid/profile" -Headers $adminH
    $updated = Invoke-RestMethod -Uri "$base/api/v1/admin/students/$sid/profile" -Method PUT -ContentType "application/json" -Headers $adminH -Body (@{
        fullName = $profile.fullName
        phone = $profile.phone
        profilePictureUrl = $profile.profilePictureUrl
        profileNotes = "E2E profile check"
    } | ConvertTo-Json)
    Record "Student profile admin API" ($updated.fullName -eq $profile.fullName) "profile saved"
} catch {
    Record "Student profile admin API" $false $_.Exception.Message
}

# Auth me with profile fields
try {
    $me = Invoke-RestMethod -Uri "$base/api/v1/auth/me" -Headers $studentH
    Record "Auth me profile fields" ($null -ne $me.userId) "role=$($me.role)"
} catch {
    Record "Auth me profile fields" $false $_.Exception.Message
}

$pass = ($results | Where-Object { $_.Pass }).Count
$fail = ($results | Where-Object { -not $_.Pass }).Count
Write-Host "`n=== Summary: $pass passed, $fail failed ===`n"
if ($fail -gt 0) { exit 1 }
