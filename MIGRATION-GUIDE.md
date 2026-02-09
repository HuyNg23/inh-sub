# Hướng dẫn Migration từ IIS sang Docker/K8s

## 🔄 So sánh IIS vs Docker/K8s

### Trên IIS (Hiện tại)
```
localhost:5001 → IBox.Client
localhost:5002 → IBox.LogService
localhost:5003 → IBox.RestService
localhost:5004 → IBox.Root.Client
localhost:5005 → IBox.Schedule
localhost:5006 → IBox.StoreService
```

### Trong Docker Compose (Test Local)
```
localhost:5001 → ibox-client (internal: 5000)
localhost:5002 → ibox-logservice (internal: 5000)
localhost:5003 → ibox-restservice (internal: 5000)
localhost:5004 → ibox-root-client (internal: 5000)
localhost:5005 → ibox-schedule (internal: 5000)
localhost:5006 → ibox-storeservice (internal: 5000)
```

### Trong Kubernetes (Production)
```
External (Ingress):
  api.ibox.com       → ibox-restservice:80
  root.ibox.com      → ibox-root-client:80
  schedule.ibox.com  → ibox-schedule:80
  store.ibox.com     → ibox-storeservice:80
  logs.ibox.com      → ibox-logservice:80

Internal (Service DNS):
  http://ibox-restservice:80
  http://ibox-root-client:80
  http://ibox-schedule:80
  http://ibox-storeservice:80
  http://ibox-logservice:80
```

## ⚠️ Những điểm quan trọng khi migration

### 1. Port Configuration

**IIS:** Mỗi app chạy trên port cố định
```
Service A gọi Service B: http://localhost:5003/api/endpoint
```

**Docker/K8s:** Service discovery thông qua DNS
```csharp
// BAD - Hard-coded ports (kiểu IIS)
var url = "http://localhost:5003/api/endpoint";

// GOOD - Environment variables (Docker/K8s)
var url = Configuration["ServiceUrls:RestService"] + "/api/endpoint";
// Giá trị: http://ibox-restservice:80/api/endpoint
```

### 2. Service-to-Service Communication

Nếu các services của bạn gọi lẫn nhau, cần update code:

**Option A: Sử dụng Configuration (Khuyến nghị)**
```csharp
// Program.cs or Startup.cs
public class ServiceUrlsConfig
{
    public string RestService { get; set; }
    public string LogService { get; set; }
    public string Schedule { get; set; }
}

// appsettings.json
{
  "ServiceUrls": {
    "RestService": "http://localhost:5003",  // IIS
    // hoặc "http://ibox-restservice:80",   // K8s
  }
}
```

**Option B: Environment Variables Override**
```csharp
var restServiceUrl = Environment.GetEnvironmentVariable("ServiceUrls__RestService") 
                     ?? "http://localhost:5003";
```

### 3. Database Connection

**Từ IIS:** Kết nối tới SQL Server qua localhost hoặc server name
```json
"ServerName": "localhost\\SQLEXPRESS"
```

**Trong Docker:** SQL Server ở bên ngoài container
```json
"ServerName": "host.docker.internal\\SQLEXPRESS"  // Docker Desktop
// hoặc
"ServerName": "your-sql-server.database.windows.net"  // Azure SQL
```

**Trong K8s:** Dùng external service hoặc connection string từ secret
```yaml
env:
  - name: IBoxConfig__Database__ServerName
    valueFrom:
      secretKeyRef:
        name: ibox-secrets
        key: Database__ServerName
```

### 4. File Storage & Volumes

**IIS:** Files lưu trực tiếp trên disk
```
C:\inetpub\wwwroot\IBox.LogService\Logs
C:\inetpub\wwwroot\IBox.LogService\DataBaseChatBot
```

**Docker:** Mount volumes
```yaml
volumes:
  - ./data/logs:/app/Logs
  - ./data/db:/app/DataBaseChatBot
```

**K8s:** PersistentVolumeClaims (đã config sẵn)
```yaml
volumeMounts:
  - name: logs
    mountPath: /app/Logs
volumes:
  - name: logs
    persistentVolumeClaim:
      claimName: ibox-logs-pvc
```

### 5. CORS Configuration

**IIS:** CORS cho localhost:3000, localhost:3001
```json
"CORSWhileList": ["http://localhost:3000", "http://localhost:3001"]
```

**Docker/K8s:** Thêm domain names
```json
"CORSWhileList": [
  "http://localhost:3000",
  "http://localhost:3001", 
  "https://your-frontend.com",
  "https://api.ibox.com"
]
```

## 🧪 Testing Strategy

### Bước 1: Test với Docker Compose (giống IIS)
```powershell
# Copy và edit .env file
copy .env.example .env
notepad .env

# Build và chạy tất cả services
docker-compose up --build

# Test từng service
curl http://localhost:5001  # Client
curl http://localhost:5002  # LogService
curl http://localhost:5003  # RestService
curl http://localhost:5004  # Root.Client
curl http://localhost:5005  # Schedule
curl http://localhost:5006  # StoreService
```

