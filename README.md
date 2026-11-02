# Badminton Court Management System (Hệ Thống Quản Lý Sân Cầu Lông)

## 🛠 Công nghệ sử dụng
- **Ngôn ngữ**: C# (.NET 8)
- **Giao diện**: WPF (XAML), MVVM Pattern với `CommunityToolkit.Mvvm`
- **UI & Biểu đồ**: `MaterialDesignThemes`, `LiveChartsCore.SkiaSharpView.WPF`
- **ORM**: Entity Framework Core 8.0 (Code First, Migrations)
- **Cơ sở dữ liệu**: Microsoft SQL Server
- **Bảo mật**: `BCrypt.Net-Next` (Băm mật khẩu)
- **Kiến trúc & DI**: `Microsoft.Extensions.Hosting` (Generic Host, Dependency Injection, `IDbContextFactory<AppDbContext>`)
- **Kiểm thử tự động**: xUnit, FluentAssertions, EF Core InMemory Database (136 unit tests 100% passed)

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

### ✅ Ngày 3: CRUD Cơ sở, Sân, Tài khoản, Cấu hình giá & Hạng hội viên, Validate BR-10
- [x] **Quản lý Cơ sở (Branch Management - Admin)**:
  - CRUD đầy đủ cho chi nhánh: Thêm mới, chỉnh sửa thông tin liên hệ (Tên, Địa chỉ, SĐT), bật/tắt kích hoạt (Soft delete).
  - Kiểm tra chống trùng tên chi nhánh trong toàn hệ thống.
  - Giao diện `AdminBranchesPage` với bảng hiển thị số sân trực thuộc, ngày thành lập và form trượt (drawer) thêm/sửa trực quan.
- [x] **Quản lý Sân (Court Management - Admin & Manager Scoping)**:
  - **Admin**: Quản lý toàn bộ danh sách sân của tất cả các chi nhánh, lọc sân theo cơ sở qua ComboBox, thêm sân mới cho bất kỳ cơ sở nào.
  - **Manager**: Giao diện `ManagerCourtsPage` bị khoá cứng theo cơ sở trực thuộc (`_currentUser.BranchId`), chỉ thấy và thao tác với sân của cơ sở mình.
  - **Ràng buộc kỹ thuật bắt buộc (Section 2 & 4)**: Giới hạn cơ sở được thực thi tại tầng **Service** (`BranchCourtService`), ném `ForbiddenException` ngay lập tức nếu Manager cố truy cập hoặc chỉnh sửa sân của cơ sở khác.
  - **Ràng buộc `Unique(BranchId, Name)`**: Chặn trùng tên sân trong cùng một chi nhánh, nhưng cho phép trùng tên sân ở các chi nhánh khác nhau (VD: Sân 1 tại Cầu Giấy và Sân 1 tại Đống Đa).
- [x] **Quản lý Tài khoản & Phân cấp nhân sự (User Management)**:
  - **Admin** (`AdminUsersPage`): Toàn quyền xem, lọc tài khoản theo Vai trò và Chi nhánh, tạo tài khoản mới (Admin, Manager, Staff), cập nhật thông tin và cấp lại mật khẩu.
  - **Manager** (`ManagerStaffPage`): Chỉ quản lý danh sách **Nhân viên (Staff)** trực thuộc chi nhánh của mình. Bị chặn ở Service nếu cố tạo Admin, Manager hoặc thao tác với nhân sự cơ sở khác.
  - **Quy tắc phân quyền tài khoản (Section 2 & 4)**:
    - Tài khoản Quản lý (Manager) và Nhân viên (Staff) **bắt buộc phải gắn với một chi nhánh cụ thể**.
    - Tài khoản Quản trị viên (Admin) **không được gắn với cơ sở cụ thể (`BranchId = null`)**.
    - Tên đăng nhập là duy nhất toàn hệ thống, mật khẩu được mã hóa an toàn bằng BCrypt.
    - Quản trị viên không thể tự khóa tài khoản của chính mình nhằm đảm bảo an toàn vận hành hệ thống.
- [x] **Cấu hình Bảng giá & Ma trận khung giờ (PriceRule Management - Admin)**:
  - Giao diện `AdminPricingPage` cho phép thiết lập đơn giá theo từng khung giờ và loại ngày (Ngày thường: Thứ 2 - Thứ 6 / Cuối tuần: Thứ 7 - Chủ Nhật).
  - Form trượt thêm/sửa khung giá với bộ chọn giờ chuẩn hóa theo bội số 30 phút (`06:00` đến `24:00`).
  - Nút bật/tắt áp dụng và xoá khung giờ linh hoạt.
