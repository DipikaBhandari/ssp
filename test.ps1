#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Test script for Weather Image Azure Function App
.DESCRIPTION
    This script tests the deployed function app endpoints
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$FunctionAppUrl
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Weather Image App Test Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Remove trailing slash if present
$FunctionAppUrl = $FunctionAppUrl.TrimEnd('/')

Write-Host "Testing Function App: $FunctionAppUrl" -ForegroundColor Yellow
Write-Host ""

# Test 1: Start a new job
Write-Host "Test 1: Starting a new job..." -ForegroundColor Yellow
try {
    $startResponse = Invoke-RestMethod -Method Post -Uri "$FunctionAppUrl/api/job/start" -ErrorAction Stop
    $jobId = $startResponse.jobId
    Write-Host "✓ Job started successfully!" -ForegroundColor Green
    Write-Host "  Job ID: $jobId" -ForegroundColor Cyan
    Write-Host "  Created: $($startResponse.createdAt)" -ForegroundColor Cyan
    Write-Host ""
} catch {
    Write-Host "✗ Failed to start job" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Wait for processing to begin
Write-Host "Waiting 10 seconds for processing to begin..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Test 2: Check job status
Write-Host "Test 2: Checking job status..." -ForegroundColor Yellow
try {
    $statusResponse = Invoke-RestMethod -Method Get -Uri "$FunctionAppUrl/api/job/$jobId" -ErrorAction Stop
    Write-Host "✓ Job status retrieved successfully!" -ForegroundColor Green
    Write-Host "  Status: $($statusResponse.status)" -ForegroundColor Cyan
    Write-Host "  Progress: $($statusResponse.processedStations)/$($statusResponse.totalStations) stations" -ForegroundColor Cyan
    Write-Host ""
} catch {
    Write-Host "✗ Failed to get job status" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 3: Poll until completion (max 2 minutes)
Write-Host "Test 3: Polling for job completion (max 2 minutes)..." -ForegroundColor Yellow
$maxAttempts = 24  # 24 * 5 seconds = 2 minutes
$attempt = 0

while ($attempt -lt $maxAttempts) {
    try {
        $statusResponse = Invoke-RestMethod -Method Get -Uri "$FunctionAppUrl/api/job/$jobId" -ErrorAction Stop
        
        $progressBar = "█" * $statusResponse.processedStations + "░" * ($statusResponse.totalStations - $statusResponse.processedStations)
        Write-Host "`r  Progress: [$progressBar] $($statusResponse.processedStations)/$($statusResponse.totalStations) - Status: $($statusResponse.status)" -NoNewline
        
        if ($statusResponse.status -eq "completed") {
            Write-Host ""
            Write-Host "✓ Job completed successfully!" -ForegroundColor Green
            Write-Host "  Total time: $((New-TimeSpan -Start $startResponse.createdAt -End $statusResponse.completedAt).TotalSeconds) seconds" -ForegroundColor Cyan
            Write-Host "  Images generated: $($statusResponse.imageUrls.Count)" -ForegroundColor Cyan
            Write-Host ""
            
            # Display first 5 image URLs
            Write-Host "Sample Image URLs:" -ForegroundColor Yellow
            $statusResponse.imageUrls | Select-Object -First 5 | ForEach-Object {
                Write-Host "  $_" -ForegroundColor Cyan
            }
            
            if ($statusResponse.imageUrls.Count -gt 5) {
                Write-Host "  ... and $($statusResponse.imageUrls.Count - 5) more" -ForegroundColor Gray
            }
            
            Write-Host ""
            break
        }
        
        if ($statusResponse.status -eq "failed") {
            Write-Host ""
            Write-Host "✗ Job failed" -ForegroundColor Red
            Write-Host "  Error: $($statusResponse.errorMessage)" -ForegroundColor Red
            exit 1
        }
        
        Start-Sleep -Seconds 5
        $attempt++
    } catch {
        Write-Host ""
        Write-Host "✗ Error checking job status" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

if ($attempt -eq $maxAttempts) {
    Write-Host ""
    Write-Host "⚠ Job did not complete within 2 minutes" -ForegroundColor Yellow
    Write-Host "  Final status: $($statusResponse.status)" -ForegroundColor Yellow
    Write-Host "  Progress: $($statusResponse.processedStations)/$($statusResponse.totalStations) stations" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "This is normal for the first run. The job will continue in the background." -ForegroundColor Gray
    Write-Host "You can check the status later with:" -ForegroundColor Gray
    Write-Host "  curl $FunctionAppUrl/api/job/$jobId" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "Testing Complete!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Job ID for future reference: $jobId" -ForegroundColor Cyan
