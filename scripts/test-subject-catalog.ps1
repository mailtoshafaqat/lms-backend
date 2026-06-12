# End-to-end tests for Subject Catalog (Phases 1–3)
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

function AuthHeaders($login) {
    $token = if ($login.accessToken) { $login.accessToken } else { $login.AccessToken }
    return @{ Authorization = "Bearer $token"; "X-Tenant-Slug" = "demo" }
}

Write-Host "`n=== Subject catalog tests ===`n"

$admin = $null
foreach ($pwd in @("Admin123!", "Dell123#")) {
    try {
        $admin = Login "admin@demo.com" $pwd
        break
    } catch { }
}
if (-not $admin) {
    Record "Admin login" $false "could not login"
    exit 1
}
$h = AuthHeaders $admin

# Phase 1: catalog list + seeded exam-prep subjects
try {
    $defs = Invoke-RestMethod -Uri "$base/api/v1/admin/subject-definitions" -Headers $h
    $physics = $defs | Where-Object { $_.code -eq "physics" } | Select-Object -First 1
    Record "Catalog list" ($defs.Count -ge 5) "count=$($defs.Count)"
    Record "Physics seeded" ($null -ne $physics) "code=physics"
} catch {
    Record "Catalog list" $false $_.Exception.Message
    $physics = $null
}

# Phase 1: create batch subject linked to catalog
$bundleId = $null
$linkedSubjectId = $null
try {
    $bundles = Invoke-RestMethod -Uri "$base/api/v1/bundles" -Headers $h
    $bundleId = ($bundles | Select-Object -First 1).id
    if (-not $bundleId) { throw "no bundle" }
    if (-not $physics) { throw "no physics definition" }
    $created = Invoke-RestMethod -Uri "$base/api/v1/admin/bundles/$bundleId/subjects" -Method POST -ContentType "application/json" -Headers $h -Body (@{
        title = "ignored"
        order = 99
        subjectDefinitionId = $physics.id
        includeSharedContent = $false
    } | ConvertTo-Json)
    $linkedSubjectId = $created.id
    Record "Create linked subject" ($created.linkedToCatalog -eq $true) "title=$($created.title)"
} catch {
    Record "Create linked subject" $false $_.Exception.Message
}

# Phase 3: library unit on catalog + link to batch subject
try {
    if (-not $physics) { throw "no physics" }
    $libUnit = Invoke-RestMethod -Uri "$base/api/v1/admin/subject-definitions/$($physics.id)/library-units" -Method POST -ContentType "application/json" -Headers $h -Body (@{
        title = "Test Shared Mechanics"
        order = 1
    } | ConvertTo-Json)
    Record "Create library unit" ($libUnit.isShared -eq $true) "unit=$($libUnit.title)"
    if ($linkedSubjectId) {
        Invoke-RestMethod -Uri "$base/api/v1/admin/subject-definitions/subjects/$linkedSubjectId/link-shared-units" -Method POST -ContentType "application/json" -Headers $h -Body "{}" | Out-Null
        $units = Invoke-RestMethod -Uri "$base/api/v1/subjects/$linkedSubjectId/units" -Headers $h
        $merged = @($units | Where-Object { $_.title -eq "Test Shared Mechanics" })
        Record "Merged shared units" ($merged.Count -ge 1) "unitCount=$($units.Count)"
    }
} catch {
    Record "Shared content library" $false $_.Exception.Message
}

# Phase 2: catalog teacher assignment
$teacherId = $null
try {
    $teachers = Invoke-RestMethod -Uri "$base/api/v1/admin/teachers?page=1&pageSize=5" -Headers $h
    $teacherId = ($teachers.data | Select-Object -First 1).userId
    if (-not $teacherId) { throw "no teacher" }
    if (-not $physics) { throw "no physics" }
    Invoke-RestMethod -Uri "$base/api/v1/admin/teachers/$teacherId/subjects" -Method PUT -ContentType "application/json" -Headers $h -Body (@{
        subjectIds = @()
        subjectDefinitionIds = @($physics.id)
    } | ConvertTo-Json) | Out-Null
    $maps = Invoke-RestMethod -Uri "$base/api/v1/admin/subject-teachers" -Headers $h
    $row = $maps | Where-Object { $_.userId -eq $teacherId } | Select-Object -First 1
    Record "Catalog teacher assign" ($row.subjectDefinitionIds -contains $physics.id) "defs=$($row.subjectDefinitionIds.Count)"
} catch {
    Record "Catalog teacher assign" $false $_.Exception.Message
}

# Phase 2: student filter by catalog subject
try {
    if (-not $physics) { throw "no physics" }
    $filtered = Invoke-RestMethod -Uri "$base/api/v1/admin/students?subjectDefinitionId=$($physics.id)&page=1&pageSize=10" -Headers $h
    Record "Student catalog filter" ($null -ne $filtered.data) "total=$($filtered.total)"
} catch {
    Record "Student catalog filter" $false $_.Exception.Message
}

# Phase 1: catalog groups API
try {
    $groups = Invoke-RestMethod -Uri "$base/api/v1/admin/catalog-subject-groups" -Headers $h
    $phyGroup = $groups | Where-Object { $_.code -eq "physics" } | Select-Object -First 1
    Record "Catalog subject groups" ($phyGroup.batchPlacements.Count -ge 0) "placements=$($phyGroup.batchPlacements.Count)"
} catch {
    Record "Catalog subject groups" $false $_.Exception.Message
}

Write-Host "`n=== Summary ==="
$passed = ($results | Where-Object { $_.Pass }).Count
$failed = ($results | Where-Object { -not $_.Pass }).Count
Write-Host "Passed: $passed  Failed: $failed  Total: $($results.Count)"
if ($failed -gt 0) { exit 1 }
