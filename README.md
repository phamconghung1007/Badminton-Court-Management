# Badminton Court Management System (Hệ Thống Quản Lý Sân Cầu Lông)

## 🛠 Công nghệ sử dụng
- **Ngôn ngữ**: C# (.NET 8)
- **Giao diện**: WPF (XAML), MVVM Pattern với `CommunityToolkit.Mvvm`
- **UI & Biểu đồ**: `MaterialDesignThemes`, `LiveChartsCore.SkiaSharpView.WPF`
- **ORM**: Entity Framework Core 8.0 (Code First, Migrations)
- **Cơ sở dữ liệu**: Microsoft SQL Server
- **Bảo mật**: `BCrypt.Net-Next` (Băm mật khẩu)
- **Kiến trúc & DI**: `Microsoft.Extensions.Hosting` (Generic Host, Dependency Injection, `IDbContextFactory<AppDbContext>`)
- **Kiểm thử tự động**: xUnit, FluentAssertions, EF Core InMemory Database (26 unit tests 100% passed)

---

## 📅 Tiến độ thực hiện theo lộ trình 10 ngày

### ✅ Ngày 1: Khởi tạo Codebase & Thiết kế Cơ sở Dữ liệu
- [x] Thiết lập Solution & Project .NET 8 WPF MVVM chuẩn kiến trúc Clean Layered Architecture.
- [x] Thiết kế 11 thực thể (Entities), 6 Enums, Fluent API Configuration và Migrations.
- [x] Xây dựng `DbSeeder` tự động seed đầy đủ 3 cơ sở, 18 sân, 13 tài khoản người dùng, 4 khách hàng, 3 hạng thành viên, 6 quy tắc bảng giá, 10 cấu hình hệ thống, ~55 booking và 95 giao dịch thanh toán.
- [x] Xây dựng `DbHealthChecker` kiểm tra tự động tiêu chí DoD Ngày 1.

### ✅ Ngày 2: Đăng nhập, Phân quyền Động, Giao diện Shell & Style Chung
- [x] **Xác thực BCrypt & Bảo mật đăng nhập (Section 8.5)**:
  - Kiểm tra mật khẩu băm BCrypt.
  - Thông báo lỗi chung: `"Sai tên đăng nhập hoặc mật khẩu."` khi sai mật khẩu hoặc tên đăng nhập không tồn tại nhằm ngăn chặn tấn công vét cạn tài khoản (User Enumeration).
  - Khóa tài khoản vô hiệu hóa: Thông báo rõ ràng `"Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên."`.
- [x] **Đăng ký tài khoản Khách hàng (Register)**:
  - Cho phép khách hàng tự đăng ký tài khoản trực tuyến.
  - Tự động liên kết với hồ sơ khách hàng đã từng tạo tại quầy (dựa trên Số điện thoại) để giữ nguyên lịch sử và điểm tích lũy.
- [x] **Giao diện ShellWindow & Phân quyền động (Role-based Navigation)**:
  - 4 vai trò (Admin, Manager, Staff, Customer) đăng nhập thấy **4 bộ menu hoàn toàn khác biệt**.
  - Triển khai DataTemplate-based View switching thông qua `CurrentPageViewModel` và `INavigationService`.
  - Giới hạn dữ liệu Manager/Staff chặt chẽ theo chi nhánh (`BranchId`) tại tầng Service và ViewModel.
- [x] **Hệ thống Dashboard & Trang tính năng chuyên biệt**:
  - `AdminDashboardPage`: Bảng điều khiển quản trị tổng quan toàn hệ thống (Cơ sở, Sân, Booking, Doanh thu).
  - `ManagerDashboardPage`: Bảng điều khiển cơ sở, số sân, nhân sự cơ sở, doanh thu chi nhánh.
  - `StaffDashboardPage`: Bảng điều khiển ca trực lễ tân, sân đang hoạt động, lịch đặt hôm nay, booking chờ check-in/thanh toán.
  - `CustomerDashboardPage`: Cổng thông tin khách hàng, hạng hội viên (Đồng, Bạc, Vàng), số điểm tích lũy và % chiết khấu.
  - `UserProfilePage`: Xem thông tin tài khoản cá nhân, cơ sở trực thuộc và đổi mật khẩu an toàn.
  - `PlaceholderPageView`: Giao diện placeholder định tuyến chuẩn xác cho các tính năng các ngày tiếp theo theo đúng lộ trình tài liệu.
