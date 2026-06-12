$ErrorActionPreference = "Stop"
$base = "http://localhost:5237"
$login = Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST -ContentType "application/json" `
    -Headers @{ "X-Tenant-Slug" = "demo" } `
    -Body '{"email":"admin@demo.com","password":"Admin123!"}'
$h = @{
    Authorization  = "Bearer $($login.accessToken)"
    "X-Tenant-Slug" = "demo"
}
$subs = Invoke-RestMethod -Uri "$base/api/v1/admin/me/subjects" -Headers $h
$sid = $subs[0].subjectId
$exams = Invoke-RestMethod -Uri "$base/api/v1/admin/subjects/$sid/mock-exams" -Headers $h
Write-Host "Exams: $($exams.Count)"
if ($exams.Count -eq 0) { exit 0 }

$exam = $exams[0]
$sectionInputs = @()
if ($exam.sections.Count -gt 0) {
    foreach ($s in $exam.sections) {
        $topics = @()
        foreach ($t in $s.topics) {
            $topics += @{ topicId = $t.topicId; questionCount = $t.questionCount }
        }
        $sectionInputs += @{
            title = $s.title
            sortOrder = $s.sortOrder
            sectionTimeLimitMinutes = $s.sectionTimeLimitMinutes
            topics = $topics
        }
    }
} else {
    $topics = @()
    foreach ($t in $exam.topics) {
        $topics += @{ topicId = $t.topicId; questionCount = $t.questionCount }
    }
    $sectionInputs = @(@{ title = "General"; sortOrder = 1; topics = $topics })
}

$body = @{
    title = $exam.title
    description = $exam.description
    timeLimitMinutes = $exam.timeLimitMinutes
    marksPerCorrect = $exam.marksPerCorrect
    penaltyPerWrong = $exam.penaltyPerWrong
    availableFromUtc = $exam.availableFromUtc
    availableUntilUtc = $exam.availableUntilUtc
    isPublished = $true
    resultVisibility = $exam.resultVisibility
    showExplanations = $exam.showExplanations
    notifyTeachersOnBatchComplete = $exam.notifyTeachersOnBatchComplete
    batchCompleteThresholdPercent = $exam.batchCompleteThresholdPercent
    sections = $sectionInputs
} | ConvertTo-Json -Depth 8

try {
    $r = Invoke-RestMethod -Uri "$base/api/v1/admin/mock-exams/$($exam.id)" -Method PUT -Headers $h -ContentType "application/json" -Body $body
    Write-Host "UPDATE OK isPublished=$($r.isPublished)"
} catch {
    Write-Host "UPDATE FAILED: $($_.ErrorDetails.Message)"
}

# Test create with isPublished true
$units = Invoke-RestMethod -Uri "$base/api/v1/subjects/$sid/units" -Headers $h
$allTopics = @()
foreach ($u in $units) {
    $allTopics += Invoke-RestMethod -Uri "$base/api/v1/units/$($u.id)/topics" -Headers $h
}
$withMcq = $allTopics | Where-Object { $_.mcqCount -gt 0 } | Select-Object -First 1
if ($null -eq $withMcq) {
    Write-Host "No topics with MCQs for create test"
    exit 0
}

$createBody = @{
    subjectId = $sid
    title = "Publish test $(Get-Date -Format 'HHmmss')"
    description = $null
    timeLimitMinutes = 30
    marksPerCorrect = 1
    penaltyPerWrong = 0
    availableFromUtc = $null
    availableUntilUtc = $null
    isPublished = $true
    resultVisibility = "AfterClose"
    showExplanations = $true
    notifyTeachersOnBatchComplete = $false
    batchCompleteThresholdPercent = 80
    sections = @(@{
        title = "Section 1"
        sortOrder = 1
        topics = @(@{ topicId = $withMcq.id; questionCount = 0 })
    })
} | ConvertTo-Json -Depth 8

try {
    $c = Invoke-RestMethod -Uri "$base/api/v1/admin/mock-exams" -Method POST -Headers $h -ContentType "application/json" -Body $createBody
    Write-Host "CREATE OK isPublished=$($c.isPublished)"
    Invoke-RestMethod -Uri "$base/api/v1/admin/mock-exams/$($c.id)" -Method DELETE -Headers $h | Out-Null
} catch {
    Write-Host "CREATE FAILED: $($_.ErrorDetails.Message)"
}
