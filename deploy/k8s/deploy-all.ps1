# Script deploy all IBox services to Kubernetes
# Usage: .\deploy-all.ps1 [-Namespace "ibox"] [-Registry "your-registry.azurecr.io"] [-Tag "v1.0.0"]

param(
    [Parameter(Mandatory=$false)]
    [string]$Namespace = "default",
    
    [Parameter(Mandatory=$false)]
    [string]$Registry = "your-registry",
    
    [Parameter(Mandatory=$false)]
    [string]$Tag = "latest"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Deploying IBox to Kubernetes ===" -ForegroundColor Cyan
Write-Host "Namespace: $Namespace" -ForegroundColor Yellow
Write-Host "Registry: $Registry" -ForegroundColor Yellow
Write-Host "Tag: $Tag" -ForegroundColor Yellow
Write-Host ""

# Create namespace if not exists
Write-Host "Checking namespace..." -ForegroundColor Green
kubectl create namespace $Namespace --dry-run=client -o yaml | kubectl apply -f -

# Update image references in deployment files
$deploymentFiles = @(
    "ibox-client.yaml",
    "ibox-logservice.yaml",
    "ibox-restservice.yaml",
    "ibox-root-client.yaml",
    "ibox-schedule.yaml",
    "ibox-storeservice.yaml"
)

Write-Host ""
Write-Host "Updating image references..." -ForegroundColor Green
foreach ($file in $deploymentFiles) {
    $content = Get-Content $file -Raw
    $content = $content -replace 'your-registry/([^:]+):latest', "$Registry/`$1:$Tag"
    $content | Set-Content $file
    Write-Host "  ✓ Updated $file" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Deploying ConfigMap and Secrets..." -ForegroundColor Green
kubectl apply -f configmap.yaml -n $Namespace
if (Test-Path "secret.yaml") {
    kubectl apply -f secret.yaml -n $Namespace
} else {
    Write-Host "  ⚠ secret.yaml not found. Please create it manually." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Deploying PVCs..." -ForegroundColor Green
kubectl apply -f pvc.yaml -n $Namespace

Write-Host ""
Write-Host "Deploying services..." -ForegroundColor Green
foreach ($file in $deploymentFiles) {
    Write-Host "  Deploying $file..." -ForegroundColor Gray
    kubectl apply -f $file -n $Namespace
}

Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Check deployment status:" -ForegroundColor Yellow
Write-Host "  kubectl get pods -n $Namespace" -ForegroundColor Gray
Write-Host "  kubectl get svc -n $Namespace" -ForegroundColor Gray
Write-Host ""
Write-Host "View logs:" -ForegroundColor Yellow
Write-Host "  kubectl logs -f deployment/ibox-logservice -n $Namespace" -ForegroundColor Gray