- [x] **Bộ UI Styles & Colors chuẩn Material Design**:
  - Phối màu vai trò trực quan: Admin (Tím thanh lịch `#4A154B`), Manager (Xanh hải quân `#1E3A8A`), Staff (Cam hổ phách `#D97706`), Customer (Xanh ngọc lục bảo `#059669`).
  - Nút bấm, Card thống kê, Form nhập liệu bo góc chuẩn Material 3.
- [x] **Đạt tiêu chí nghiệm thu Ngày 2 (DoD Ngày 2)**:
  - 26/26 unit tests tự động vượt qua (100% Passed) kiểm tra cả xác thực, phân quyền 4 menu độc lập, đổi mật khẩu và đăng ký tài khoản.

---

## 🏗 Cấu trúc dự án
```
BadmintonCourtManagement/
├── BadmintonCourtManagement.sln      # Solution mở bằng Visual Studio 2022
├── scripts/
│   ├── verify_day1_dod.sql           # Script SQL kiểm tra đạt tiêu chí Ngày 1 bằng SSMS
│   └── Verify-Database.ps1           # Script PowerShell kiểm thử tự động
├── BadmintonManager/
│   ├── App.xaml / App.xaml.cs         # Host, DI, Startup, Migration & Seeder
│   ├── appsettings.example.json       # File mẫu cấu hình chuỗi kết nối DB
│   ├── Models/
│   │   ├── Entities/                  # 11 Entity: Branch, Court, AppUser, Customer, MembershipTier,
│   │   │                              # PriceRule, Booking, FixedSchedule, Payment, PointTransaction, SystemSetting
│   │   └── Enums/                     # UserRole, BookingStatus, BookingType, DayType, PaymentMethod, PaymentType
│   ├── Data/
│   │   ├── AppDbContext.cs            # DbContext cấu hình qua Assembly Configurations, Business validation
│   │   ├── Configurations/            # Fluent API IEntityTypeConfiguration<T> cho từng bảng
│   │   ├── Migrations/                # EF Core Code First Migrations
│   │   ├── DbSeeder.cs                # Seed dữ liệu mẫu (3 cơ sở, 18 sân, tài khoản, giá, settings, ~55 bookings)
│   │   └── DbHealthChecker.cs         # Kiểm tra đạt chuẩn Definition of Done (DoD)
│   ├── Services/
│   │   ├── Interfaces/                # IAuthService, IBookingService, IPricingService, ...
│   │   └── (AuthService, BookingService, PricingService, PaymentService, MembershipService, ...)
│   ├── Common/
│   │   ├── Result.cs                  # Standard Result / Result<T> pattern
│   │   ├── CurrentUserContext.cs      # Thông tin phiên đăng nhập (Singleton)
│   │   ├── Exceptions/                # BusinessException, ForbiddenException
│   │   └── Constants.cs
│   ├── DTOs/                          # Data Transfer Objects cho Request / Response
│   ├── ViewModels/
│   │   ├── Base/                      # ViewModelBase (ObservableObject, IsBusy, RunAsync), PlaceholderPageViewModel
│   │   ├── Admin/                     # AdminDashboardViewModel
│   │   ├── Manager/                   # ManagerDashboardViewModel
│   │   ├── Staff/                     # StaffDashboardViewModel
│   │   ├── Customer/                  # CustomerDashboardViewModel
│   │   ├── Shared/                    # UserProfileViewModel
│   │   ├── LoginViewModel.cs          # Đăng nhập hệ thống
│   │   ├── RegisterViewModel.cs       # Đăng ký tài khoản khách hàng
│   │   └── ShellViewModel.cs          # Điều hướng menu động phân quyền theo 4 vai trò
│   ├── Views/
│   │   ├── LoginWindow.xaml           # Cửa sổ đăng nhập
│   │   ├── RegisterWindow.xaml        # Cửa sổ đăng ký khách hàng
│   │   ├── ShellWindow.xaml           # Khung giao diện chính với Navigation Bar và Viewport
│   │   └── Pages/                     # AdminDashboardPage, ManagerDashboardPage, StaffDashboardPage,
│   │                                  # CustomerDashboardPage, UserProfilePage, PlaceholderPageView
│   ├── Infrastructure/                # NavigationService, DialogService, InvoicePrinter
│   ├── Converters/                    # BoolToVisibility, MoneyConverter, StatusToBrushConverter
│   └── Resources/                     # Colors.xaml, Styles.xaml
└── tests/
    └── BadmintonManager.Tests/        # 26 bài kiểm thử tự động xUnit (DoD Ngày 1 & Ngày 2)
```

