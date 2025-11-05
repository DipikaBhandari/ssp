#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Deployment script for Weather Image Azure Function App
.DESCRIPTION
    This script builds the .NET application, creates Azure resources using Bicep template,
    and deploys the function app to Azure.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location = "westeurope",
    
    [Parameter(Mandatory=$false)]
    [string]$UnsplashAccessKey = "",
    
    [Parameter(Mandatory=$false)]
    [securestring]$ApiKey
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Weather Image App Deployment Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if Azure CLI is installed
Write-Host "Checking Azure CLI installation..." -ForegroundColor Yellow
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
}

# Check if .NET SDK is installed
Write-Host "Checking .NET SDK installation..." -ForegroundColor Yellow
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK is not installed. Please install .NET 8 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

# Login to Azure (if not already logged in)
Write-Host "Checking Azure login status..." -ForegroundColor Yellow
$accountInfo = az account show 2>$null
if (-not $accountInfo) {
    Write-Host "Logging in to Azure..." -ForegroundColor Yellow
    az login
}

$subscription = az account show --query name -o tsv
Write-Host "Using subscription: $subscription" -ForegroundColor Green
Write-Host ""

# Prompt for API key if not provided
if (-not $ApiKey) {
    Write-Host "API Key is required for authentication." -ForegroundColor Yellow
    $ApiKey = Read-Host "Enter API Key" -AsSecureString
}

# Convert SecureString to plain text for Azure deployment
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($ApiKey)
$ApiKeyPlainText = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

# Create resource group
Write-Host "Creating resource group '$ResourceGroupName' in '$Location'..." -ForegroundColor Yellow
az group create --name $ResourceGroupName --location $Location --output none
Write-Host "Resource group created successfully!" -ForegroundColor Green
Write-Host ""

# Deploy Bicep template
Write-Host "Deploying Azure resources using Bicep template..." -ForegroundColor Yellow
$deploymentOutput = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file "./deploy/main.bicep" `
    --parameters location=$Location apiKey=$ApiKeyPlainText `
    --query "properties.outputs" `
    --output json

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to deploy Bicep template"
    exit 1
}

$deploymentResult = $deploymentOutput | ConvertFrom-Json
$functionAppName = $deploymentResult.functionAppName.value
$functionAppUrl = $deploymentResult.functionAppUrl.value
$storageAccountName = $deploymentResult.storageAccountName.value

Write-Host "Azure resources deployed successfully!" -ForegroundColor Green
Write-Host "  Function App: $functionAppName" -ForegroundColor Cyan
Write-Host "  Function URL: $functionAppUrl" -ForegroundColor Cyan
Write-Host "  Storage Account: $storageAccountName" -ForegroundColor Cyan
Write-Host ""

# Update Unsplash API key if provided
if ($UnsplashAccessKey) {
    Write-Host "Updating Unsplash API key..." -ForegroundColor Yellow
    az functionapp config appsettings set `
        --name $functionAppName `
        --resource-group $ResourceGroupName `
        --settings "UnsplashAccessKey=$UnsplashAccessKey" `
        --output none
    Write-Host "Unsplash API key updated!" -ForegroundColor Green
    Write-Host ""
}

# Build the .NET project
Write-Host "Building .NET project..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build .NET project"
    exit 1
}
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host ""

# Publish the .NET project
Write-Host "Publishing .NET project..." -ForegroundColor Yellow
$publishPath = "./bin/Release/net8.0/publish"
dotnet publish --configuration Release --output $publishPath
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish .NET project"
    exit 1
}
Write-Host "Publish completed successfully!" -ForegroundColor Green
Write-Host ""

# Create deployment package
Write-Host "Creating deployment package..." -ForegroundColor Yellow
$zipPath = "./deploy/function.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path "$publishPath/*" -DestinationPath $zipPath
Write-Host "Deployment package created!" -ForegroundColor Green
Write-Host ""

# Deploy to Azure Functions
Write-Host "Deploying function app to Azure..." -ForegroundColor Yellow
az functionapp deployment source config-zip `
    --resource-group $ResourceGroupName `
    --name $functionAppName `
    --src $zipPath `
    --output none

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to deploy function app"
    exit 1
}

Write-Host "Function app deployed successfully!" -ForegroundColor Green
Write-Host ""

# Wait for deployment to complete
Write-Host "Waiting for deployment to complete..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Test the deployment
Write-Host "Testing deployment..." -ForegroundColor Yellow
Write-Host ""

Write-Host "=====================================" -ForegroundColor Green
Write-Host "Deployment Completed Successfully!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Function App URL: $functionAppUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "API Endpoints:" -ForegroundColor Yellow
Write-Host "  Start Job:  POST   $functionAppUrl/api/job/start" -ForegroundColor Cyan
Write-Host "  Get Status: GET    $functionAppUrl/api/job/{jobId}" -ForegroundColor Cyan
Write-Host ""
Write-Host "Example usage:" -ForegroundColor Yellow
Write-Host "  # Start a new job" -ForegroundColor Gray
Write-Host "  curl -X POST $functionAppUrl/api/job/start" -ForegroundColor Gray
Write-Host ""
Write-Host "  # Check job status (replace {jobId} with actual job ID)" -ForegroundColor Gray
Write-Host "  curl $functionAppUrl/api/job/{jobId}" -ForegroundColor Gray
Write-Host ""

# Clean up
Write-Host "Cleaning up temporary files..." -ForegroundColor Yellow
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Write-Host "Cleanup completed!" -ForegroundColor Green
Write-Host ""

Write-Host "Note: It may take a few minutes for the function app to fully start." -ForegroundColor Yellow
Write-Host "If you receive errors when testing, please wait a moment and try again." -ForegroundColor Yellow
