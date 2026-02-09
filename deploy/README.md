# IBox Deployment Guide

Hướng dẫn build Docker images và triển khai lên Kubernetes cho các services IBox.

## 📋 Danh sách Services

5 services chính được containerize:
- **IBox.LogService** - Xử lý log và scheduled jobs
- **IBox.RestService** - REST API service 
- **IBox.Root.Client** - Root client service with SignalR
- **IBox.Schedule** - Scheduling service với Quartz
- **IBox.StoreService** - Store service

## 🚀 Quy trình Deploy

### Bước 0: Test Local với Docker Compose (Khuyến nghị)

Trước khi deploy lên K8s, nên test với Docker Compose để đảm bảo không có breaking changes:

```powershell
# Copy và config environment
copy .env.example .env
notepad .env  # Điền DB connection string

# Chạy tất cả services với ports giống IIS
docker-compose up --build

# Test các services
curl http://localhost:5001  # Client
curl http://localhost:5002  # LogService
curl http://localhost:5003  # RestService
curl http://localhost:5004  # Root.Client
curl http://localhost:5005  # Schedule
curl http://localhost:5006  # StoreService
```

Xem chi tiết tại [MIGRATION-GUIDE.md](../MIGRATION-GUIDE.md) và [DOCKER-QUICKSTART.md](../DOCKER-QUICKSTART.md)

### Bước 1: Build Docker Images

```powershell
# Build tất cả images
.\build-all-images.ps1 -Registry "your-registry.azurecr.io" -Tag "v1.0.0"

# Hoặc build từng image
docker build -f IBox.LogService/Dockerfile -t your-registry/ibox-logservice:v1.0.0 .
docker build -f IBox.RestService/Dockerfile -t your-registry/ibox-restservice:v1.0.0 .
docker build -f IBox.Root.Client/Dockerfile -t your-registry/ibox-root-client:v1.0.0 .
docker build -f IBox.Schedule/Dockerfile -t your-registry/ibox-schedule:v1.0.0 .
docker build -f IBox.StoreService/Dockerfile -t your-registry/ibox-storeservice:v1.0.0 .
```

### Bước 2: Push Images lên Registry

```powershell
# Push tất cả images
.\push-all-images.ps1 -Registry "your-registry.azurecr.io" -Tag "v1.0.0"

# Nếu dùng Azure Container Registry, login trước:
az acr login --name your-registry
```

### Bước 3: Chuẩn bị Kubernetes Secrets

**QUAN TRỌNG:** Tạo file secret chứa thông tin nhạy cảm

```powershell
# Edit file deploy/k8s/secret.yaml và cập nhật:
# - Database connection string
# - JWT secret keys
# - Passwords

# KHÔNG commit file này vào git!
```

### Bước 4: Deploy lên Kubernetes

```powershell
cd deploy/k8s

# Tự động deploy tất cả
.\deploy-all.ps1 -Namespace "ibox" -Registry "your-registry.azurecr.io" -Tag "v1.0.0"

# Hoặc deploy thủ công từng bước:

# 1. Create ConfigMap và Secrets
kubectl apply -f configmap.yaml -n ibox
kubectl apply -f secret.yaml -n ibox

# 2. Create PVCs (nếu cần persistent storage)
kubectl apply -f pvc.yaml -n ibox

# 3. Deploy services
kubectl apply -f ibox-logservice.yaml -n ibox
kubectl apply -f ibox-restservice.yaml -n ibox
kubectl apply -f ibox-root-client.yaml -n ibox
kubectl apply -f ibox-schedule.yaml -n ibox
kubectl apply -f ibox-storeservice.yaml -n ibox

# 4. (Optional) Deploy Ingress
kubectl apply -f ingress.yaml -n ibox
```

## 🔍 Kiểm tra Deployment

```powershell
# Xem trạng thái pods
kubectl get pods -n ibox

# Xem services
kubectl get svc -n ibox

# Xem logs của service
kubectl logs -f deployment/ibox-restservice -n ibox

# Xem chi tiết deployment
kubectl describe deployment ibox-restservice -n ibox

# Port-forward để test local
kubectl port-forward svc/ibox-restservice 8080:80 -n ibox
```

## 📦 Cấu trúc Thư mục

