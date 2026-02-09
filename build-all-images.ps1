# Script build all Docker images cho IBox projects
# Usage: .\build-all-images.ps1 -Registry "your-registry.azurecr.io" -Tag "v1.0.0"

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

Write-Host "=== Building Docker Images ===" -ForegroundColor Cyan
Write-Host "Registry: $Registry" -ForegroundColor Yellow
Write-Host "Tag: $Tag" -ForegroundColor Yellow
Write-Host ""

foreach ($project in $projects) {
    $imageName = $project.ToLower().Replace(".", "-")
    $fullImageName = "$Registry/$imageName:$Tag"
    
    Write-Host "Building $project..." -ForegroundColor Green
    Write-Host "  Image: $fullImageName" -ForegroundColor Gray
    
    docker build -f "$project/Dockerfile" -t $fullImageName .
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to build $project" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "  ✓ Built successfully" -ForegroundColor Green
    Write-Host ""
}

Write-Host "=== Build Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "To push images to registry, run:" -ForegroundColor Yellow
foreach ($project in $projects) {
    $imageName = $project.ToLower().Replace(".", "-")
    Write-Host "  docker push $Registry/$imageName:$Tag" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Or use: .\push-all-images.ps1 -Registry $Registry -Tag $Tag" -ForegroundColor Yellow