### Bước 2: Test inter-service communication
```powershell
# Vào container và test DNS resolution
docker-compose exec ibox-client ping ibox-restservice
docker-compose exec ibox-client curl http://ibox-restservice:5000/api/health
```

### Bước 3: Check logs
```powershell
# Xem logs tất cả services
docker-compose logs -f

# Xem logs 1 service cụ thể
docker-compose logs -f ibox-restservice
```

### Bước 4: Deploy lên K8s
```powershell
# Sau khi test OK với docker-compose
cd deploy/k8s
.\deploy-all.ps1 -Namespace "ibox" -Registry "your-registry" -Tag "v1.0.0"
```

## 🔧 Code Changes Required (Nếu có inter-service calls)

### Nếu code hiện tại có hardcoded URLs:

**Tìm và thay thế:**
```csharp
// TÌM patterns như:
"http://localhost:5001"
"http://localhost:5002"
"http://localhost:5003"
...

// THAY BẰNG:
Configuration["ServiceUrls:Client"]
Configuration["ServiceUrls:LogService"]
Configuration["ServiceUrls:RestService"]
```

### Add configuration class:
```csharp
// ServiceUrlsConfiguration.cs
public class ServiceUrlsConfiguration
{
    public string Client { get; set; } = "http://localhost:5001";
    public string LogService { get; set; } = "http://localhost:5002";
    public string RestService { get; set; } = "http://localhost:5003";
    public string RootClient { get; set; } = "http://localhost:5004";
    public string Schedule { get; set; } = "http://localhost:5005";
    public string StoreService { get; set; } = "http://localhost:5006";
}

// Program.cs
builder.Services.Configure<ServiceUrlsConfiguration>(
    builder.Configuration.GetSection("ServiceUrls"));
```

### Inject vào services:
```csharp
public class MyService
{
    private readonly IOptions<ServiceUrlsConfiguration> _serviceUrls;
    
    public MyService(IOptions<ServiceUrlsConfiguration> serviceUrls)
    {
        _serviceUrls = serviceUrls;
    }
    
    public async Task CallRestService()
    {
        var baseUrl = _serviceUrls.Value.RestService;
        var response = await _httpClient.GetAsync($"{baseUrl}/api/endpoint");
    }
}
```

## 📊 Port Mapping Reference

| Service | IIS Port | Docker External | Docker Internal | K8s Service | K8s Ingress |
|---------|----------|-----------------|-----------------|-------------|-------------|
| Client | 5001 | 5001 | 5000 | ibox-client:80 | client.ibox.com |
| LogService | 5002 | 5002 | 5000 | ibox-logservice:80 | logs.ibox.com |
| RestService | 5003 | 5003 | 5000 | ibox-restservice:80 | api.ibox.com |
| Root.Client | 5004 | 5004 | 5000 | ibox-root-client:80 | root.ibox.com |
| Schedule | 5005 | 5005 | 5000 | ibox-schedule:80 | schedule.ibox.com |
| StoreService | 5006 | 5006 | 5000 | ibox-storeservice:80 | store.ibox.com |

## ✅ Checklist Migration

- [ ] Copy `.env.example` thành `.env` và điền thông tin database
- [ ] Review code để tìm hardcoded localhost URLs
- [ ] Update code để dùng Configuration["ServiceUrls:..."]
- [ ] Test với `docker-compose up --build`
- [ ] Verify inter-service communication
- [ ] Check logs và database connections
- [ ] Update CORS whitelist nếu cần
- [ ] Build production images
- [ ] Update K8s secrets với connection strings thật
- [ ] Deploy lên K8s staging environment
- [ ] Run smoke tests
- [ ] Deploy lên production

## 🆘 Troubleshooting

### Container không kết nối được database
```powershell
# Check network connectivity from container
docker-compose exec ibox-restservice ping your-sql-server.database.windows.net

# Test SQL connection
docker-compose exec ibox-restservice dotnet tool install -g dotnet-sql-cache
```

### Service không gọi được nhau
```powershell
# Check DNS resolution trong Docker network
docker-compose exec ibox-client nslookup ibox-restservice
docker-compose exec ibox-client curl http://ibox-restservice:5000/

# Check environment variables
docker-compose exec ibox-client env | grep ServiceUrls
```

### Port already in use
```powershell
# Stop IIS services trước khi chạy Docker
Stop-Website -Name "IBox.Client"
Stop-Website -Name "IBox.RestService"
# ... hoặc stop tất cả IIS

# Hoặc thay đổi ports trong docker-compose.yml
ports:
  - "6001:5000"  # Thay vì 5001:5000
```

## 📞 Next Steps

1. **Test local với Docker Compose** để verify không có breaking changes
2. **Identify inter-service calls** trong code và update configuration
3. **Setup monitoring** (Prometheus + Grafana) cho K8s
4. **Setup CI/CD pipeline** tự động build và deploy
5. **Plan zero-downtime migration** strategy