- [x] **Thuật toán Kiểm định Chuẩn BR-10 (BR-10 Coverage & Anti-Overlap Validation)**:
  - **Quy tắc BR-10**: Các khung giờ của cùng loại ngày không được chồng nhau và phải phủ kín liên tục `06:00 – 24:00` (360 đến 1440 phút).
  - **Chống trùng lấn (Anti-overlap)**: Kiểm tra ngay khi thêm hoặc cập nhật khung giá đang áp dụng (`r.StartMinute < request.EndMinute && request.StartMinute < r.EndMinute`), ném lỗi nghiệp vụ nêu rõ khung giờ bị xung đột.
  - **Kiểm định độ phủ kín (`ValidateCoverageAsync`)**: Tự động phát hiện thiếu giờ mở cửa (bắt đầu sau 06:00), thiếu giờ đóng cửa (kết thúc trước 24:00) hoặc khoảng trống ở giữa các khung giờ.
  - **Thẻ trạng thái BR-10 trực quan thời gian thực**: Hiển thị badge xanh "Đạt chuẩn BR-10" hoặc cảnh báo đỏ chi tiết các khoảng giờ còn thiếu cho cả Ngày thường và Cuối tuần.
- [x] **Quản lý Chính sách Hạng hội viên & Tích điểm (Membership Tier Policy - Admin)**:
  - Giao diện `AdminTiersPage` cấu hình các hạng thẻ (Đồng, Bạc, Vàng,...), ngưỡng điểm tích lũy tối thiểu để tự động nâng hạng (BR-16) và mức chiết khấu giảm giá trực tiếp (BR-09).
  - Thống kê số lượng hội viên hiện tại đang thuộc từng hạng thẻ.
  - Chặn xóa hạng thẻ nếu hiện tại đang có khách hàng liên kết để bảo toàn dữ liệu khách hàng.
- [x] **Đạt tiêu chí nghiệm thu Ngày 3 (DoD Ngày 3)**:
  - Admin thêm sân/đổi giá được; Manager chỉ thấy cơ sở mình.
  - Toàn bộ **66/66 unit tests tự động vượt qua (100% Passed)**, bao gồm 40 bài test mới cho CRUD Chi nhánh, Sân, Tài khoản, Phân quyền chi nhánh và Validate BR-10.

### ✅ Ngày 4: Pricing Engine, Booking Service, Toàn bộ Validation BR-01..06, 09, 11 & Unit Test Suite
- [x] **Thuật toán tính giá theo ô 30 phút & Chiết khấu đa tầng (BR-07, BR-08, BR-09, BR-11 - PricingService)**:
  - Tách khoảng thời gian đặt sân thành các block 30 phút riêng biệt (`_pricingService.CalculatePriceAsync`).
  - Tự động nhận diện thứ 7 / Chủ nhật để áp giá Cuối tuần (`DayType.Weekend`) theo chuẩn **BR-08**.
  - Ánh xạ chính xác bảng giá của từng ô 30 phút theo ma trận `PriceRule` tương ứng (**BR-07**: tính toán hoàn hảo khi đặt vắt qua 2 hoặc nhiều khung giá khác nhau).
  - Tra cứu hạng thẻ khách hàng (Đồng, Bạc, Vàng...) và tự động áp dụng % chiết khấu hội viên (**BR-09**: giảm trực tiếp trên tổng tiền giờ trước cọc).
  - Tính tiền đặt cọc theo tỷ lệ cấu hình hệ thống (`DepositPercent`, mặc định 30%) và tự động làm tròn lên bội số 1.000 VNĐ gần nhất (**BR-11**: `Math.Ceiling(amount / 1000m) * 1000m`).