---

## 👥 Danh sách tài khoản mẫu để kiểm thử phân quyền (Mật khẩu mặc định: `123456`)

| Tên đăng nhập | Mật khẩu | Vai trò | Tên hiển thị | Chi nhánh trực thuộc | Ghi chú quyền hạn |
|---|---|---|---|---|---|
| `admin` | `123456` | **Admin** | Hệ Thống Admin | Toàn hệ thống | Quản trị 10 mục: Bảng điều khiển, Cơ sở, Sân, Người dùng, Giá, Hạng thẻ, Khách hàng, Lịch toàn hệ thống, Báo cáo doanh thu, Tài khoản |
| `manager1` | `123456` | **Manager** | Nguyễn Văn Quản Lý | Cơ sở Cầu Giấy | Quản lý cơ sở 1: Bảng điều khiển CS, Lịch sân CS, Duyệt booking, Sân CS, Nhân viên CS, Khách hàng, Báo cáo CS, Tài khoản |
| `manager2` | `123456` | **Manager** | Trần Thị Quản Lý | Cơ sở Đống Đa | Quản lý cơ sở 2 |
| `manager3` | `123456` | **Manager** | Lê Văn Quản Lý | Cơ sở Thanh Xuân | Quản lý cơ sở 3 |
| `staff1_1` | `123456` | **Staff** | Lê Thu Staff | Cơ sở Cầu Giấy | Nhân viên ca trực: Bảng điều khiển ca trực, Lịch sân ma trận, Đặt sân / Check-in tại quầy, Danh sách booking, Khách hàng, Tài khoản |
| `staff1_2` | `123456` | **Staff** | Hoàng Nam Staff | Cơ sở Cầu Giấy | Nhân viên ca trực cơ sở 1 |
| `customer1` | `123456` | **Customer** | Nguyễn Văn An | Khách hàng | Khách hàng hạng Vàng (1.600 điểm, chiết khấu 10%): Cổng thông tin, Tìm & Đặt sân trực tuyến, Lịch sử đặt, Điểm thưởng, Hồ sơ cá nhân |
| `customer2` | `123456` | **Customer** | Trần Thị Bình | Khách hàng | Khách hàng hạng Bạc (750 điểm, chiết khấu 5%) |
| `customer3` | `123456` | **Customer** | Lê Hoàng Cường | Khách hàng | Khách hàng hạng Đồng (120 điểm, chiết khấu 0%) |

---

## 🚀 Hướng dẫn cài đặt & kiểm thử

### 1. Cấu hình chuỗi kết nối
Sao chép file `appsettings.example.json` thành `appsettings.json` trong thư mục `BadmintonManager/` và cập nhật chuỗi kết nối tới SQL Server của bạn:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BadmintonDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 2. Chạy toàn bộ 26 bài kiểm thử tự động (Unit Tests)
```powershell
dotnet test
```

### 3. Khởi chạy ứng dụng
Mở solution `BadmintonCourtManagement.sln` trong Visual Studio 2022 và nhấn `F5`, hoặc chạy từ dòng lệnh:
```powershell
dotnet run --project BadmintonManager/BadmintonManager.csproj
```
Khi ứng dụng mở:
- Thử đăng nhập lần lượt các tài khoản `admin`, `manager1`, `staff1_1`, `customer1` (với mật khẩu `123456`) để xem 4 bộ menu và Dashboard giao diện khác nhau.
- Thử nhập sai mật khẩu để kiểm tra thông báo bảo mật: `"Sai tên đăng nhập hoặc mật khẩu."`.
- Thử tính năng "Đăng ký thành viên mới" trên giao diện Login.
- Thử tính năng "Đổi mật khẩu" tại trang Tài khoản cá nhân.
