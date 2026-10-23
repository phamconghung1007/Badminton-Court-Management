# Badminton Court Management System (Hệ Thống Quản Lý Sân Cầu Lông)

Dự án đồ án / CV ứng tuyển vị trí Thực tập sinh .NET / Desktop Developer.

## 🛠 Công nghệ sử dụng
- **Ngôn ngữ**: C# (.NET 8)
- **Giao diện**: WPF (XAML), MVVM Pattern với `CommunityToolkit.Mvvm`
- **UI & Biểu đồ**: `MaterialDesignThemes`, `LiveChartsCore.SkiaSharpView.WPF`
- **ORM**: Entity Framework Core 8.0 (Code First, Migrations)
- **Cơ sở dữ liệu**: Microsoft SQL Server
- **Bảo mật**: `BCrypt.Net-Next` (Băm mật khẩu)
- **Kiến trúc & DI**: `Microsoft.Extensions.Hosting` (Generic Host, Dependency Injection, `IDbContextFactory<AppDbContext>`)

---

## 🏗 Cấu trúc dự án
```
BadmintonManager/
├── App.xaml / App.xaml.cs            # Host, DI, Startup, Migration & Seeder
├── appsettings.example.json         # File mẫu cấu hình chuỗi kết nối DB
├── Models/
│   ├── Entities/                    # Branch, Court, AppUser, Customer, MembershipTier,
│   │                                # PriceRule, Booking, FixedSchedule, Payment, PointTransaction, SystemSetting
│   └── Enums/                       # UserRole, BookingStatus, BookingType, DayType, PaymentMethod, PaymentType
├── Data/
│   ├── AppDbContext.cs              # DbContext cấu hình qua Assembly Configurations
│   ├── Configurations/              # Fluent API IEntityTypeConfiguration<T> cho từng bảng
│   ├── Migrations/                  # EF Core Code First Migrations
│   └── DbSeeder.cs                  # Seed dữ liệu mẫu (3 cơ sở, 18 sân, tài khoản, giá, settings, ~50 bookings)
├── Services/
│   ├── Interfaces/                  # IAuthService, IBookingService, IPricingService, ...
│   └── (AuthService, BookingService, PricingService, PaymentService, MembershipService, ...)
├── Common/
│   ├── Result.cs                    # Standard Result / Result<T> pattern
│   ├── CurrentUserContext.cs        # Thông tin phiên đăng nhập (Singleton)
│   ├── Exceptions/                  # BusinessException, ForbiddenException
│   └── Constants.cs
├── DTOs/                            # Data Transfer Objects cho Request / Response
├── ViewModels/
│   ├── Base/ViewModelBase.cs        # ObservableObject với IsBusy và try-catch RunAsync helper
│   ├── LoginViewModel.cs            # Xử lý đăng nhập
│   └── ShellViewModel.cs            # Menu & điều hướng phân quyền động theo vai trò
├── Views/
│   ├── LoginWindow.xaml             # Màn hình đăng nhập Material Design
│   └── ShellWindow.xaml             # Giao diện chính phân quyền động theo 4 vai trò
├── Infrastructure/                  # NavigationService, DialogService, InvoicePrinter
├── Converters/                      # BoolToVisibility, MoneyConverter, StatusToBrushConverter
└── Resources/                       # Colors.xaml, Styles.xaml
```

---

## 👥 Danh sách tài khoản mẫu (Mật khẩu mặc định: `123456`)

| Tên đăng nhập | Vai trò | Tên hiển thị | Chi nhánh trực thuộc | Ghi chú |
|---|---|---|---|---|
| `admin` | Admin | Hệ Thống Admin | Toàn hệ thống | Quản trị toàn quyền |
| `manager1` | Manager | Nguyễn Văn Quản Lý | Cơ sở Cầu Giấy | Quản lý cơ sở 1 |
| `manager2` | Manager | Trần Thị Quản Lý | Cơ sở Đống Đa | Quản lý cơ sở 2 |
| `manager3` | Manager | Lê Văn Quản Lý | Cơ sở Thanh Xuân | Quản lý cơ sở 3 |
| `staff1_1` | Staff | Lê Thu Staff | Cơ sở Cầu Giấy | Nhân viên lễ tân cơ sở 1 |
| `staff1_2` | Staff | Hoàng Nam Staff | Cơ sở Cầu Giấy | Nhân viên lễ tân cơ sở 1 |
| `staff2_1` | Staff | Vũ Mai Staff | Cơ sở Đống Đa | Nhân viên lễ tân cơ sở 2 |
| `customer1` | Customer | Nguyễn Văn An | Khách hàng hệ thống | Hạng Vàng (1.600 điểm, giảm 10%) |
| `customer2` | Customer | Trần Thị Bình | Khách hàng hệ thống | Hạng Bạc (750 điểm, giảm 5%) |
| `customer3` | Customer | Lê Hoàng Cường | Khách hàng hệ thống | Hạng Đồng (120 điểm, giảm 0%) |

---

## 🚀 Hướng dẫn cài đặt & chạy ứng dụng

### 1. Yêu cầu hệ thống
- .NET 8 SDK
- Microsoft SQL Server (LocalDB, Express hoặc Developer)
- Visual Studio 2022 (v17.8 trở lên có cài đặt workload *.NET Desktop Development*)

### 2. Cấu hình chuỗi kết nối
Sao chép file `appsettings.example.json` thành `appsettings.json` trong thư mục `BadmintonManager/` và cập nhật chuỗi kết nối:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BadmintonDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 3. Tạo cơ sở dữ liệu và seed dữ liệu mẫu
Cơ sở dữ liệu sẽ tự động migrate và seed dữ liệu khi khởi động ứng dụng lần đầu. Bạn cũng có thể chủ động cập nhật bằng lệnh CLI:
```powershell
dotnet ef database update --project BadmintonManager/BadmintonManager.csproj
dotnet run --project BadmintonManager/BadmintonManager.csproj -- --seed-only
```

### 4. Khởi chạy ứng dụng
Mở solution `BadmintonCourtManagement.sln` bằng Visual Studio và nhấn `F5`, hoặc chạy từ dòng lệnh:
```powershell
dotnet run --project BadmintonManager/BadmintonManager.csproj
```

---

## 📐 Các quy tắc nghiệp vụ nổi bật (Business Rules)
- **Chống trùng lịch và quy tắc khoảng trống (BR-03 & BR-04)**: Kiểm tra không cho phép khoảng trống đúng 30 phút giữa các ca đặt sân để tránh lãng phí tài nguyên sân.
- **Transaction an toàn (BR-06)**: Kiểm tra và lưu booking trong Database Transaction (IsolationLevel Serializable) kết hợp concurrency token `RowVersion` nhằm chống race condition khi nhiều người cùng đặt 1 sân.
- **Tính giá theo từng block 30 phút (BR-07 & BR-08)**: Hỗ trợ booking vắt qua nhiều khung giá khác nhau (giờ vàng / giờ thường, thứ trong tuần / cuối tuần).
- **Phân quyền và bảo mật tại tầng Service**: Mọi thao tác truy vấn dữ liệu của Manager/Staff đều được tự động lọc theo `BranchId` của tài khoản đang đăng nhập trong `CurrentUserContext`.
