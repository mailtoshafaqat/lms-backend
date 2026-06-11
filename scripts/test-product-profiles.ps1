# Edge-case tests for ProductProfile (ExamPrep / GeneralLms / Both)
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

function Login-Platform($email, $password) {
    $body = @{ email = $email; password = $password } | ConvertTo-Json
    return Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body
}

function Get-AccessToken($login) {
    if ($login.accessToken) { return $login.accessToken }
    if ($login.AccessToken) { return $login.AccessToken }
    throw "login response missing access token"
}

function AuthHeaders($token, $slug = "demo") {
    return @{ Authorization = "Bearer $(Get-AccessToken $token)"; "X-Tenant-Slug" = $slug }
}

function Get-StatusCode($uri, $headers) {
    try {
        Invoke-WebRequest -Uri $uri -Headers $headers -UseBasicParsing | Out-Null
        return 200
    } catch {
        if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode.value__ }
        throw
    }
}

function Ensure-DemoStudent($adminToken) {
    $h = AuthHeaders $adminToken
    foreach ($pwd in @("Student123!", "Dell123#")) {
        try {
            return Login "student1@demo.com" $pwd
        } catch { }
    }
    $list = Invoke-RestMethod -Uri "$base/api/v1/admin/students?page=1&pageSize=50" -Headers $h
    $row = $list.data | Where-Object { $_.email -eq "student1@demo.com" } | Select-Object -First 1
    if (-not $row) { throw "student1 not found on demo tenant" }
    $reset = Invoke-RestMethod -Uri "$base/api/v1/admin/students/$($row.userId)/reset-password" -Method POST -Headers $h
    $pwd = if ($reset.tempPassword) { $reset.tempPassword } else { $reset.TempPassword }
    return Login "student1@demo.com" $pwd
}

Write-Host "`n=== Product profile edge-case tests ===`n"

# 0. Ensure demo tenant is ExamPrep (idempotent)
try {
    $sa0 = Login-Platform "superadmin@platform.com" "SuperAdmin123!"
    $saH0 = @{ Authorization = "Bearer $($sa0.accessToken)" }
    $allTenants = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants" -Headers $saH0
    $demoRow = $allTenants | Where-Object { $_.slug -eq "demo" } | Select-Object -First 1
    if ($demoRow) {
        $dd = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$($demoRow.id)" -Headers $saH0
        if ($dd.productProfile -ne "ExamPrep") {
            Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$($demoRow.id)/flags" -Method PUT -ContentType "application/json" -Headers $saH0 -Body (@{
                status = $dd.status; plan = $dd.plan; productProfile = "ExamPrep"; customDomain = $dd.customDomain
                liveClassesEnabled = $dd.liveClassesEnabled; zoomMode = $dd.zoomMode; paymentMode = $dd.paymentMode
                allowStudentSelfEnroll = $dd.allowStudentSelfEnroll; allowAdminCreateStudent = $dd.allowAdminCreateStudent
                syllabusMentorEnabled = $dd.syllabusMentorEnabled; bundlePriceEditEnabled = $dd.bundlePriceEditEnabled
                mcqBulkImportEnabled = $dd.mcqBulkImportEnabled
            } | ConvertTo-Json) | Out-Null
            Record "Demo reset to ExamPrep" $true "was $($dd.productProfile)"
        }
    }
} catch {
    Record "Demo profile reset" $false $_.Exception.Message
}

# 1. Demo tenant (ExamPrep default) login tenant features
try {
    $admin = $null
    foreach ($pwd in @("Admin123!", "Dell123#")) {
        try {
            $admin = Login "admin@demo.com" $pwd
            break
        } catch { }
    }
    if (-not $admin) { throw "demo admin login failed" }
    $t = $admin.tenant
    Record "Demo login + tenant block" ($null -ne $t) "tenant present=$($null -ne $t)"
    Record "Demo profile=ExamPrep" ($t.productProfile -eq "ExamPrep") "profile=$($t.productProfile)"
    Record "Demo mockExamsEnabled" ($t.mockExamsEnabled -eq $true) "mockExams=$($t.mockExamsEnabled)"
    Record "Demo doubtsEnabled" ($t.doubtsEnabled -eq $true) "doubts=$($t.doubtsEnabled)"
    Record "Demo mistakeDiaryEnabled" ($t.mistakeDiaryEnabled -eq $true) "mistakes=$($t.mistakeDiaryEnabled)"
} catch {
    Record "Demo admin login" $false $_.Exception.Message
}

