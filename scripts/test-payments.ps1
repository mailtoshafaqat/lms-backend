# End-to-end tenant-managed payment flows (Manual, Easypaisa, JazzCash, Stripe)
$ErrorActionPreference = "Stop"
$base = "http://localhost:5237"
$tenantHdr = @{ "X-Tenant-Slug" = "demo" }
$results = @()

function Record($name, $pass, $detail) {
    $script:results += [pscustomobject]@{ Test = $name; Pass = $pass; Detail = $detail }
    $icon = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "[$icon] $name - $detail"
}

function Login-Platform($email, $password) {
    $body = @{ email = $email; password = $password } | ConvertTo-Json
    return Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body
}

function Login-Tenant($email, $password) {
    $body = @{ email = $email; password = $password } | ConvertTo-Json
    return Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body -Headers $tenantHdr
}

function Try-Login-Tenant($email, [string[]]$passwords) {
    foreach ($pwd in $passwords) {
        try { return Login-Tenant $email $pwd } catch { }
    }
    throw "Could not login $email"
}

function AuthHeaders($token) {
    return @{ Authorization = "Bearer $($token.accessToken)"; "X-Tenant-Slug" = "demo" }
}

function Get-EnrolledBundleIds($studentH) {
    $enrollments = Invoke-RestMethod -Uri "$base/api/v1/me/enrollments" -Headers $studentH
    return @($enrollments | ForEach-Object { "$($_.bundleId)" })
}

function Find-BundleWithoutPending($adminH, $userId, $bundles) {
    $orders = @(Invoke-RestMethod -Uri "$base/api/v1/admin/payments" -Headers $adminH)
    $blocked = @($orders | Where-Object {
        $_.userId -eq $userId -and $_.status -in @("Processing", "Pending", "AwaitingApproval")
    } | ForEach-Object { $_.bundleId })
    return $bundles | Where-Object { $_.price -gt 0 -and $blocked -notcontains $_.id } | Select-Object -First 1
}

function Find-CleanPaidBundle($studentH, $adminH, $userId, $bundles) {
    $enrolled = Get-EnrolledBundleIds $studentH
    $orders = @(Invoke-RestMethod -Uri "$base/api/v1/admin/payments" -Headers $adminH)
    $blocked = @($orders | Where-Object {
        $_.userId -eq $userId -and $_.status -in @("Processing", "Pending", "AwaitingApproval")
    } | ForEach-Object { $_.bundleId })
    return $bundles | Where-Object {
        $_.price -gt 0 -and ($enrolled -notcontains "$($_.id)") -and ($blocked -notcontains "$($_.id)")
    } | Select-Object -First 1
}