- [x] **Nghiệp vụ Đặt sân toàn diện & Chống trùng lịch (BR-01..06, A7 - BookingService)**:
  - **BR-01 (Khung giờ & Mốc :00/:30)**: Giờ hoạt động 06:00 - 24:00 (chuẩn hóa mốc 24:00 tức 00:00 ngày tiếp theo), bắt buộc mốc bắt đầu và kết thúc tại phút :00 hoặc :30, giây = 0.
  - **BR-02 (Thời lượng)**: Đọc động cấu hình `MinDurationMinutes` (60) và `MaxDurationMinutes` (240) từ `ISettingService`, từ chối nếu thời lượng < 60 phút hoặc > 4 giờ (240 phút).
  - **BR-03 (Chống trùng lịch / Overlap)**: Phát hiện và chặn tức thời mọi dạng xung đột lịch trên cùng sân (`StartTime < existing.EndTime && existing.StartTime < EndTime`). Tự động bỏ qua các đơn ở trạng thái `Cancelled`.
  - **BR-04 (Quy tắc khoảng trống 30 phút - A4)**: Kiểm tra khoảng trống với lịch đặt liền trước (hoặc mốc mở cửa 06:00) và liền sau (hoặc mốc đóng cửa 24:00) trên cùng sân trong ngày. Nếu để hở đúng 30 phút, từ chối với thông báo hướng dẫn cụ thể: `"Quy tắc khoảng trống: Không được để lại khoảng trống đúng 30 phút giữa các lịch đặt (Hãy đặt sát giờ hoặc chừa từ 1 giờ trở lên)."`.
  - **BR-05 (Chống đặt trong quá khứ & Walk-in)**: Chặn đặt lùi thời gian cho đơn đặt trước. Cho phép khách vãng lai (Walk-in) đặt bắt đầu từ khung 30 phút hiện tại.
  - **BR-06 & Quản lý giao dịch an toàn (Concurrency & Isolation)**: Giao dịch an toàn hỗ trợ `IsolationLevel.Serializable` trên relational database, tự động fallback `NoOpTransaction` trên InMemory test runner; xử lý bắt lỗi `DbUpdateConcurrencyException`.
  - **A7 (Trạng thái ban đầu & Sinh mã)**: Khách hàng tự đặt trực tuyến: `Pending`; Nhân viên tạo: `Confirmed`; Khách vãng lai: `CheckedIn`. Sinh mã đơn chuẩn hóa `BK{yyyyMMdd}-{seq:D3}` duy nhất.
  - **Phân quyền cơ sở**: Quản lý / Nhân viên chỉ có thể tạo hoặc sửa đơn đặt trên sân thuộc chi nhánh của mình; Khách hàng tự động gán theo hồ sơ cá nhân.
  - **BR-14 (Hủy lịch & Hoàn cọc)**: Hủy trước giờ chơi $\ge 24$ giờ: tự động tạo phiếu hoàn cọc (`PaymentType.Refund`); Hủy dưới 24 giờ: hủy đơn và mất cọc theo quy định.
- [x] **Đạt tiêu chí nghiệm thu Ngày 4 (DoD Ngày 4)**:
  - Toàn bộ các test: **trùng giờ**, **khoảng trống 30 phút**, **vắt qua 2 khung giá**, **cuối tuần**, **giảm hạng hội viên**, **làm tròn cọc** đều xanh.
  - Toàn bộ **106/106 unit tests tự động vượt qua (100% Passed)** với 40 bài test mới bổ sung cho Day 4.

### ✅ Ngày 5: Màn hình Lịch sân (Schedule Grid) + Form đặt sân + Danh sách booking
- [x] **Mô hình DTO ma trận & Thuật toán sinh lưới lịch 36 slots $\times$ N sân (`ScheduleMatrixHelper`)**:
  - Chia ngày thành 36 time slots cố định 30 phút (`06:00` đến `24:00`, Slot 0..35 theo phụ lục A1, A2).
  - Thuật toán `BuildBookingBlock` ánh xạ thời gian thực của booking sang vị trí ma trận: `RowIndex` (slot bắt đầu), `RowSpan` (số block 30 phút), `ColumnIndex` (sân tương ứng).
  - Ánh xạ bảng màu trạng thái chuẩn UI/UX: Chờ duyệt (`#F59E0B` - Vàng/Cam), Đã duyệt (`#2563EB` - Xanh dương), Đang chơi (`#10B981` - Xanh lá), Hoàn thành (`#6B7280` - Xám), Vắng mặt (`#EF4444` - Đỏ).
  - Thuật toán `BuildEmptySlots` quét toàn bộ ô trống trên từng sân để người dùng click đặt sân trực tiếp.
  - Tự động tính toán toạ độ pixel (`X`, `Y`, `Width`, `Height`) để hiển thị trực quan và mượt mà trên Canvas mà không làm giảm hiệu năng WPF.
