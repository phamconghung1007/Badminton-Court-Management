# ==============================================================================
# HE THONG QUAN LY SAN CAU LONG (BADMINTON COURT MANAGEMENT)
# SCRIPT TU DONG KIEM TRA MAY MOI (FRESH-MACHINE SETUP VERIFICATION - DAY 10 DoD)
# Tieu chuan: Chay sach tu may moi theo README; checklist muc 10 dat
# ==============================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "==========================================================================================" -ForegroundColor Cyan
Write-Host "          KIEM CHUNG CAI DAT MAY MOI VA NGHIEM THU TOAN DIEN (DAY 10 FINAL DoD)          " -ForegroundColor Yellow
Write-Host "==========================================================================================" -ForegroundColor Cyan
Write-Host ""

$startTime = Get-Date

# ------------------------------------------------------------------------------
# BUOC 1: KIEM TRA MOI TRUONG .NET SDK
# ------------------------------------------------------------------------------
Write-Host "[1/4] Kiem tra moi truong thuc thi .NET SDK..." -ForegroundColor Cyan
try {
    $dotnetVer = dotnet --version
    Write-Host "  -> .NET SDK hien tai: $dotnetVer (Yeu cau: >= 8.0.0)" -ForegroundColor Green
} catch {
    Write-Host "  [LOI] Khong tim thay dotnet CLI. Vui long cai dat .NET 8 SDK!" -ForegroundColor Red
    exit 1
}

# ------------------------------------------------------------------------------
# BUOC 2: BIEN DICH DU AN (BUILD SOLUTION)
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[2/4] Khoi phuc dependencies va bien dich ma nguon..." -ForegroundColor Cyan
dotnet build BadmintonCourtManagement.sln --configuration Debug --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "  [LOI] Bien dich that bai!" -ForegroundColor Red
    exit 1
}
Write-Host "  -> Bien dich thanh cong 0 loi!" -ForegroundColor Green

# ------------------------------------------------------------------------------
# BUOC 3: KIEM THU TU DONG HOA (209+ TESTS)
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[3/4] Chay toan bo bo kiem thu tu dong hoa (xUnit unit and matrix tests)..." -ForegroundColor Cyan
dotnet test tests/BadmintonManager.Tests/BadmintonManager.Tests.csproj --no-build --nologo --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "  [LOI] Co test that bai!" -ForegroundColor Red
    exit 1
}
Write-Host "  -> 100% Kiem thu tu dong da DAT XANH!" -ForegroundColor Green

# ------------------------------------------------------------------------------
# BUOC 4: KIEM CHUNG CO SO DU LIEU VA SEED DATA (DoD DAY 10)
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[4/4] Khoi tao migration, seed du lieu mau va kiem chung DbHealthChecker..." -ForegroundColor Cyan
dotnet run --project BadmintonManager/BadmintonManager.csproj --no-build -- --verify-day10
if ($LASTEXITCODE -ne 0) {
    Write-Host "  [LOI] Kiem tra CSDL khong dat yeu cau DoD!" -ForegroundColor Red
    exit 1
}

# ------------------------------------------------------------------------------
# TONG KET NGHIEM THU
# ------------------------------------------------------------------------------
$duration = (Get-Date) - $startTime
Write-Host ""
Write-Host "==========================================================================================" -ForegroundColor Green
Write-Host "          TAT CA TIEU CHI NGHIEM THU DAY 10 DA DAT CHUAN HOAN TOAN!                       " -ForegroundColor Green
Write-Host "  Thoi gian thuc thi: $([math]::Round($duration.TotalSeconds, 1)) giay" -ForegroundColor White
Write-Host "  He thong san sang van hanh thuc te va bao ve do an!                                    " -ForegroundColor Green
Write-Host "==========================================================================================" -ForegroundColor Green
Write-Host ""