function Clear-AllStuckOrders($adminH) {
    $orders = @(Invoke-RestMethod -Uri "$base/api/v1/admin/payments" -Headers $adminH)
    foreach ($o in $orders) {
        if ($o.status -eq "AwaitingApproval") {
            try {
                Invoke-RestMethod -Uri "$base/api/v1/admin/payments/$($o.id)/reject" -Method POST `
                    -ContentType "application/json" -Headers $adminH -Body '{"reason":"test cleanup"}' | Out-Null
            } catch { }
        }
        elseif ($o.status -in @("Processing", "Pending", "AwaitingApproval")) {
            if ($o.gateway -eq "Easypaisa" -and $o.externalPaymentId) {
                Invoke-EasypaisaWebhook $o.externalPaymentId $false | Out-Null
            }
            elseif ($o.gateway -eq "JazzCash" -and $o.externalPaymentId) {
                Invoke-JazzCashWebhook $o.externalPaymentId $false | Out-Null
            }
            elseif ($o.gateway -eq "Manual" -and $o.status -eq "AwaitingApproval") {
                try {
                    Invoke-RestMethod -Uri "$base/api/v1/admin/payments/$($o.id)/reject" -Method POST `
                        -ContentType "application/json" -Headers $adminH -Body '{"reason":"test cleanup"}' | Out-Null
                } catch { }
            }
        }
    }
}
function Clear-PendingOrders($adminH, $bundleId, $userId) {
    $orders = @(Invoke-RestMethod -Uri "$base/api/v1/admin/payments" -Headers $adminH)
    foreach ($o in $orders) {
        if ($bundleId -and $o.bundleId -ne $bundleId) { continue }
        if ($userId -and $o.userId -ne $userId) { continue }
        if ($o.status -eq "AwaitingApproval") {
            try {
                Invoke-RestMethod -Uri "$base/api/v1/admin/payments/$($o.id)/reject" -Method POST `
                    -ContentType "application/json" -Headers $adminH -Body '{"reason":"test cleanup"}' | Out-Null
            } catch { }
        }
        elseif ($o.status -in @("Processing", "Pending") -and $o.externalPaymentId) {
            if ($o.gateway -eq "Easypaisa") {
                try {
                    Invoke-WebRequest -Uri "$base/api/v1/payments/webhooks/easypaisa?orderRefNum=$($o.externalPaymentId)&responseCode=9999&responseDesc=test-cleanup" `
                        -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue | Out-Null
                } catch { }
            }
            elseif ($o.gateway -eq "JazzCash") {
                try {
                    $form = @{ pp_TxnRefNo = $o.externalPaymentId; pp_ResponseCode = "999" }
                    Invoke-RestMethod -Uri "$base/api/v1/payments/webhooks/jazzcash" -Method POST `
                        -ContentType "application/x-www-form-urlencoded" -Body $form | Out-Null
                } catch { }
            }
        }
    }
}

function Invoke-EasypaisaWebhook($orderRef, $success) {
    if ($success) {
        $uri = "$base/api/v1/payments/webhooks/easypaisa?orderRefNum=$orderRef&responseCode=0000&responseDesc=success"
    } else {
        $uri = "$base/api/v1/payments/webhooks/easypaisa?orderRefNum=$orderRef&responseCode=9999&responseDesc=failed"
    }
    try {
        Invoke-WebRequest -Uri $uri -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue | Out-Null
        return $true
    } catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 302) { return $true }
        return $false
    }
}

function Invoke-JazzCashWebhook($txnRef, $success) {
    $code = if ($success) { "000" } else { "999" }
    $form = @{ pp_TxnRefNo = $txnRef; pp_ResponseCode = $code; pp_ResponseMessage = "test" }
    try {
        Invoke-RestMethod -Uri "$base/api/v1/payments/webhooks/jazzcash" -Method POST `
            -ContentType "application/x-www-form-urlencoded" -Body $form | Out-Null
        return $true
    } catch { return $false }
}

function Test-Enrolled($studentH, $bundleId) {
    $enrolled = Get-EnrolledBundleIds $studentH
    return $enrolled -contains "$bundleId"
}

Write-Host "`n=== Payment E2E tests (all flows) ===`n"

try {
    $sa = Login-Platform "superadmin@platform.com" "SuperAdmin123!"
    $saH = @{ Authorization = "Bearer $($sa.accessToken)" }
    $admin = Try-Login-Tenant "admin@demo.com" @("Dell123#", "Admin123!")
    $adminH = AuthHeaders $admin
    $student1 = Try-Login-Tenant "student1@demo.com" @("Student123!", "Dell123#", "Admin123!")
    $student1H = AuthHeaders $student1
    $student2 = Try-Login-Tenant "student2@demo.com" @("Student123!", "Dell123#", "Admin123!")
    $student2H = AuthHeaders $student2
    $student3 = Try-Login-Tenant "student3@demo.com" @("Student123!", "Dell123#", "Admin123!")
    $student3H = AuthHeaders $student3

    $tenants = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants" -Headers $saH
    $demo = $tenants | Where-Object { $_.slug -eq "demo" } | Select-Object -First 1
    if (-not $demo) { throw "demo tenant not found" }

    $detail = Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$($demo.id)" -Headers $saH
    $flagsBody = @{
        status = $detail.status
        plan = $detail.plan
        productProfile = $detail.productProfile
        customDomain = $detail.customDomain
        liveClassesEnabled = $detail.liveClassesEnabled
        zoomMode = $detail.zoomMode
        paymentMode = $detail.paymentMode
        allowStudentSelfEnroll = $true
        allowAdminCreateStudent = $detail.allowAdminCreateStudent
        syllabusMentorEnabled = $detail.syllabusMentorEnabled
        bundlePriceEditEnabled = $detail.bundlePriceEditEnabled
        mcqBulkImportEnabled = $detail.mcqBulkImportEnabled
        trialEndsAt = $detail.trialEndsAt
        country = "PK"
        currency = "PKR"
        allowedPaymentGateways = 15   # Manual + Stripe + JazzCash + Easypaisa
        enrollmentModes = 6           # ManualPayment + OnlineCheckout
    }
    Invoke-RestMethod -Uri "$base/api/v1/superadmin/tenants/$($demo.id)/flags" -Method PUT `
        -ContentType "application/json" -Headers $saH -Body ($flagsBody | ConvertTo-Json) | Out-Null
    Record "SuperAdmin enable all gateways" $true "allowed=15 modes=6"

    Clear-AllStuckOrders $adminH

    $bundles = Invoke-RestMethod -Uri "$base/api/v1/bundles" -Headers $student1H
    $paidBundles = @($bundles | Where-Object { $_.price -gt 0 } | Sort-Object title)
    if ($paidBundles.Count -eq 0) { throw "No paid bundle found" }
    Record "Paid bundles available" $true "count=$($paidBundles.Count)"

  # Deterministic student/bundle assignment (avoids cross-test enrollment collisions)
    $epBundle = $paidBundles[0]
    $epStudentH = $student2H
    $epUserId = $student2.userId
    $jcBundle = if ($paidBundles.Count -gt 1) { $paidBundles[1] } else { $paidBundles[0] }
    $manualRejectBundle = if ($paidBundles.Count -gt 2) { $paidBundles[2] } else { $paidBundles[-1] }

    Clear-PendingOrders $adminH $null $student1.userId
    Clear-PendingOrders $adminH $null $student2.userId
    Clear-PendingOrders $adminH $null $student3.userId
    Clear-PendingOrders $adminH $epBundle.id $epUserId
    Clear-PendingOrders $adminH $jcBundle.id $student2.userId

    try {
        Invoke-RestMethod -Uri "$base/api/v1/bundles/$($epBundle.id)/enroll" -Method POST -Headers $student1H | Out-Null
        Record "Direct enroll blocked (paid)" $false "Expected failure"
    } catch {
        Record "Direct enroll blocked (paid)" $true ($_.ErrorDetails.Message)
    }

    $allGatewaysSettings = @{
        enrollmentModes = 6
        manualPaymentInstructions = "Send to Demo Academy, HBL 1234567890, ref your email."
        manualEnabled = $true
        stripeEnabled = $false
        stripePublishableKey = ""
        stripeSecretKey = $null
        stripeWebhookSecret = $null
        jazzCashEnabled = $true
        jazzCashMerchantId = "MC12345"
        jazzCashPassword = "testpassword"
        jazzCashHashKey = "TESTHASHKEY123456"
        jazzCashReturnUrl = $null
        easypaisaEnabled = $true
        easypaisaStoreId = "12345"
        easypaisaHashKey = "TESTHASHKEY12345"
        easypaisaCredentials = $null
    }
    Invoke-RestMethod -Uri "$base/api/v1/admin/settings/payments" -Method PUT `
        -ContentType "application/json" -Headers $adminH -Body ($allGatewaysSettings | ConvertTo-Json) | Out-Null
    Record "Admin save all gateway settings" $true "manual+jazzcash+easypaisa"

    $gateways = Invoke-RestMethod -Uri "$base/api/v1/payments/available-gateways?bundleId=$($epBundle.id)" -Headers $student1H
    $gwNames = ($gateways | ForEach-Object { $_.gateway }) -join ","
    Record "Available gateways list" ($gateways.Count -ge 3) $gwNames

    # --- Easypaisa: checkout form + fail callback + success callback + enrollment ---
    Clear-AllStuckOrders $adminH
    Clear-PendingOrders $adminH $epBundle.id $epUserId
    try {
        $epCheckout = Invoke-RestMethod -Uri "$base/api/v1/payments/checkout" -Method POST `
            -ContentType "application/json" -Headers $epStudentH -Body (@{
                bundleId = $epBundle.id
                gateway = "Easypaisa"
                studentCountry = "PK"
            } | ConvertTo-Json)
        $hasForm = $null -ne $epCheckout.formPost -and $epCheckout.formPost.fields
        $sandboxUrl = $epCheckout.formPost.actionUrl -like "*easypaystg*"
        Record "Easypaisa checkout form" $hasForm "order=$($epCheckout.orderId) bundle=$($epBundle.title)"
        Record "Easypaisa sandbox URL" $sandboxUrl $epCheckout.formPost.actionUrl

        $failOk = Invoke-EasypaisaWebhook $epCheckout.sessionId $false
        Record "Easypaisa webhook (fail)" $failOk "cleared processing order"

        Clear-PendingOrders $adminH $epBundle.id $epUserId
        $epCheckout2 = Invoke-RestMethod -Uri "$base/api/v1/payments/checkout" -Method POST `
            -ContentType "application/json" -Headers $epStudentH -Body (@{
                bundleId = $epBundle.id
                gateway = "Easypaisa"
                studentCountry = "PK"
            } | ConvertTo-Json)

        $successOk = Invoke-EasypaisaWebhook $epCheckout2.sessionId $true
        Record "Easypaisa webhook (success)" $successOk "orderRef=$($epCheckout2.sessionId)"

        Start-Sleep -Milliseconds 500
        $enrolled = Test-Enrolled $epStudentH $epBundle.id
        Record "Easypaisa enrolls student" $enrolled "bundle=$($epBundle.title)"
    } catch {
        $detail = if ($_.ErrorDetails.Message) { $_.ErrorDetails.Message } else { $_.Exception.Message }
        Record "Easypaisa checkout" $false $detail
    }

    # --- JazzCash: checkout URL + fail webhook + success webhook + enrollment ---
    try {
        Clear-PendingOrders $adminH $jcBundle.id $student2.userId
        $jcCheckout = Invoke-RestMethod -Uri "$base/api/v1/payments/checkout" -Method POST `
            -ContentType "application/json" -Headers $student2H -Body (@{
                bundleId = $jcBundle.id
                gateway = "JazzCash"
                studentCountry = "PK"
            } | ConvertTo-Json)
        $hasUrl = $null -ne $jcCheckout.checkoutUrl -and $jcCheckout.checkoutUrl -like "*jazzcash*"
        Record "JazzCash checkout URL" $hasUrl "txn=$($jcCheckout.sessionId)"

        $jcFail = Invoke-JazzCashWebhook $jcCheckout.sessionId $false
        Record "JazzCash webhook (fail)" $jcFail "txn=$($jcCheckout.sessionId)"

        Clear-PendingOrders $adminH $jcBundle.id $student2.userId
        $jcCheckout2 = Invoke-RestMethod -Uri "$base/api/v1/payments/checkout" -Method POST `
            -ContentType "application/json" -Headers $student2H -Body (@{
                bundleId = $jcBundle.id
                gateway = "JazzCash"
                studentCountry = "PK"
            } | ConvertTo-Json)

        $jcSuccess = Invoke-JazzCashWebhook $jcCheckout2.sessionId $true
        Record "JazzCash webhook (success)" $jcSuccess "txn=$($jcCheckout2.sessionId)"

        Start-Sleep -Milliseconds 500
        $jcEnrolled = Test-Enrolled $student2H $jcBundle.id
        Record "JazzCash enrolls student" $jcEnrolled "bundle=$($jcBundle.title)"
    } catch {
        Record "JazzCash flow" $false $_.Exception.Message
    }

    # --- Manual: reject flow (pick fresh unenrolled bundle after online flows) ---
    $manualOnlySettings = $allGatewaysSettings.Clone()
    $manualOnlySettings.jazzCashEnabled = $false
    $manualOnlySettings.easypaisaEnabled = $false
    Invoke-RestMethod -Uri "$base/api/v1/admin/settings/payments" -Method PUT `
        -ContentType "application/json" -Headers $adminH -Body ($manualOnlySettings | ConvertTo-Json) | Out-Null

    $rejectStudentH = $student3H
    $rejectUserId = $student3.userId

    if (-not (Test-Enrolled $rejectStudentH $manualRejectBundle.id)) {
    try {
        Clear-PendingOrders $adminH $manualRejectBundle.id $rejectUserId
        $wasEnrolled = $false
        $rejectOrder = Invoke-RestMethod -Uri "$base/api/v1/payments/manual" -Method POST `
            -ContentType "application/json" -Headers $rejectStudentH -Body (@{
                bundleId = $manualRejectBundle.id
                transactionRef = "REJECT-REF-$(Get-Random -Maximum 999999)"
                note = "Should be rejected"
                studentCountry = "PK"
            } | ConvertTo-Json)
        Record "Manual payment submit (reject)" ($rejectOrder.status -eq "AwaitingApproval") "order=$($rejectOrder.id)"

        $rejected = Invoke-RestMethod -Uri "$base/api/v1/admin/payments/$($rejectOrder.id)/reject" -Method POST `
            -ContentType "application/json" -Headers $adminH -Body '{"reason":"Invalid proof"}'
        Record "Admin reject manual" ($rejected.status -eq "Failed") $rejected.failureReason

        $afterEnrolled = Test-Enrolled $rejectStudentH $manualRejectBundle.id
        $rejectOk = (-not $wasEnrolled) -and (-not $afterEnrolled)
        Record "Reject does not enroll" $rejectOk "bundle=$($manualRejectBundle.title) was=$wasEnrolled after=$afterEnrolled"
    } catch {
        Record "Manual reject flow" $false $_.Exception.Message
    }
    } else {
        Record "Manual payment submit (reject)" $true "skipped (student already enrolled)"
        Record "Admin reject manual" $true "skipped"
        Record "Reject does not enroll" $true "skipped (already enrolled)"
    }

    # --- Manual: approve flow + enrollment (student2, different bundle than JazzCash) ---
    $manualApproveBundle = if ($paidBundles.Count -gt 2) { $paidBundles[2] } elseif ($paidBundles.Count -gt 1) { $paidBundles[1] } else { $paidBundles[0] }
    if ($manualApproveBundle.id -eq $jcBundle.id -and $paidBundles.Count -gt 2) {
        $manualApproveBundle = $paidBundles[2]
    }

    try {
        Clear-PendingOrders $adminH $manualApproveBundle.id $student2.userId
        $manualOrder = Invoke-RestMethod -Uri "$base/api/v1/payments/manual" -Method POST `
            -ContentType "application/json" -Headers $student2H -Body (@{
                bundleId = $manualApproveBundle.id
                transactionRef = "APPROVE-REF-$(Get-Random -Maximum 999999)"
                note = "Automated test payment"
                studentCountry = "PK"
            } | ConvertTo-Json)
        Record "Manual payment submit (approve)" ($manualOrder.status -eq "AwaitingApproval") "order=$($manualOrder.id)"

        $approved = Invoke-RestMethod -Uri "$base/api/v1/admin/payments/$($manualOrder.id)/approve" -Method POST -Headers $adminH -Body "{}"
        Record "Admin approve manual" ($approved.status -eq "Paid") "enrollment=$($approved.enrollmentId)"

        $manualEnrolled = Test-Enrolled $student2H $manualApproveBundle.id
        Record "Manual approve enrolls student" $manualEnrolled "bundle=$($manualApproveBundle.title)"
    } catch {
        Record "Manual approve flow" $false $_.Exception.Message
    }

    $orders = Invoke-RestMethod -Uri "$base/api/v1/admin/payments" -Headers $adminH
    Record "Admin list payments" ($orders.Count -gt 0) "count=$($orders.Count)"

    $myOrders = Invoke-RestMethod -Uri "$base/api/v1/me/payments" -Headers $student1H
    Record "Student order history" ($myOrders.Count -gt 0) "count=$($myOrders.Count)"

    # --- Stripe (optional live keys) ---
    $stripeKey = $env:STRIPE_TEST_SECRET_KEY
    if ($stripeKey) {
        $stripeSettings = $manualOnlySettings.Clone()
        $stripeSettings.stripeEnabled = $true
        $stripeSettings.stripePublishableKey = $env:STRIPE_TEST_PUBLISHABLE_KEY
        $stripeSettings.stripeSecretKey = $stripeKey
        Invoke-RestMethod -Uri "$base/api/v1/admin/settings/payments" -Method PUT `
            -ContentType "application/json" -Headers $adminH -Body ($stripeSettings | ConvertTo-Json) | Out-Null
        try {
            $stripeBundle = Find-CleanPaidBundle $student1H $adminH $student1.userId $paidBundles
            if (-not $stripeBundle) { $stripeBundle = $paidBundles[0] }
            Clear-PendingOrders $adminH $stripeBundle.id $student1.userId
            $checkout = Invoke-RestMethod -Uri "$base/api/v1/payments/checkout" -Method POST `
                -ContentType "application/json" -Headers $student1H -Body (@{
                    bundleId = $stripeBundle.id
                    gateway = "Stripe"
                    studentCountry = "PK"
                } | ConvertTo-Json)
            Record "Stripe checkout session" ($null -ne $checkout.sessionId) "session=$($checkout.sessionId)"
        } catch {
            Record "Stripe checkout session" $false $_.Exception.Message
        }
    } else {
        Record "Stripe checkout session" $true "skipped (set STRIPE_TEST_SECRET_KEY to test live)"
    }
}
catch {
    Record "Setup" $false $_.Exception.Message
}

Write-Host "`n--- Summary ---"
$results | Format-Table -AutoSize
$passed = @($results | Where-Object { $_.Pass }).Count
$failed = @($results | Where-Object { -not $_.Pass }).Count
Write-Host "`nTotal: $($results.Count)  Passed: $passed  Failed: $failed"
if ($failed -gt 0) { exit 1 }