- [x] **Màn hình Lịch sân chi nhánh (`SchedulePage` & `ScheduleViewModel`)**:
  - Bộ điều hướng ngày linh hoạt: Nút lùi 1 ngày (◀), Hôm nay, Nút tiến 1 ngày (▶), bộ chọn `DatePicker`.
  - Bộ chọn chi nhánh động cho Admin (`CanSelectBranch = true`), tự động khóa cứng chi nhánh với Manager và Staff.
  - Vùng lưới lịch cuộn mượt mà hai chiều (`ScrollViewer`): Cột giờ 36 khung thời gian bên trái, hàng header hiển thị các sân thể thao, lớp nền ô trống có hiệu ứng hover và dấu `+`, lớp khối booking nổi bật với bo góc, thông tin mã đơn, tên khách, SĐT và số tiền.
  - Thanh chú thích màu trạng thái (Legend bar) giúp nhân viên nhận diện tức thì tình trạng sân bãi.
- [x] **Form Đặt Sân Tích Hợp (Create Booking Drawer) & Live Pricing Preview**:
  - Tự động điền sân và giờ chơi khi click vào bất kỳ ô trống nào trên lịch sân.
  - Hỗ trợ linh hoạt 2 luồng khách hàng: Khách vãng lai (nhập tên, SĐT) và Hội viên (tìm kiếm theo tên/SĐT, tự động lấy thông tin hạng thẻ và chiết khấu).
  - Khung xem trước giá thời gian thực (**Live Pricing Preview**): Tự động tính toán đơn giá gốc theo khung giờ, trừ chiết khấu hội viên (Đồng 0%, Bạc 5%, Vàng 10%), tổng tiền sân và số tiền đặt cọc tối thiểu 30% làm tròn theo quy chuẩn BR-11.
  - Bắt lỗi nghiệp vụ tiếng Việt chi tiết khi vi phạm: trùng giờ (BR-03), vi phạm khoảng trống 30 phút (BR-04), thời lượng không hợp lệ (BR-02), hoặc đặt trong quá khứ (BR-05).
  - Tự động nạp lại lịch và hiển thị khối booking ngay tại ô vừa đặt khi đặt sân thành công.
- [x] **Drawer Chi Tiết Booking & Thao Tác Nghiệp Vụ (Booking Details Drawer)**:
  - Hiển thị đầy đủ thông tin: Mã đơn, Sân & Cơ sở, Khung giờ, Khách hàng, Tổng tiền, Tiền cọc, Đã thanh toán.
  - Các nút hành động nghiệp vụ nhanh cho lễ tân:
    - **Duyệt đơn** (`Confirm`): Chuyển đơn từ `Pending` sang `Confirmed`.
    - **Check-in vào sân** (`Check-in`): Chuyển đơn từ `Confirmed` sang `CheckedIn` khi khách tới nhận sân.
    - **Hủy đặt sân** (`Cancel`): Xác nhận hủy đơn, tự động tính toán hoàn cọc nếu hủy trước 24 giờ.
- [x] **Màn hình Danh Sách Booking (`BookingListPage` & `BookingListViewModel`)**:
  - KPI Cards thống kê: Tổng số đơn, Chờ duyệt, Đã duyệt, Đang chơi, Doanh số hiện tại.
  - Bộ lọc đa năng: Lọc theo Ngày, Lọc theo Trạng thái (Tất cả, Pending, Confirmed, CheckedIn, Completed, Cancelled, NoShow), Lọc theo Sân đấu, và Ô tìm kiếm theo từ khóa.
  - Thao tác trực tiếp trên từng dòng danh sách: Duyệt đơn, Check-in, Hủy đơn với thông báo xác nhận tiếng Việt.
- [x] **Đạt tiêu chí nghiệm thu Ngày 5 (DoD Ngày 5)**:
  - *"Staff đặt được trên lịch, thấy khối booking đúng chỗ; đặt trùng bị chặn với thông báo tiếng Việt"*.
  - Toàn bộ **126/126 unit tests tự động vượt qua (100% Passed)** với 20 bài test mới bao phủ `ScheduleMatrixHelper`, `ScheduleViewModel` (DoD workflow, overlap blocking, live pricing preview), `BookingListViewModel` và điều hướng Shell.

