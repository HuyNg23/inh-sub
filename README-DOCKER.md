# ⚠️ LƯU Ý QUAN TRỌNG - Migration từ IIS sang Docker/K8s

## 📌 TÓM TẮT

Bạn đang chạy **6 services** trên IIS với 6 ports khác nhau. Khi containerize, có **NHỮNG ĐIỂM KHÁC BIỆT QUAN TRỌNG** cần lưu ý:

## 🔑 Những điểm cần lưu ý

### 1. ✅ MỖI CONTAINER CHẠY CÙNG INTERNAL PORT (5000)
- **IIS:** Mỗi app có port riêng (5001, 5002, 5003...)
- **Docker:** Mỗi container expose port 5000 bên trong, nhưng map ra external ports khác nhau
- **K8s:** Services expose port 80, routing qua DNS names

### 2. ⚠️ SERVICE-TO-SERVICE COMMUNICATION
Nếu các services gọi lẫn nhau, **PHẢI CẬP NHẬT CODE**:

**❌ Không dùng được (hardcoded ports):**
```csharp
var url = "http://localhost:5003/api/data";
```

**✅ Phải dùng configuration:**
```csharp
var url = Configuration["ServiceUrls:RestService"] + "/api/data";
// K8s: http://ibox-restservice:80/api/data
// Docker: http://ibox-restservice:5000/api/data
// IIS: http://localhost:5003/api/data
```

### 3. 🗄️ DATABASE CONNECTION
- **IIS:** `localhost\SQLEXPRESS` hoặc local server
- **Docker:** `host.docker.internal\SQLEXPRESS` (Windows) hoặc external server
- **K8s:** External SQL server (Azure SQL, AWS RDS, etc.)

### 4. 📁 FILE STORAGE
- **IIS:** Files trên local disk `C:\inetpub\wwwroot\...`
- **Docker:** Volumes mount vào `/app/Logs`, `/app/DataBaseChatBot`, etc.
- **K8s:** PersistentVolumeClaims (cloud storage)

### 5. 🔗 CORS CONFIGURATION
Phải thêm domain names mới vào CORS whitelist:
```json
"CORSWhileList": [
  "http://localhost:3000",           // Frontend local
  "https://your-frontend-app.com",   // Frontend production
  "http://ibox-restservice",         // K8s internal
  "https://api.ibox.com"             // External domain
]
```

## 🚀 QUY TRÌNH KHUYẾN NGHỊ

### Bước 1: Test với Docker Compose TRƯỚC
```powershell
# Setup
copy .env.example .env
notepad .env  # Điền DB credentials

# Chạy với ports giống IIS (5001-5006)
docker-compose up --build

# Test
curl http://localhost:5001  # IBox.Client
curl http://localhost:5002  # IBox.LogService
curl http://localhost:5003  # IBox.RestService
curl http://localhost:5004  # IBox.Root.Client
curl http://localhost:5005  # IBox.Schedule
curl http://localhost:5006  # IBox.StoreService
```

**MỤC ĐÍCH:** Phát hiện breaking changes TRƯỚC KHI deploy K8s

### Bước 2: Kiểm tra inter-service calls trong code
```powershell
# Tìm tất cả hardcoded URLs
Get-ChildItem -Recurse -Filter *.cs | Select-String "localhost:500"
Get-ChildItem -Recurse -Filter *.cs | Select-String "127.0.0.1"
```

Nếu tìm thấy → **PHẢI REFACTOR CODE** (xem MIGRATION-GUIDE.md)

### Bước 3: Test database connection
```powershell
# Trong docker-compose, verify DB connection works
docker-compose logs ibox-restservice | Select-String "database"
```

### Bước 4: Chỉ deploy K8s khi Docker Compose chạy OK
```powershell
cd deploy/k8s
.\deploy-all.ps1 -Namespace "ibox" -Registry "your-registry" -Tag "v1.0.0"
```

## 📊 PORT MAPPING REFERENCE

| Service | IIS (hiện tại) | Docker Compose | K8s Internal | K8s External |
|---------|----------------|----------------|--------------|--------------|
| **Client** | :5001 | :5001→5000 | ibox-client:80 | client.ibox.com |
| **LogService** | :5002 | :5002→5000 | ibox-logservice:80 | logs.ibox.com |
| **RestService** | :5003 | :5003→5000 | ibox-restservice:80 | api.ibox.com |
| **Root.Client** | :5004 | :5004→5000 | ibox-root-client:80 | root.ibox.com |
| **Schedule** | :5005 | :5005→5000 | ibox-schedule:80 | schedule.ibox.com |
| **StoreService** | :5006 | :5006→5000 | ibox-storeservice:80 | store.ibox.com |