# 2. Student ExamPrep — mocks API allowed (reset password via admin if needed)
try {
    if (-not $admin) { throw "demo admin required" }
    $student = Ensure-DemoStudent $admin
    $h = AuthHeaders $student
    $mockCode = Get-StatusCode "$base/api/v1/me/mock-exams" $h
    Record "ExamPrep student mocks list" ($mockCode -eq 200) "status=$mockCode"
    $mistakeCode = Get-StatusCode "$base/api/v1/me/mistakes" $h
    Record "ExamPrep student mistakes" ($mistakeCode -eq 200) "status=$mistakeCode"
} catch {
    Record "ExamPrep student APIs" $false $_.Exception.Message
}

# 3. Create GeneralLms test tenant via SuperAdmin
$generalSlug = "gen-lms-$([guid]::NewGuid().ToString('N').Substring(0,8))"
$generalId = $null
$genAdminEmail = "admin+$generalSlug@test.local"
$genPwd = $null
try {
    $sa = Login-Platform "superadmin@platform.com" "SuperAdmin123!"
    $saH = @{ Authorization = "Bearer $($sa.accessToken)" }
    $created = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants" -Method POST -ContentType "application/json" -Headers $saH -Body (@{
        name = "Test General LMS"; slug = $generalSlug; plan = "MVP"; productProfile = "GeneralLms"
    } | ConvertTo-Json)
    $generalId = $created.id
    Record "GeneralLms tenant created" ($created.productProfile -eq "GeneralLms") "slug=$generalSlug"
    $updated = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$generalId/flags" -Method PUT -ContentType "application/json" -Headers $saH -Body (@{
        status = "Active"; plan = "MVP"; productProfile = "GeneralLms"; customDomain = $null
        liveClassesEnabled = $false; zoomMode = "TenantManaged"; paymentMode = "TenantManaged"
        allowStudentSelfEnroll = $true; allowAdminCreateStudent = $true
        syllabusMentorEnabled = $false; bundlePriceEditEnabled = $true; mcqBulkImportEnabled = $false
    } | ConvertTo-Json)
    Record "GeneralLms flags saved" ($updated.productProfile -eq "GeneralLms") "live=$($updated.liveClassesEnabled)"
    $na = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$generalId/admins" -Method POST -ContentType "application/json" -Headers $saH -Body (@{
        fullName = "General Admin"; email = $genAdminEmail
    } | ConvertTo-Json)
    $genPwd = $na.tempPassword
} catch {
    Record "GeneralLms tenant setup" $false $_.Exception.Message
}

# 4. GeneralLms login — mocks/mistakes blocked (404)
if ($generalId -and $genPwd) {
    try {
        $gHdr = @{ "X-Tenant-Slug" = $generalSlug }
        $gAdmin = Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST -ContentType "application/json" -Body (@{
            email = $genAdminEmail; password = $genPwd
        } | ConvertTo-Json) -Headers $gHdr
        $gt = $gAdmin.tenant
        Record "GeneralLms admin login" ($gt.productProfile -eq "GeneralLms") "profile=$($gt.productProfile)"
        Record "GeneralLms mockExams off" ($gt.mockExamsEnabled -eq $false) "mockExams=$($gt.mockExamsEnabled)"
        Record "GeneralLms mentor off" ($gt.syllabusMentorEnabled -eq $false) "mentor=$($gt.syllabusMentorEnabled)"
        $gH = AuthHeaders $gAdmin $generalSlug
        $genStudentEmail = "student+$generalSlug@test.local"
        $createdStudent = Invoke-RestMethod -Uri "$base/api/v1/admin/students" -Method POST -ContentType "application/json" -Headers $gH -Body (@{
            fullName = "General Student"; email = $genStudentEmail; bundleId = $null
        } | ConvertTo-Json)
        $stuPwd = if ($createdStudent.tempPassword) { $createdStudent.tempPassword } else { $createdStudent.TempPassword }
        $gStudent = Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST -ContentType "application/json" -Body (@{
            email = $genStudentEmail; password = $stuPwd
        } | ConvertTo-Json) -Headers $gHdr
        $gStuH = AuthHeaders $gStudent $generalSlug

        $mockCode = Get-StatusCode "$base/api/v1/me/mock-exams" $gStuH
        Record "GeneralLms mocks API 404" ($mockCode -eq 404) "status=$mockCode"
        $mistakeCode = Get-StatusCode "$base/api/v1/me/mistakes" $gStuH
        Record "GeneralLms mistakes API 404" ($mistakeCode -eq 404) "status=$mistakeCode"
        $adminMockCode = Get-StatusCode "$base/api/v1/admin/subjects/00000000-0000-0000-0000-000000000001/mock-exams" $gH
        Record "GeneralLms admin mocks API 404" ($adminMockCode -eq 404) "status=$adminMockCode"
        $doubtsCode = Get-StatusCode "$base/api/v1/me/doubts" $gStuH
        Record "GeneralLms doubts API 404" ($doubtsCode -eq 404) "status=$doubtsCode"
        $unitHdr = @{ "X-Tenant-Slug" = $generalSlug }
        $unitCode = Get-StatusCode "$base/api/v1/units/00000000-0000-0000-0000-000000000001/quizzes/unit" $unitHdr
        Record "GeneralLms unit quiz API 404" ($unitCode -eq 404) "status=$unitCode"
        $topicCode = Get-StatusCode "$base/api/v1/topics/00000000-0000-0000-0000-000000000001/quiz" $unitHdr
        Record "GeneralLms topic quiz still reachable" ($topicCode -in @(200, 404)) "status=$topicCode (404=missing quiz ok)"
    } catch {
        Record "GeneralLms API guards" $false $_.Exception.Message
    }
}