### ✅ Ngày 6: Quy trình Booking đầy đủ, Check-in, Thanh toán, Hóa đơn & Nâng hạng hội viên
- [x] **Dịch vụ Thanh toán & Phân loại giao dịch (PaymentService - BR-11, BR-12, BR-13, BR-14)**:
  - Hỗ trợ thanh toán đa phương thức: Tiền mặt (`Cash`) và Chuyển khoản (`BankTransfer`).
  - Thu cọc tối thiểu 30% làm tròn theo bội số 1.000 VNĐ (BR-11), tự động cập nhật trạng thái đơn từ `Pending` sang `Confirmed`.
  - Thu nốt phần tiền còn thiếu khi khách trả sân, tự động chuyển đơn sang `Completed` khi thanh toán đủ (BR-13).
  - Tự động tạo bản ghi hoàn cọc (`Refund`) khi hủy đơn trước giờ chơi $\ge 24$ giờ (BR-14).
  - Bảo mật chi nhánh: Kiểm tra quyền hạn cơ sở tại tầng Service, ném `ForbiddenException` nếu thanh toán trái cơ sở.
- [x] **Tích điểm hội viên & Tự động nâng hạng thẻ (BR-15, BR-16)**:
  - Tích điểm tức thì khi hoàn tất đơn: $\lfloor \text{FinalAmount} / 10.000 \rfloor$ điểm.
  - Ghi vết biến động điểm minh bạch vào bảng `PointTransaction`.
  - Tự động so sánh điểm tích lũy và nâng hạng thẻ hội viên (Đồng $\to$ Bạc $\to$ Vàng...) ngay trong Transaction.
- [x] **Hệ thống In & Xem trước Hóa đơn (FlowDocument + PrintDialog - InvoiceService & InvoicePrinter)**:
  - Sinh mã hóa đơn chuẩn hóa `INV-yyyyMMdd-seq`.
  - Mẫu hóa đơn `FlowDocument` chuyên nghiệp với đầy đủ thông tin cơ sở, khách hàng, chi tiết giờ chơi, đơn giá, chiết khấu hạng thẻ, tiền cọc và dư nợ.
  - Hộp thoại xem trước `InvoicePreviewDialog` tích hợp `FlowDocumentScrollViewer` và nút in trực tiếp ra máy in hóa đơn.
- [x] **Tích hợp UI Check-out & Điều chỉnh Booking**:
  - Hộp thoại `PaymentDialog`: Thu cọc hoặc thu nốt, kiểm soát số tiền, tùy chọn tự động mở in hóa đơn.
  - Hộp thoại `EditBookingDialog`: Đổi sân, đổi giờ linh hoạt kèm tính lại giá tự động.
  - Tích hợp trọn vẹn nút hành động Thu tiền, In hóa đơn, Điều chỉnh giờ trên cả `SchedulePage` và `BookingListPage`.
- [x] **Đạt tiêu chí nghiệm thu Ngày 6 (DoD Ngày 6)**:
  - *"Chạy trọn vòng đời 1 booking từ Pending đến Completed; hóa đơn in/preview ra đúng số"*.
  - Toàn bộ **136/136 unit tests tự động vượt qua (100% Passed)** với các bài test trọn vòng đời `BookingLifecycleTests`, `PaymentAndMembershipTests` và `InvoiceTests`.

### ✅ Ngày 7: Quản lý Khách hàng, Đặt lịch cố định (BR-18, BR-19), Khách vãng lai (A12, BR-05)
- [x] **Quản lý Hồ sơ Khách hàng & Ràng buộc toàn vẹn (CustomerService & CustomerListPage - BR-20, BR-21)**:
  - Danh bạ khách hàng chuyên nghiệp với KPI thống kê: Tổng số khách, Hội viên VIP/Vàng/Bạc, Khách mới trong tháng, Tổng doanh thu.
  - Tìm kiếm linh hoạt theo Họ tên, Số điện thoại hoặc Mã thẻ.
  - **BR-20 (Số điện thoại duy nhất)**: Bắt buộc và kiểm tra SĐT duy nhất trên toàn hệ thống trước khi thêm/sửa khách hàng.
  - **BR-21 (Xóa mềm)**: Tuyệt đối không xóa cứng khỏi CSDL (`IsActive = false`) để bảo toàn lịch sử giao dịch điểm và đơn đặt sân.
  - Drawer chi tiết khách hàng: Hiển thị hạng thẻ, % chiết khấu, lịch sử biến động điểm (`PointTransaction`), lịch sử đặt sân, và nút khóa/mở khóa tài khoản.
