$ErrorActionPreference = "Stop"
$base = "http://localhost:5237"
$tenantHdr = @{ "X-Tenant-Slug" = "demo" }

function Login($email, [string[]]$passwords) {
    foreach ($pwd in $passwords) {
        try {
            return Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST `
                -Headers $tenantHdr -ContentType "application/json" `
                -Body (@{ email = $email; password = $pwd } | ConvertTo-Json)
        } catch { }
    }
    throw "Could not login $email"
}

function AuthHdr($token) {
    return @{ Authorization = "Bearer $token"; "X-Tenant-Slug" = "demo" }
}

Write-Host "=== Admin: set student1 country to PK ==="
$admin = Login "admin@demo.com" @("Dell123#", "Admin123!")
$adminHdr = AuthHdr $admin.accessToken
$students = Invoke-RestMethod -Uri "$base/api/v1/admin/students?page=1&pageSize=50" -Headers $adminHdr
$s1 = $students.data | Where-Object { $_.email -eq "student1@demo.com" } | Select-Object -First 1
if (-not $s1) { throw "student1 not found" }

$profile = Invoke-RestMethod -Uri "$base/api/v1/admin/students/$($s1.userId)/profile" -Headers $adminHdr
Invoke-RestMethod -Uri "$base/api/v1/admin/students/$($s1.userId)/profile" -Method PUT -Headers $adminHdr `
    -ContentType "application/json" `
    -Body (@{
        fullName = $profile.fullName
        phone = $profile.phone
        country = "PK"
        profilePictureUrl = $profile.profilePictureUrl
        profileNotes = $profile.profileNotes
    } | ConvertTo-Json) | Out-Null

Write-Host "=== Student1 /me after country set ==="
$student = Login "student1@demo.com" @("Student123!", "Dell123#", "Admin123!")
$studentHdr = AuthHdr $student.accessToken
$me = Invoke-RestMethod -Uri "$base/api/v1/auth/me" -Headers $studentHdr
Write-Host "country=$($me.country) tenantDefault=$($me.tenantDefaultCountry)"

$bundles = Invoke-RestMethod -Uri "$base/api/v1/bundles" -Headers $studentHdr
$bundle = $bundles | Where-Object { $_.price -gt 0 } | Select-Object -First 1
if (-not $bundle) { throw "no paid bundle" }
Write-Host "checkout bundle=$($bundle.id) $($bundle.title)"

$gw = Invoke-RestMethod -Uri "$base/api/v1/payments/available-gateways?bundleId=$($bundle.id)&studentCountry=PK" -Headers $studentHdr
Write-Host "gateways (PK): $($gw.gateway -join ', ')"

Write-Host "=== Clear country (tenant default only) ==="
Invoke-RestMethod -Uri "$base/api/v1/admin/students/$($s1.userId)/profile" -Method PUT -Headers $adminHdr `
    -ContentType "application/json" `
    -Body (@{
        fullName = $profile.fullName
        phone = $profile.phone
        country = $null
        profilePictureUrl = $profile.profilePictureUrl
        profileNotes = $profile.profileNotes
    } | ConvertTo-Json) | Out-Null
$me2 = Invoke-RestMethod -Uri "$base/api/v1/auth/me" -Headers $studentHdr
Write-Host "country=$($me2.country) tenantDefault=$($me2.tenantDefaultCountry)"

Write-Host "PASS: API country profile flow OK"
Write-Host "BUNDLE_ID=$($bundle.id)"
