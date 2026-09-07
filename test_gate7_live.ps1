# test_gate7_live.ps1 - Live End-to-End Test for Gate 7 Autonomous Revenue System
$ErrorActionPreference = "Stop"

Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "   GATE 7 LIVE VERIFICATION - REVENUE OPERATING SYSTEM    " -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

$baseUrl = "http://localhost:5000"

# Step 1: Authenticate
Write-Host "`n[1/5] Authenticating with Live API..." -ForegroundColor Yellow
$loginPayload = @{
    email = "mayur@bitbloom.in"
    password = "Password123!"
} | ConvertTo-Json

$loginRes = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginPayload -ContentType "application/json"
$token = $loginRes.token
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}
Write-Host "  -> Authenticated successfully as: $($loginRes.user.email) ($($loginRes.user.role))" -ForegroundColor Green

# Step 2: Create & Launch Gate 7 Autonomous Revenue Mission
Write-Host "`n[2/5] Launching Gate 7 Autonomous Revenue Mission..." -ForegroundColor Yellow
$missionPayload = @{
    title = "BFSI Autonomous Revenue Campaign (Q4)"
    objective = "Generate Rs 50L qualified pipeline in Indian BFSI with Governed Multi-Persona Account Graph"
    targetIndustry = "Enterprise BFSI"
    targetProspectCount = 25
    targetValueINR = 5000000.0
    mode = 0 # Simulation Mode with Real-World Connectors
    autonomyLevel = 3 # Level 3 Controlled Autonomy
    walletBudgetINR = 15000.0
} | ConvertTo-Json

$mission = Invoke-RestMethod -Uri "$baseUrl/api/agentmissions" -Method Post -Body $missionPayload -Headers $headers
$missionId = $mission.id
Write-Host "  -> Mission Launched: ID=$missionId" -ForegroundColor Green
Write-Host "  -> Initial Status: $($mission.status)" -ForegroundColor Cyan
Write-Host "  -> Pipeline Value Generated: Rs $($mission.pipelineValueGeneratedINR)" -ForegroundColor Cyan
Write-Host "  -> Target Pipeline: Rs $($mission.targetValueINR)" -ForegroundColor Cyan
Write-Host "  -> Companies Researched: $($mission.companiesResearched)" -ForegroundColor Cyan
Write-Host "  -> Prospects Discovered: $($mission.prospectsDiscovered)" -ForegroundColor Cyan
Write-Host "  -> Qualified Leads: $($mission.qualifiedCount)" -ForegroundColor Cyan
Write-Host "  -> Outreach Sent: $($mission.outreachSent)" -ForegroundColor Cyan

# Step 3: Query Trajectory Health & Bottleneck Diagnosis
Write-Host "`n[3/5] Inspecting Mission Trajectory & Diagnosis..." -ForegroundColor Yellow
$trajectory = Invoke-RestMethod -Uri "$baseUrl/api/agentmissions/$missionId/trajectory" -Method Get -Headers $headers
Write-Host "  -> Trajectory Health: $($trajectory.trajectoryHealth)" -ForegroundColor Magenta
Write-Host "  -> Response Rate: $($trajectory.responseRate)%" -ForegroundColor Magenta
Write-Host "  -> Bottleneck Diagnosis: $($trajectory.bottleneckDiagnosis)" -ForegroundColor Yellow
Write-Host "  -> Recommended Pivot: $($trajectory.recommendedPivot)" -ForegroundColor Yellow

# Step 4: Execute Autonomous Closed-Loop Re-plan
Write-Host "`n[4/5] Triggering Autonomous Closed-Loop Re-plan..." -ForegroundColor Yellow
$replan = Invoke-RestMethod -Uri "$baseUrl/api/agentmissions/$missionId/replan" -Method Post -Headers $headers
Write-Host "  -> Re-plan Complete: Status = $($replan.status), Trajectory = $($replan.trajectoryStatus)" -ForegroundColor Green
Write-Host "  -> Total Tasks in Augmented DAG: $($replan.tasks.Count)" -ForegroundColor Green
Write-Host "  -> Pipeline Boost Generated: Rs $($replan.pipelineValueGeneratedINR) / Rs $($replan.targetValueINR)" -ForegroundColor Green