- [x] **Đặt lịch cố định theo tuần & Tự động sinh lịch 4 tuần tới (FixedScheduleService - BR-18, BR-19, A11)**:
  - Sinh đủ booking cho **4 tuần tiếp theo** (`FixedWeeksAhead = 4`) theo thứ trong tuần, khung giờ và sân đấu đã chọn (**BR-18**).
  - Từng booking đều được thẩm định tự động qua toàn bộ ma trận quy tắc: **BR-01** (mốc :00/:30), **BR-02** (thời lượng 60-240p), **BR-03** (chống trùng lịch), **BR-04** (khoảng trống 30p), **BR-05** (chống quá khứ).
  - **Cơ chế Bỏ qua tuần trùng & Báo cáo chi tiết**: Tự động bỏ qua tuần bị xung đột (trùng lịch hoặc để lại khe 30p) và thông báo danh sách ngày/tuần bị bỏ qua kèm lý do cụ thể (`SkippedDates` & `SkippedDetails`).
  - **BR-19 (Hủy lịch cố định)**: Khi hủy lịch cố định, tự động hủy toàn bộ các booking tương lai chưa check-in (`StartTime > DateTime.Now` và trạng thái `Pending`/`Confirmed`), bảo lưu toàn vẹn các đơn đã chơi để đối soát doanh thu.
  - Hộp thoại `FixedScheduleDialog`: Giao diện đặt lịch cố định trực quan, tích hợp chọn ngày trong tuần, giờ chơi, sân đấu và hiển thị ngay kết quả các tuần được tạo hoặc bị bỏ qua.
- [x] **Khách vãng lai & Check-in Nhanh tại Quầy (SchedulePage - A12, BR-05)**:
  - Nút bấm **"Khách vãng lai"** (⚡) nổi bật trên thanh công cụ Lịch sân.
  - Tự động làm tròn thời gian hiện tại xuống mốc 30 phút gần nhất theo chuẩn **BR-05** (ví dụ 14:15 $\to$ 14:00) và chọn sân trống ngay thời điểm hiện tại.
  - Tự động gán trạng thái ban đầu là **Đang chơi (`CheckedIn`) ngay lập tức** theo chuẩn **A12**, không yêu cầu tạo trước tài khoản hội viên.
- [x] **Đạt tiêu chí nghiệm thu Ngày 7 (DoD Ngày 7)**:
  - *"Tạo lịch cố định sinh đủ 4 booking, báo tuần trùng; vãng lai check-in được"*.
  - Toàn bộ **156/156 unit tests tự động vượt qua (100% Passed)** với 20 bài test mới cho `CustomerService`, `CustomerListViewModel`, `FixedScheduleService`, và các test tích hợp DoD Ngày 7 trong `ScheduleServiceTests`.

---

