# PowerShell Script kiểm tra tiêu chí hoàn thành Ngày 1 (DoD)
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  HỆ THỐNG QUẢN LÝ SÂN CẦU LÔNG - KIỂM TRA TIÊU CHÍ NGÀY 1" -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Chạy verify qua ứng dụng .NET
Write-Host "`n[1/2] Đang kiểm tra qua DbHealthChecker (.NET 8):" -ForegroundColor Green
dotnet run --project BadmintonManager/BadmintonManager.csproj -- --verify-dod

# 2. Chạy xUnit tests
Write-Host "`n[2/2] Đang chạy kiểm thử tự động xUnit Tests:" -ForegroundColor Green
dotnet test --logger "console;verbosity=normal"