```
deploy/
├── k8s/
│   ├── configmap.yaml         # ConfigMap cho các settings chung
│   ├── secret.yaml            # Secrets (DB passwords, JWT keys)
│   ├── pvc.yaml               # PersistentVolumeClaims
│   ├── ibox-logservice.yaml   # Deployment + Service
│   ├── ibox-restservice.yaml
│   ├── ibox-root-client.yaml
│   ├── ibox-schedule.yaml
│   ├── ibox-storeservice.yaml
│   ├── ingress.yaml           # Ingress routing
│   └── deploy-all.ps1         # Script deploy tự động
├── build-all-images.ps1       # Script build images
└── push-all-images.ps1        # Script push images
```

## ⚙️ Configuration

### ConfigMap (configmap.yaml)
Chứa các cấu hình không nhạy cảm:
- CORS settings
- JWT issuer và expiration
- Logging configuration
- Schedule cron expressions

### Secrets (secret.yaml)
Chứa thông tin nhạy cảm:
- Database connection strings
- Passwords
- JWT secret keys

**LƯU Ý:** File secret.yaml không được commit vào Git. Tạo file này riêng cho từng môi trường.

### Persistent Storage

Mặc định các deployment sử dụng PersistentVolumeClaims:
- `ibox-logs-pvc` - 10GB cho logs
- `ibox-chatbot-db-pvc` - 20GB cho ChatBot databases
- `ibox-log-db-pvc` - 20GB cho IBox log databases

Nếu không cần persistent, có thể chuyển về `emptyDir` trong deployment YAML.

## 🔧 Resource Limits

Mỗi service có resource requests/limits:
- **Requests:** 250m CPU, 512Mi RAM
- **Limits:** 1000m CPU, 2Gi RAM

Điều chỉnh trong deployment YAML nếu cần.

## 🏥 Health Checks

Tất cả services có:
- **Liveness Probe:** Kiểm tra service còn sống
- **Readiness Probe:** Kiểm tra service sẵn sàng nhận traffic

## 🌐 Ingress

File `ingress.yaml` cấu hình routing cho:
- `api.ibox.yourdomain.com` → RestService
- `root.ibox.yourdomain.com` → Root.Client
- `schedule.ibox.yourdomain.com` → Schedule
- `store.ibox.yourdomain.com` → StoreService
- `logs.ibox.yourdomain.com` → LogService

## 📊 Scaling

Scale services theo nhu cầu:

```powershell
# Scale RestService lên 3 replicas
kubectl scale deployment ibox-restservice --replicas=3 -n ibox

# Auto-scaling (cần metrics-server)
kubectl autoscale deployment ibox-restservice --min=2 --max=10 --cpu-percent=80 -n ibox
```

## 🐛 Troubleshooting

### Pod không start được
```powershell
kubectl describe pod <pod-name> -n ibox
kubectl logs <pod-name> -n ibox
```

### Database connection issues
- Kiểm tra secret đã được tạo: `kubectl get secret ibox-secrets -n ibox`
- Xem logs: `kubectl logs -f deployment/ibox-restservice -n ibox`
- Verify connection string trong secret

### Image pull errors
```powershell
# Kiểm tra image pull secrets
kubectl get secrets -n ibox

# Tạo secret cho private registry
kubectl create secret docker-registry acr-secret \
  --docker-server=your-registry.azurecr.io \
  --docker-username=<username> \
  --docker-password=<password> \
  -n ibox

# Thêm vào deployment spec
spec:
  imagePullSecrets:
    - name: acr-secret
```

## 🔐 Security Best Practices

1. **Không commit secrets vào Git**
2. Sử dụng Azure Key Vault hoặc HashiCorp Vault cho production
3. Enable RBAC trong K8s cluster
4. Scan images cho vulnerabilities trước khi deploy
5. Sử dụng Network Policies để restrict traffic
6. Enable Pod Security Policies

## 📝 Updates và Rollbacks

```powershell
# Update image version
kubectl set image deployment/ibox-restservice \
  ibox-restservice=your-registry/ibox-restservice:v1.1.0 -n ibox

# Check rollout status
kubectl rollout status deployment/ibox-restservice -n ibox

# Rollback nếu có vấn đề
kubectl rollout undo deployment/ibox-restservice -n ibox

# Xem rollout history
kubectl rollout history deployment/ibox-restservice -n ibox
```

## 📞 Support

- Xem logs: `kubectl logs -f deployment/<service-name> -n ibox`
- Events: `kubectl get events -n ibox --sort-by='.lastTimestamp'`
- Resource usage: `kubectl top pods -n ibox`
