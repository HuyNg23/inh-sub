# Script push all Docker images to registry
# Usage: .\push-all-images.ps1 -Registry "your-registry.azurecr.io" -Tag "v1.0.0"

param(
    [Parameter(Mandatory=$true)]
    [string]$Registry,
    
    [Parameter(Mandatory=$false)]
    [string]$Tag = "latest"
)

$ErrorActionPreference = "Stop"

$projects = @(
    "IBox.Client",
    "IBox.LogService",
    "IBox.RestService", 
    "IBox.Root.Client",
    "IBox.Schedule",
    "IBox.StoreService"
)

Write-Host "=== Pushing Docker Images ===" -ForegroundColor Cyan
Write-Host "Registry: $Registry" -ForegroundColor Yellow
Write-Host "Tag: $Tag" -ForegroundColor Yellow
Write-Host ""

foreach ($project in $projects) {
    $imageName = $project.ToLower().Replace(".", "-")
    $fullImageName = "$Registry/$imageName:$Tag"
    
    Write-Host "Pushing $project..." -ForegroundColor Green
    Write-Host "  Image: $fullImageName" -ForegroundColor Gray
    
    docker push $fullImageName
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to push $project" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "  ✓ Pushed successfully" -ForegroundColor Green
    Write-Host ""
}

Write-Host "=== Push Complete ===" -ForegroundColor Cyan