# Step 5: Check Gated Tasks and Resolve Executive Approval
Write-Host "`n[5/5] Checking Gated Tasks & Approving Commercial Proposal..." -ForegroundColor Yellow
$gatedTask = $null
foreach ($t in $replan.tasks) {
    Write-Host "      * [$($t.status)] $($t.title) (Role: $($t.assignedRole), Cost: Rs $($t.actualCostINR))"
    if ($t.status -eq "BlockedOnApproval" -or $t.status -eq 3) {
        $gatedTask = $t
    }
}

if ($gatedTask -ne $null) {
    Write-Host "`n  -> Human Authorization Gate Detected on: $($gatedTask.title) ($($gatedTask.id))" -ForegroundColor Yellow
    $approvedMission = Invoke-RestMethod -Uri "$baseUrl/api/agentmissions/$missionId/approve-task/$($gatedTask.id)" -Method Post -Headers $headers
    Write-Host "  -> Gated Proposal Approved! Mission Status: $($approvedMission.status)" -ForegroundColor Green
    Write-Host "  -> Final Pipeline Generated: Rs $($approvedMission.pipelineValueGeneratedINR)" -ForegroundColor Green
    Write-Host "  -> Final Opportunities Created: $($approvedMission.opportunitiesCreated)" -ForegroundColor Green
    Write-Host "  -> Final Wallet Consumed: Rs $($approvedMission.wallet.consumedSpendINR) / Rs $($approvedMission.wallet.totalBudgetINR)" -ForegroundColor Green
}

# Final Verification Summary
$finalMission = Invoke-RestMethod -Uri "$baseUrl/api/agentmissions/$missionId" -Method Get -Headers $headers
Write-Host "`n=========================================================" -ForegroundColor Green
Write-Host "   GATE 7 LIVE VERIFICATION COMPLETED SUCCESSFULLY!       " -ForegroundColor Green
Write-Host "=========================================================" -ForegroundColor Green
Write-Host "  Mission ID: $($finalMission.id)" -ForegroundColor White
Write-Host "  Final Status: $($finalMission.status)" -ForegroundColor Green
Write-Host "  Trajectory: $($finalMission.trajectoryStatus)" -ForegroundColor Green
Write-Host "  Target Pipeline: Rs $($finalMission.targetValueINR)" -ForegroundColor White
Write-Host "  Actual Pipeline Generated: Rs $($finalMission.pipelineValueGeneratedINR)" -ForegroundColor Green
Write-Host "  Opportunities Created: $($finalMission.opportunitiesCreated)" -ForegroundColor Green
Write-Host "  Companies Researched: $($finalMission.companiesResearched)" -ForegroundColor White
Write-Host "  Prospects Discovered: $($finalMission.prospectsDiscovered)" -ForegroundColor White
Write-Host "  Qualified Leads: $($finalMission.qualifiedCount)" -ForegroundColor White
Write-Host "  Outreach Sent: $($finalMission.outreachSent)" -ForegroundColor White
Write-Host "  Responses Received: $($finalMission.responsesReceived)" -ForegroundColor White
Write-Host "  Wallet Budget Spent: Rs $($finalMission.wallet.consumedSpendINR) / Rs $($finalMission.wallet.totalBudgetINR)" -ForegroundColor White
Write-Host "  ROI Multiplier: $([Math]::Round($finalMission.pipelineValueGeneratedINR / [Math]::Max(1, $finalMission.wallet.consumedSpendINR), 1))x" -ForegroundColor Green
Write-Host "=========================================================" -ForegroundColor Green