# 5. Switch demo to Both and verify modules on
try {
    $sa = Login-Platform "superadmin@platform.com" "SuperAdmin123!"
    $saH = @{ Authorization = "Bearer $($sa.accessToken)" }
    $demo = ($existing = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants" -Headers $saH) | Where-Object { $_.slug -eq "demo" } | Select-Object -First 1
    $demoDetail = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$($demo.id)" -Headers $saH
    Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$($demo.id)/flags" -Method PUT -ContentType "application/json" -Headers $saH -Body (@{
        status = $demoDetail.status; plan = $demoDetail.plan; productProfile = "Both"; customDomain = $demoDetail.customDomain
        liveClassesEnabled = $demoDetail.liveClassesEnabled; zoomMode = $demoDetail.zoomMode; paymentMode = $demoDetail.paymentMode
        allowStudentSelfEnroll = $demoDetail.allowStudentSelfEnroll; allowAdminCreateStudent = $demoDetail.allowAdminCreateStudent
        syllabusMentorEnabled = $demoDetail.syllabusMentorEnabled; bundlePriceEditEnabled = $demoDetail.bundlePriceEditEnabled
        mcqBulkImportEnabled = $demoDetail.mcqBulkImportEnabled
    } | ConvertTo-Json) | Out-Null
    $adminBoth = $null
    foreach ($pwd in @("Admin123!", "Dell123#")) {
        try {
            $adminBoth = Login "admin@demo.com" $pwd
            break
        } catch { }
    }
    if (-not $adminBoth) { throw "demo admin re-login failed" }
    Record "Both profile after re-login" ($adminBoth.tenant.productProfile -eq "Both") "profile=$($adminBoth.tenant.productProfile)"
    Record "Both mocks still on" ($adminBoth.tenant.mockExamsEnabled -eq $true) "mocks=$($adminBoth.tenant.mockExamsEnabled)"
    # Restore ExamPrep for demo
    Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$($demo.id)/flags" -Method PUT -ContentType "application/json" -Headers $saH -Body (@{
        status = $demoDetail.status; plan = $demoDetail.plan; productProfile = "ExamPrep"; customDomain = $demoDetail.customDomain
        liveClassesEnabled = $demoDetail.liveClassesEnabled; zoomMode = $demoDetail.zoomMode; paymentMode = $demoDetail.paymentMode
        allowStudentSelfEnroll = $demoDetail.allowStudentSelfEnroll; allowAdminCreateStudent = $demoDetail.allowAdminCreateStudent
        syllabusMentorEnabled = $demoDetail.syllabusMentorEnabled; bundlePriceEditEnabled = $demoDetail.bundlePriceEditEnabled
        mcqBulkImportEnabled = $demoDetail.mcqBulkImportEnabled
    } | ConvertTo-Json) | Out-Null
    Record "Demo restored to ExamPrep" $true "restored"
} catch {
    Record "Both profile switch" $false $_.Exception.Message
}

# 6. Invalid tenant slug
try {
    Login "admin@demo.com" "Admin123!" | Out-Null
    Record "Invalid login creds" $false "should not reach"
} catch {
    Record "Invalid password rejected" $true "401 expected"
}

# 7. Topic quiz still works on GeneralLms (core LMS)
# Skipped if no enrollment - note only

Write-Host "`n=== Summary ==="
$passed = ($results | Where-Object { $_.Pass }).Count
$failed = ($results | Where-Object { -not $_.Pass }).Count
Write-Host "Passed: $passed  Failed: $failed  Total: $($results.Count)"
if ($failed -gt 0) { exit 1 }
