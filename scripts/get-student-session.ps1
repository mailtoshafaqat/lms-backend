$ErrorActionPreference = "Stop"
$base = "http://localhost:5237"
$h = @{ "X-Tenant-Slug" = "demo" }

function TryLogin($email, $passwords) {
    foreach ($pwd in $passwords) {
        try {
            return Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST -Headers $h `
                -ContentType "application/json" `
                -Body (@{ email = $email; password = $pwd } | ConvertTo-Json)
        } catch { }
    }
    throw "login failed $email"
}

$admin = TryLogin "admin@demo.com" @("Dell123#", "Admin123!")
$ah = @{ Authorization = "Bearer $($admin.accessToken)"; "X-Tenant-Slug" = "demo" }
$students = Invoke-RestMethod -Uri "$base/api/v1/admin/students?page=1&pageSize=50" -Headers $ah
$s1 = $students.data | Where-Object { $_.email -eq "student1@demo.com" } | Select-Object -First 1
$p = Invoke-RestMethod -Uri "$base/api/v1/admin/students/$($s1.userId)/profile" -Headers $ah
Invoke-RestMethod -Uri "$base/api/v1/admin/students/$($s1.userId)/profile" -Method PUT -Headers $ah `
    -ContentType "application/json" `
    -Body (@{
        fullName = $p.fullName
        phone = $p.phone
        country = "PK"
        profilePictureUrl = $p.profilePictureUrl
        profileNotes = $p.profileNotes
    } | ConvertTo-Json) | Out-Null

$login = TryLogin "student1@demo.com" @("Student123!", "Dell123#", "Admin123!")
$login | ConvertTo-Json -Compress -Depth 6