## 📝 FILES ĐÃ TẠO

```
ibox-be/
├── docker-compose.yml          ← Test local (ports 5001-5006)
├── .env.example                ← Template environment variables
├── MIGRATION-GUIDE.md          ← Chi tiết migration (ĐỌC NÀY TRƯỚC!)
├── DOCKER-QUICKSTART.md        ← Quick commands
├── build-all-images.ps1        ← Build 6 Docker images
├── push-all-images.ps1         ← Push to registry
├── IBox.Client/Dockerfile      ← Dockerfile cho Client (mới thêm)
├── IBox.LogService/Dockerfile
├── IBox.RestService/Dockerfile
├── IBox.Root.Client/Dockerfile
├── IBox.Schedule/Dockerfile
├── IBox.StoreService/Dockerfile
└── deploy/
    ├── k8s/
    │   ├── configmap.yaml      ← Updated với ServiceUrls
    │   ├── secret.yaml
    │   ├── pvc.yaml
    │   ├── ibox-client.yaml    ← K8s manifest (mới thêm)
    │   ├── ibox-logservice.yaml
    │   ├── ibox-restservice.yaml
    │   ├── ibox-root-client.yaml
    │   ├── ibox-schedule.yaml
    │   ├── ibox-storeservice.yaml
    │   ├── ingress.yaml        ← Updated với client.ibox.com
    │   └── deploy-all.ps1
    └── README.md
```

## ⚡ QUICK START

```powershell
# 1. ĐỌC HƯỚNG DẪN CHI TIẾT
notepad MIGRATION-GUIDE.md

# 2. TEST LOCAL TRƯỚC
copy .env.example .env
notepad .env
docker-compose up --build

# 3. Verify tất cả services chạy OK
curl http://localhost:5001
curl http://localhost:5002
curl http://localhost:5003
curl http://localhost:5004
curl http://localhost:5005
curl http://localhost:5006

# 4. Check logs không có errors
docker-compose logs -f

# 5. Nếu OK, build production images
.\build-all-images.ps1 -Registry "your-registry.azurecr.io" -Tag "v1.0.0"

# 6. Push và deploy K8s
.\push-all-images.ps1 -Registry "your-registry.azurecr.io" -Tag "v1.0.0"
cd deploy\k8s
.\deploy-all.ps1 -Namespace "ibox" -Registry "your-registry.azurecr.io" -Tag "v1.0.0"
```

## 🆘 CÁC VẤN ĐỀ THƯỜNG GẶP

### ❌ "Connection refused" khi service gọi nhau
**Nguyên nhân:** Code có hardcoded `localhost:5003` thay vì dùng service name

**Giải pháp:** Refactor code dùng Configuration["ServiceUrls:..."]

### ❌ Database connection timeout
**Nguyên nhân:** Container không reach được SQL Server

**Giải pháp:** 
- Docker: Dùng `host.docker.internal` thay vì `localhost`
- K8s: Verify SQL server accessible từ cluster

### ❌ Port already in use
**Nguyên nhân:** IIS services vẫn đang chạy

**Giải pháp:** Stop IIS trước khi chạy Docker
```powershell
Stop-Website -Name "*IBox*"
```

## 📚 TÀI LIỆU CHI TIẾT

1. **[MIGRATION-GUIDE.md](MIGRATION-GUIDE.md)** - Đọc đầu tiên!
   - So sánh chi tiết IIS vs Docker vs K8s
   - Hướng dẫn refactor code nếu cần
   - Troubleshooting guide

2. **[DOCKER-QUICKSTART.md](DOCKER-QUICKSTART.md)** - Quick commands
   - Lệnh Docker Compose
   - Debug commands
   - Common issues

3. **[deploy/README.md](deploy/README.md)** - K8s deployment
   - Build & deploy to K8s
   - Configuration details
   - Scaling & monitoring

## ✅ CHECKLIST

- [ ] Đọc MIGRATION-GUIDE.md
- [ ] Copy .env.example → .env và config
- [ ] Tìm hardcoded URLs trong code (nếu có)
- [ ] Test với docker-compose up --build
- [ ] Verify tất cả 6 services chạy OK
- [ ] Check database connections
- [ ] Check inter-service communication (nếu có)
- [ ] Build production images
- [ ] Deploy lên K8s staging
- [ ] Run smoke tests
- [ ] Deploy production

---

**🎯 MỤC TIÊU:** Chuyển từ IIS sang Docker/K8s KHÔNG CÓ DOWNTIME, KHÔNG CÓ BREAKING CHANGES

**🚨 QUAN TRỌNG:** Test kỹ với Docker Compose trước khi deploy K8s!
