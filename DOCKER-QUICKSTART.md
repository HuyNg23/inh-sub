# Quick Start Scripts cho Docker Development

## Test với Docker Compose (Giống IIS Setup)

### 1. Setup môi trường
```powershell
# Copy environment file
Copy-Item .env.example .env

# Edit với thông tin database thật
notepad .env
```

### 2. Build và chạy tất cả services
```powershell
# Build và start
docker-compose up --build -d

# Xem logs
docker-compose logs -f
```

### 3. Test các services
```powershell
# Test từng service (giống ports trên IIS)
curl http://localhost:5001  # IBox.Client
curl http://localhost:5002  # IBox.LogService
curl http://localhost:5003  # IBox.RestService
curl http://localhost:5004  # IBox.Root.Client
curl http://localhost:5005  # IBox.Schedule
curl http://localhost:5006  # IBox.StoreService
```

### 4. Stop services
```powershell
docker-compose down

# Stop và xóa volumes
docker-compose down -v
```

## Các lệnh hữu ích

### Xem logs từng service
```powershell
docker-compose logs -f ibox-restservice
docker-compose logs -f ibox-logservice
```

### Restart một service
```powershell
docker-compose restart ibox-restservice
```

### Rebuild một service sau khi sửa code
```powershell
docker-compose up --build -d ibox-restservice
```

### Vào container để debug
```powershell
docker-compose exec ibox-restservice bash
# Hoặc với PowerShell
docker-compose exec ibox-restservice pwsh
```

### Check resource usage
```powershell
docker stats
```

## Migration từ IIS

### Chạy song song IIS và Docker
Nếu muốn test song song:

**Option 1:** Stop IIS services trước
```powershell
Stop-Website -Name "*IBox*"
```

**Option 2:** Thay ports trong docker-compose.yml
```yaml
ports:
  - "6001:5000"  # Thay vì 5001
  - "6002:5000"  # Thay vì 5002
  ...
```

## Common Issues

### Port already in use
```powershell
# Kiểm tra process đang dùng port
netstat -ano | findstr :5001

# Stop IIS hoặc process đó
Stop-Process -Id <PID>
```

### Database connection failed
```powershell
# Test connectivity từ container
docker-compose exec ibox-restservice ping your-db-server

# Check connection string trong .env
cat .env
```

### Volume permission issues
```powershell
# Xóa volumes và recreate
docker-compose down -v
docker-compose up --build -d
```