## 🏗 Cấu trúc dự án
```
BadmintonCourtManagement/
├── BadmintonCourtManagement.sln      # Solution mở bằng Visual Studio 2022
├── scripts/
│   ├── verify_day1_dod.sql           # Script SQL kiểm tra đạt tiêu chí Ngày 1 bằng SSMS
│   └── Verify-Database.ps1           # Script PowerShell kiểm thử tự động
├── BadmintonManager/
│   ├── App.xaml / App.xaml.cs         # Host, DI, Startup, Migration, Seeder & DI Services Registration
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
│   │   ├── Interfaces/                # IAuthService, IBranchCourtService, IUserService, IPricingService, IMembershipService...
│   │   └── (AuthService, BranchCourtService, UserService, PricingService, MembershipService, ...)
│   ├── Common/
│   │   ├── Result.cs                  # Standard Result / Result<T> pattern
│   │   ├── CurrentUserContext.cs      # Thông tin phiên đăng nhập (Singleton)
│   │   ├── Exceptions/                # BusinessException, ForbiddenException
│   │   └── Constants.cs
│   ├── DTOs/                          # AuthDtos, BranchCourtDtos, UserDtos, PricingDtos, CustomerDtos, BookingDtos
│   ├── ViewModels/
│   │   ├── Base/                      # ViewModelBase (ObservableObject, IsBusy, RunAsync), PlaceholderPageViewModel
│   │   ├── Admin/                     # AdminDashboardViewModel, AdminBranchesViewModel, AdminCourtsViewModel,
│   │   │                              # AdminUsersViewModel, AdminPricingViewModel, AdminTiersViewModel
│   │   ├── Manager/                   # ManagerDashboardViewModel, ManagerCourtsViewModel, ManagerStaffViewModel
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
│   │   └── Pages/                     # AdminBranchesPage, AdminCourtsPage, AdminUsersPage, AdminPricingPage, AdminTiersPage,
│   │                                  # ManagerCourtsPage, ManagerStaffPage, UserProfilePage, Dashboards...
│   ├── Infrastructure/                # NavigationService, DialogService, InvoicePrinter
│   ├── Converters/                    # BoolToVisibility, MoneyConverter, StatusToBrushConverter
│   └── Resources/                     # Colors.xaml, Styles.xaml
└── tests/
    └── BadmintonManager.Tests/        # 156 bài kiểm thử tự động xUnit (DoD Ngày 1, 2, 3, 4, 5, 6, 7 100% Passed)
```

---

## 👥 Danh sách tài khoản mẫu để kiểm thử phân quyền (Mật khẩu mặc định: `123456`)

| Tên đăng nhập | Mật khẩu | Vai trò | Tên hiển thị | Chi nhánh trực thuộc | Ghi chú quyền hạn |
|---|---|---|---|---|---|
| `admin` | `123456` | **Admin** | Hệ Thống Admin | Toàn hệ thống | Quản trị toàn quyền: Cơ sở, Sân mọi chi nhánh, Người dùng toàn hệ thống, Bảng giá & BR-10, Hạng thẻ, Báo cáo |
| `manager1` | `123456` | **Manager** | Nguyễn Văn Quản Lý | Cơ sở Cầu Giấy | Quản lý cơ sở 1: Chỉ thấy và quản lý sân CS Cầu Giấy, chỉ quản lý Staff CS Cầu Giấy |
| `manager2` | `123456` | **Manager** | Trần Thị Quản Lý | Cơ sở Đống Đa | Quản lý cơ sở 2: Chỉ thấy và quản lý sân CS Đống Đa, chỉ quản lý Staff CS Đống Đa |
| `manager3` | `123456` | **Manager** | Lê Văn Quản Lý | Cơ sở Thanh Xuân | Quản lý cơ sở 3: Chỉ thấy và quản lý sân CS Thanh Xuân, chỉ quản lý Staff CS Thanh Xuân |
| `staff1_1` | `123456` | **Staff** | Lê Thu Staff | Cơ sở Cầu Giấy | Nhân viên ca trực: Bảng điều khiển ca trực, Lịch sân ma trận, Đặt sân / Check-in tại quầy, Danh sách booking |
| `staff1_2` | `123456` | **Staff** | Hoàng Nam Staff | Cơ sở Cầu Giấy | Nhân viên ca trực cơ sở 1 |
| `customer1` | `123456` | **Customer** | Nguyễn Văn An | Khách hàng | Khách hàng hạng Vàng (1.600 điểm, chiết khấu 10%): Cổng thông tin, Tìm & Đặt sân trực tuyến |
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

### 2. Chạy toàn bộ 156 bài kiểm thử tự động (Unit Tests)
```powershell
dotnet test
```

### 3. Khởi chạy ứng dụng
Mở solution `BadmintonCourtManagement.sln` trong Visual Studio 2022 và nhấn `F5`, hoặc chạy từ dòng lệnh:
```powershell
dotnet run --project BadmintonManager/BadmintonManager.csproj
```
Khi ứng dụng mở:
- **Kiểm thử DoD Ngày 7 (Lịch Cố Định & Khách Vãng Lai Check-in)**:
  - Đăng nhập tài khoản Staff: `staff1_1` / `123456`.
  - Mở menu **"Lịch sân chi nhánh"**:
    - **Thao tác Khách vãng lai**: Bấm nút **"Khách vãng lai"** (⚡) trên thanh công cụ -> Hệ thống tự động làm tròn thời gian hiện tại xuống mốc 30 phút gần nhất (BR-05) và chọn sân trống -> Bấm **"Xác nhận nhận sân"** -> Đơn lập tức xuất hiện trên ma trận lịch ở trạng thái **Đang chơi (`CheckedIn`)** màu xanh lá.
    - **Thao tác Đặt lịch cố định**: Bấm nút **"Lịch cố định"** (📅) -> Chọn ngày trong tuần, giờ chơi (ví dụ 18:00 - 20:00) và sân đấu -> Hệ thống sinh đủ 4 booking cho 4 tuần tới. Nếu có tuần nào trùng giờ hoặc vi phạm khoảng trống 30 phút, hệ thống tự động bỏ qua tuần đó và báo chi tiết lý do.
    - **Thao tác Hủy lịch cố định (BR-19)**: Bấm vào một booking cố định trên lịch sân -> Bấm **"Hủy Toàn Bộ Lịch Cố Định"** -> Hệ thống tự động quét và hủy toàn bộ các lịch tương lai chưa check-in thuộc lịch này.
  - Mở menu **"Quản lý Khách hàng"**:
    - Tìm kiếm khách hàng theo Tên / SĐT / Mã thẻ.
    - Thêm khách hàng mới với số điện thoại đã tồn tại -> Bị chặn với thông báo tiếng Việt theo chuẩn **BR-20**.
    - Bấm nút xem chi tiết hồ sơ: Xem biến động tích điểm, danh sách các đơn đã đặt và thao tác khóa tài khoản.
- **Kiểm thử DoD Ngày 6 (Full Lifecycle & In Hóa Đơn)**:
  - Đăng nhập tài khoản Staff: `staff1_1` / `123456`.
  - Mở menu **"Lịch sân chi nhánh"** hoặc **"Danh sách đặt sân"**.
  - Bấm vào đơn đặt sân ở trạng thái Chờ duyệt (`Pending`) -> Chọn **Thu cọc**: Chọn tiền mặt hoặc chuyển khoản, đơn chuyển ngay sang Đã duyệt (`Confirmed`).
  - Khi khách đến nhận sân: Bấm nút **Check-in**, đơn chuyển sang trạng thái Đang chơi (`CheckedIn`).
  - Khi khách trả sân: Bấm nút **Thu tiền** -> Thu nốt số tiền còn lại -> Đơn tự động chuyển sang Hoàn tất (`Completed`), hệ thống tự động tích điểm và nâng hạng hội viên nếu đủ điều kiện.
  - Chọn **Xem & In hóa đơn**: Cửa sổ `InvoicePreviewDialog` mở bản in `FlowDocument` sắc nét với đầy đủ thông tin cơ sở, khách hàng, số tiền, chiết khấu và dư nợ; hỗ trợ in trực tiếp ra máy in hóa đơn.
- **Kiểm thử DoD Ngày 5 (Schedule Grid & Live Booking)**:
  - Xem ma trận 36 khung thời gian (06:00 - 24:00) chia theo 6 sân. Click ô trống bất kỳ để mở Drawer đặt sân; tự động tính giá và chiết khấu theo hạng thẻ hội viên; đặt trùng giờ sẽ bị chặn ngay lập tức.
- **Kiểm thử DoD Ngày 4 (Pricing & Booking Rules)**:
  - Tất cả các trường hợp nghiệp vụ: **Trùng giờ (BR-03)**, **Khoảng trống 30 phút (BR-04)**, **Vắt qua 2 khung giá (BR-07)**, **Cuối tuần (BR-08)**, **Giảm hạng hội viên (BR-09)**, **Làm tròn tiền cọc (BR-11)**, **Khung giờ :00/:30 (BR-01)**, **Thời lượng 60-240p (BR-02)**, **Chống quá khứ (BR-05)** đều được kiểm thử tự động bằng 40 bài xUnit test trong `BookingServiceTests.cs` và `PricingAndTierServiceTests.cs`.
- **Kiểm thử DoD Ngày 3 cho Admin & Manager**:
  - Đăng nhập `admin` / `123456` để quản lý chi nhánh, quản lý sân toàn bộ cơ sở, phân quyền tài khoản, cấu hình bảng giá phủ kín BR-10 và chính sách hạng hội viên.
  - Đăng nhập `manager1` / `123456` để kiểm tra phân quyền scoped: chỉ xem và thao tác với sân và nhân sự thuộc Cơ sở Cầu Giấy.
