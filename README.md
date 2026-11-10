# Badminton Court Management System (Hệ Thống Quản Lý Sân Cầu Lông)

## 🛠 Công nghệ sử dụng
- **Ngôn ngữ**: C# (.NET 8)
- **Giao diện**: WPF (XAML), MVVM Pattern với `CommunityToolkit.Mvvm`
- **UI & Biểu đồ**: `MaterialDesignThemes`, `LiveChartsCore.SkiaSharpView.WPF`
- **ORM**: Entity Framework Core 8.0 (Code First, Migrations)
- **Cơ sở dữ liệu**: Microsoft SQL Server
- **Bảo mật**: `BCrypt.Net-Next` (Băm mật khẩu)
- **Kiến trúc & DI**: `Microsoft.Extensions.Hosting` (Generic Host, Dependency Injection, `IDbContextFactory<AppDbContext>`)
- **Kiểm thử tự động**: xUnit, FluentAssertions, EF Core InMemory Database (213 unit tests 100% passed)

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

### ✅ Ngày 8: Customer Portal & Phân quyền bảo mật (Cross-Access Protection)
- [x] **Bảo vệ Phân quyền & Chống Truy cập Chéo ở Tầng Service (Section 2, 4, 8 & DoD Day 8)**:
  - **`BookingService`**:
    - Khi `Role == "Customer"`, tự động khóa bộ lọc cứng `CustomerId == user.CustomerId`.
    - Chặn xem, sửa, hủy (`CancelBookingAsync`), hoặc đổi lịch (`RescheduleBookingAsync`) đơn của khách khác bằng `UnauthorizedAccessException`.
    - Chặn khách tự duyệt (`ApproveBookingAsync`), check-in (`CheckInAsync`) hoặc hoàn tất đơn.
  - **`CustomerService`**: Khách hàng chỉ có thể xem hồ sơ của chính mình; chặn mọi hành vi tra cứu, cập nhật, xóa mềm khách khác hoặc tự cộng điểm.
  - **`PaymentService`**: Chặn hoàn toàn vai trò `Customer` thu tiền hoặc hoàn cọc trực tiếp qua Service.
  - **`FixedScheduleService`**: Chặn khách hàng tạo hoặc hủy lịch cố định của tài khoản khác.
- [x] **Tìm kiếm Sân Trống & Đặt Sân Trực Tuyến (`CustomerBookCourtPage` & `CustomerBookCourtViewModel`)**:
  - Tra cứu sân khả dụng thời gian thực (`GetAvailableCourtsAsync`) theo Chi nhánh, Ngày và Khung giờ.
  - Tự động thẩm định qua toàn bộ quy tắc: loại bỏ sân trùng lịch (**BR-03**), loại bỏ sân để lại khoảng trống 30 phút (**BR-04**), giới hạn thời lượng 60-240 phút (**BR-02**) và chặn đặt trong quá khứ (**BR-05**).
  - Khung **Live Pricing Preview**: Tự động chia block 30 phút, áp giá Ngày thường / Cuối tuần (**BR-07, BR-08**), trừ chiết khấu hội viên (**BR-09**), tính tiền cọc tối thiểu 30% làm tròn lên 1.000 VNĐ (**BR-11**).
  - Tạo đơn trực tuyến ở trạng thái Chờ duyệt (`Pending`), tự động gán đúng `CustomerId`, hiển thị nhắc nhở chuyển khoản cọc trong vòng **2 giờ** theo chuẩn **A7/A8**.
- [x] **Lịch sử Đặt Sân & Đổi/Hủy Lịch (`CustomerBookingsPage` & `CustomerBookingsViewModel`)**:
  - Quản lý danh sách các đơn đặt sân của riêng khách hàng đăng nhập; bộ lọc trạng thái đa năng.
  - **BR-14 (Hủy lịch)**: Tự động phát hiện hủy trước $\ge 24$ giờ (thông báo hoàn cọc 100%) hoặc hủy $< 24$ giờ (cảnh báo mất cọc).
  - **A9 (Đổi lịch)**: Drawer trượt cho phép chọn ngày và giờ chơi mới, bắt buộc trước giờ chơi ít nhất 24 giờ.
- [x] **Điểm thưởng, Thăng hạng & Dashboard Khách hàng (`CustomerPointsPage`, `CustomerDashboardPage`)**:
  - Thẻ hạng hội viên (Đồng, Bạc, Vàng), số điểm tích lũy và % chiết khấu đang áp dụng (**BR-16**).
  - Bảng lịch sử biến động điểm (`PointTransaction`) minh bạch (**BR-15**).
  - Thanh tiến trình thăng hạng (Progress Bar) trực quan, hiển thị số điểm cần thêm để lên hạng tiếp theo.
  - Danh sách 5 đơn đặt sân sắp diễn ra gần nhất và các nút phím tắt điều hướng nhanh.
- [x] **Đạt tiêu chí nghiệm thu Ngày 8 (DoD Ngày 8)**:
  - *"Customer chỉ thấy dữ liệu của mình (thử truy cập chéo bị chặn ở Service)"*.
  - Toàn bộ **173/173 unit tests tự động vượt qua (100% Passed)** với 17 bài test mới bao phủ `CustomerAccessSecurityTests` (8 tests), `CustomerBookCourtViewModelTests` (5 tests), `CustomerBookingsViewModelTests` (3 tests), và `CustomerPointsViewModelTests` (1 test).

### ✅ Ngày 9: Báo cáo & Thống kê doanh thu, Biểu đồ LiveCharts2, So sánh 3 cơ sở & Phân quyền
- [x] **Dịch vụ Báo cáo & Phân quyền Scoping dữ liệu (ReportService - Section 2, 4, 13 & Quyết định 4)**:
  - **Quản trị viên (`Admin`)**: Xem doanh thu toàn hệ thống, lọc từng cơ sở, độc quyền xem so sánh cạnh nhau 3 chi nhánh (`GetBranchRevenueComparisonAsync`, `GetBranchOccupancyComparisonAsync`).
  - **Quản lý (`Manager`)**: Bị khóa cứng theo `BranchId` trực thuộc (`_currentUser.BranchId`). Nếu cố tình truyền `BranchId` khác hoặc gọi hàm so sánh cơ sở của Admin, Service ném ngay `ForbiddenException`.
  - **Nhân viên (`Staff`) & Khách hàng (`Customer`)**: Bị chặn hoàn toàn quyền truy cập báo cáo tại Service với `UnauthorizedAccessException`.
  - **Thuật toán tỷ lệ lấp đầy sân (Occupancy Rate)**: $\frac{\sum \text{Phút đã đặt}}{\text{Số sân} \times \text{Số ngày} \times 18 \times 60} \times 100\%$ (giờ mở cửa `06:00 - 24:00` theo A1, BR-01).
  - **Thuật toán Top khách hàng**: Gom nhóm các đơn `Completed`, tính tổng lượt đặt và tổng chi tiêu thực tế.
- [x] **Báo cáo Toàn hệ thống cho Admin (`AdminReportsPage` & `AdminReportsViewModel`)**:
  - Tích hợp biểu đồ trực quan **LiveCharts2** (`LiveChartsCore.SkiaSharpView.WPF`): biểu đồ cột doanh thu ngày (`RevenueSeries`), biểu đồ so sánh cạnh nhau 3 chi nhánh (`BranchComparisonSeries`), và biểu đồ tỷ lệ lấp đầy top sân (`OccupancySeries`).
  - Bộ lọc: Từ ngày, Đến ngày, các nút chọn nhanh (Hôm nay, Tuần này, Tháng này, 30 ngày qua), cùng bộ chọn Cơ sở linh hoạt.
  - 4 thẻ KPI tổng quan: Doanh thu toàn hệ thống, Tổng lượt booking, Tổng số giờ chơi, Tỷ lệ lấp đầy bình quân.
  - 4 Tab dữ liệu chuyên sâu: Doanh thu theo ngày, So sánh 3 cơ sở, Tỷ lệ lấp đầy sân, Top khách hàng thân thiết.
- [x] **Báo cáo Doanh thu & Hiệu suất Cơ sở cho Manager (`ManagerReportsPage` & `ManagerReportsViewModel`)**:
  - Giao diện chuẩn phong cách Quản lý, hiển thị huy hiệu chi nhánh trực thuộc nổi bật.
  - Khóa cứng dữ liệu theo cơ sở của Quản lý; không có chức năng chọn cơ sở khác hay xem dữ liệu cơ sở khác.
  - Biểu đồ LiveCharts2 doanh thu theo ngày và tỷ lệ lấp đầy 6 sân trực thuộc chi nhánh.
  - Danh sách Top khách hàng thường xuyên phát sinh doanh số tại cơ sở mình.
- [x] **Đạt tiêu chí nghiệm thu Ngày 9 (DoD Ngày 9)**:
  - *"Số liệu báo cáo khớp với dữ liệu seed khi đối chiếu tay bằng SQL"*.
  - Xây dựng script SQL đối soát độc lập [`scripts/verify_day9_reports.sql`](scripts/verify_day9_reports.sql) đối chiếu tay 100% khớp số liệu seed trong `BadmintonDb`.
  - Toàn bộ **191/191 unit tests tự động vượt qua (100% Passed)** với 18 bài test mới bao phủ `ReportServiceTests` (8 tests), `AdminReportsViewModelTests` (4 tests), `ManagerReportsViewModelTests` (4 tests), và cập nhật điều hướng trong `ShellNavigationTests` (2 tests).

### ✅ Ngày 10: Sửa Lỗi, Kiểm Tra Checklist Mục 10, E2E Role Matrix Tests & Tự Động Hóa Nghiệm Thu Máy Mới
- [x] **Dọn dẹp mã nguồn & Kiểm toán Checklist Mục 10 (Code Cleanup & Architecture Audit)**:
  - Dọn dẹp sạch sẽ 100% các compiler warnings CS8604 và CS8601 trong `BranchCourtService.cs` và `InvoiceService.cs`.
  - Kiểm toán cấu trúc bằng Reflection trong `ChecklistAuditTests.cs`: Đảm bảo **100% không gọi `MessageBox.Show()` trong ViewModel** (tuân thủ nghiêm ngặt mô hình DI với `IDialogService`).
  - Đảm bảo **triệt tiêu hoàn toàn lỗi N+1 queries** qua việc chuẩn hóa Eager Loading (`Include` / `ThenInclude`) và `AsNoTracking()` trong toàn bộ các truy vấn báo cáo và danh sách.
- [x] **Kiểm thử tự động Ma Trận Phân Quyền Mục 2 (13 Quyền x 4 Vai Trò - `RolePermissionMatrixTests`)**:
  - Viết 11 kịch bản kiểm thử tích hợp E2E tự động xác thực Ma trận phân quyền Mục 2 của tài liệu đặc tả trên 4 vai trò:
    - *Quản lý cơ sở*: Admin toàn quyền, Manager/Staff/Customer bị chặn (`ForbiddenException`).
    - *Quản lý sân*: Admin quản lý toàn bộ, Manager chỉ quản lý sân thuộc cơ sở của mình (thao tác sân cơ sở khác bị từ chối), Staff/Customer bị chặn.
    - *Quản lý tài khoản*: Admin quản lý mọi tài khoản; Manager chỉ được tạo Staff cơ sở mình (cấm tạo Admin/Manager và cấm can thiệp cơ sở khác).
    - *Cấu hình giá & Hạng hội viên*: Độc quyền tối cao của Admin; Manager/Staff/Customer bị cấm can thiệp.
    - *Tra cứu danh sách đặt sân*: Admin thấy toàn bộ; Manager/Staff chỉ thấy cơ sở mình; Customer chỉ thấy đơn của chính mình.
    - *Duyệt đơn & Điểm danh / Thu tiền*: Nhân viên quầy và Quản lý thực hiện; cấm Khách hàng tự duyệt hoặc thu tiền.
    - *Hủy đơn đặt sân*: Khách hàng chỉ được hủy đơn của mình (không thể hủy đơn của người khác).
    - *Báo cáo doanh thu*: Chỉ mở cho Admin và Manager cơ sở; cấm hoàn toàn Staff và Customer.
- [x] **Nâng cấp `DbHealthChecker.cs` & Tự Động Hóa Kiểm Chứng Máy Mới (DoD Ngày 10)**:
  - Bổ sung `VerifyDay10FinalDoDAsync`: Kiểm tra tự động 10 chỉ tiêu sống còn của CSDL (Hạ tầng, Cơ sở, Sân, Người dùng băm BCrypt, Hạng thẻ, Bảng giá 06:00-24:00, 10 SystemSettings, 50+ bookings đủ trạng thái, Payments, Tính toàn vẹn tài chính).
  - Tích hợp cờ dòng lệnh `--verify-day10` và `--verify-all` trong `App.xaml.cs` in báo cáo nghiệm thu chuyên nghiệp.
  - Cung cấp script tự động hóa 1-click [scripts/Verify-FreshMachine.ps1](scripts/Verify-FreshMachine.ps1) và [scripts/Verify-FreshMachine.bat](scripts/Verify-FreshMachine.bat).
- [x] **Đạt tiêu chí nghiệm thu Ngày 10 (DoD Ngày 10)**:
  - *"Chạy sạch từ máy mới theo README; checklist mục 10 đạt"*.
  - Toàn bộ **213/213 unit & E2E tests tự động vượt qua (100% Passed)**.
  - Chạy `Verify-FreshMachine.ps1`: Đạt **10/10 tiêu chí nghiệm thu hoàn hảo**!

---

## 🏗 Cấu trúc dự án
```
BadmintonCourtManagement/
├── BadmintonCourtManagement.sln      # Solution mở bằng Visual Studio 2022
├── docs/                             # Báo cáo nghiệm thu Definition of Done (DoD)
│   ├── DOD_DAY5_REPORT.md            # Báo cáo nghiệm thu Ngày 5
│   ├── DOD_DAY6_REPORT.md            # Báo cáo nghiệm thu Ngày 6
│   ├── DOD_DAY7_REPORT.md            # Báo cáo nghiệm thu Ngày 7
│   ├── DOD_DAY8_REPORT.md            # Báo cáo nghiệm thu Ngày 8
│   ├── DOD_DAY9_REPORT.md            # Báo cáo nghiệm thu Ngày 9
│   └── DOD_DAY10_FINAL_REPORT.md     # Báo cáo nghiệm thu Tổng kết Toàn diện Ngày 10
├── scripts/
│   ├── Verify-FreshMachine.ps1       # Script PowerShell tự động nghiệm thu máy mới từ A-Z
│   ├── Verify-FreshMachine.bat       # File Batch Windows 1-click nghiệm thu máy mới
│   ├── verify_day1_dod.sql           # Script SQL kiểm tra đạt tiêu chí Ngày 1 bằng SSMS
│   ├── verify_day9_reports.sql        # Script SQL đối soát số liệu báo cáo Ngày 9
│   └── Verify-Database.ps1           # Script PowerShell kiểm thử CSDL Ngày 1
├── BadmintonManager/
│   ├── App.xaml / App.xaml.cs         # Host, DI, Startup, Migration, Seeder & Command-line Flags
│   ├── appsettings.example.json       # File mẫu cấu hình chuỗi kết nối DB
│   ├── Models/
│   │   ├── Entities/                  # 11 Entity: Branch, Court, AppUser, Customer, MembershipTier,
│   │   │                              # PriceRule, Booking, FixedSchedule, Payment, PointTransaction, SystemSetting
│   │   └── Enums/                     # UserRole, BookingStatus, BookingType, DayType, PaymentMethod, PaymentType
│   ├── Data/
│   │   ├── AppDbContext.cs            # DbContext cấu hình qua Assembly Configurations, Business validation
│   │   ├── Configurations/            # Fluent API IEntityTypeConfiguration<T> cho từng bảng
│   │   ├── Migrations/                # EF Core Code First Migrations
│   │   ├── DbSeeder.cs                # Seed dữ liệu mẫu (3 cơ sở, 18 sân, tài khoản, giá, settings, ~56 bookings)
│   │   └── DbHealthChecker.cs         # Kiểm tra đạt chuẩn Definition of Done (DoD Day 1 & Day 10)
│   ├── Services/
│   │   ├── Interfaces/                # IAuthService, IBranchCourtService, IUserService, IPricingService, IReportService...
│   │   └── (AuthService, BranchCourtService, UserService, PricingService, MembershipService, ReportService, ...)
│   ├── Common/
│   │   ├── Result.cs                  # Standard Result / Result<T> pattern
│   │   ├── CurrentUserContext.cs      # Thông tin phiên đăng nhập (Singleton)
│   │   ├── Exceptions/                # BusinessException, ForbiddenException
│   │   └── Constants.cs
│   ├── DTOs/                          # AuthDtos, BranchCourtDtos, UserDtos, PricingDtos, CustomerDtos, BookingDtos, ReportDtos
│   ├── ViewModels/
│   │   ├── Base/                      # ViewModelBase (ObservableObject, IsBusy, RunAsync), PlaceholderPageViewModel
│   │   ├── Admin/                     # AdminDashboardViewModel, AdminBranchesViewModel, AdminCourtsViewModel,
│   │   │                              # AdminUsersViewModel, AdminPricingViewModel, AdminTiersViewModel, AdminReportsViewModel
│   │   ├── Manager/                   # ManagerDashboardViewModel, ManagerCourtsViewModel, ManagerStaffViewModel, ManagerReportsViewModel
│   │   ├── Staff/                     # StaffDashboardViewModel
│   │   ├── Customer/                  # CustomerDashboardViewModel, CustomerBookCourtViewModel,
│   │   │                              # CustomerBookingsViewModel, CustomerPointsViewModel
│   │   ├── Shared/                    # UserProfileViewModel, ScheduleViewModel, BookingListViewModel, CustomerListViewModel
│   │   ├── LoginViewModel.cs          # Đăng nhập hệ thống
│   │   ├── RegisterViewModel.cs       # Đăng ký tài khoản khách hàng
│   │   └── ShellViewModel.cs          # Điều hướng menu động phân quyền theo 4 vai trò
│   ├── Views/
│   │   ├── LoginWindow.xaml           # Cửa sổ đăng nhập
│   │   ├── RegisterWindow.xaml        # Cửa sổ đăng ký khách hàng
│   │   ├── ShellWindow.xaml           # Khung giao diện chính với Navigation Bar và Viewport
│   │   └── Pages/                     # AdminReportsPage, ManagerReportsPage, CustomerBookCourtPage, CustomerBookingsPage...
│   ├── Infrastructure/                # NavigationService, DialogService, InvoicePrinter
│   ├── Converters/                    # BoolToVisibility, MoneyConverter, StatusToBrushConverter
│   └── Resources/                     # Colors.xaml, Styles.xaml
└── tests/
    └── BadmintonManager.Tests/        # 213 bài kiểm thử tự động xUnit (DoD Ngày 1 đến 10 100% Passed)
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

### ⚡ Cài đặt & Nghiệm thu tự động trên máy mới (Day 10 DoD - 1-Click Verification)
Chỉ cần thực hiện 1 thao tác duy nhất để hệ thống tự động: Kiểm tra .NET SDK -> Build mã nguồn -> Chạy 213 bài kiểm thử -> Migrate & Seed CSDL -> In báo cáo đạt chuẩn 10/10 tiêu chí:

**Cách 1: Chạy file Batch (Khuyên dùng trên Windows - Nhấp đúp chuột):**
```cmd
scripts\Verify-FreshMachine.bat
```

**Cách 2: Chạy qua PowerShell:**
```powershell
.\scripts\Verify-FreshMachine.ps1
```

**Cách 3: Chạy trực tiếp từ .NET CLI:**
```powershell
dotnet run --project BadmintonManager/BadmintonManager.csproj -- --verify-day10
```

---

### 1. Cấu hình chuỗi kết nối thủ công
Sao chép file `appsettings.example.json` thành `appsettings.json` trong thư mục `BadmintonManager/` và cập nhật chuỗi kết nối tới SQL Server của bạn (nếu dùng cấu hình riêng):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BadmintonDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 2. Chạy toàn bộ 213 bài kiểm thử tự động (Unit & E2E Role Matrix Tests)
```powershell
dotnet test
```

### 3. Khởi chạy ứng dụng
Mở solution `BadmintonCourtManagement.sln` trong Visual Studio 2022 và nhấn `F5`, hoặc chạy từ dòng lệnh:
```powershell
dotnet run --project BadmintonManager/BadmintonManager.csproj
```
Khi ứng dụng mở:
- **Kiểm thử DoD Ngày 10 (Chạy Sạch Từ Máy Mới, Ma Trận Phân Quyền Mục 2 & Checklist Mục 10)**:
  - Xem chi tiết tại báo cáo nghiệm thu [docs/DOD_DAY10_FINAL_REPORT.md](docs/DOD_DAY10_FINAL_REPORT.md).
  - Kiểm thử 13 chức năng trên 4 vai trò qua bộ test E2E:
    ```powershell
    dotnet test --filter "FullyQualifiedName~RolePermissionMatrixTests"
    ```
  - Kiểm tra kiến trúc không MessageBox trong ViewModel và không N+1 queries:
    ```powershell
    dotnet test --filter "FullyQualifiedName~ChecklistAuditTests"
    ```
- **Kiểm thử DoD Ngày 9 (Báo Cáo & Thống Kê Doanh Thu, LiveCharts2 & Đối Soát SQL)**:
  - **Kiểm thử Báo cáo Quản trị viên (Admin)**:
    - Đăng nhập: `admin` / `123456`.
    - Mở menu **"Báo cáo doanh thu"**: Xem 4 thẻ KPI tổng quan hệ thống, biểu đồ cột doanh thu ngày LiveCharts2, biểu đồ so sánh 3 cơ sở cạnh nhau (Cơ sở Cầu Giấy, Đống Đa, Thanh Xuân), biểu đồ tỷ lệ lấp đầy top sân và danh sách top khách hàng thân thiết.
    - Chọn một cơ sở cụ thể trong ComboBox và bấm "Xem Báo Cáo" -> Số liệu và biểu đồ tự động cập nhật riêng cho cơ sở đó.
  - **Kiểm thử Báo cáo Quản lý cơ sở (Manager)**:
    - Đăng nhập: `manager1` / `123456`.
    - Mở menu **"Báo cáo doanh thu cơ sở"**: Hệ thống khóa cứng theo "Cơ sở Cầu Giấy", hiển thị huy hiệu cơ sở trực thuộc; không có bộ chọn cơ sở khác, không có tab so sánh đa cơ sở. Mọi số liệu chỉ hiển thị các đơn và 6 sân của Cơ sở Cầu Giấy.
  - **Đối soát số liệu bằng SQL (DoD Ngày 9)**:
    - Chạy script SQL đối soát:
      ```powershell
      sqlcmd -S localhost -d BadmintonDb -i scripts/verify_day9_reports.sql
      ```
    - Đối chiếu tay số liệu kết quả SQL với ứng dụng: tổng doanh thu, số lượt booking, doanh thu từng cơ sở, tỷ lệ lấp đầy sân và top khách hàng khớp 100%.
- **Kiểm thử DoD Ngày 8 (Customer Portal & Bảo Mật Chống Truy Cập Chéo)**:
  - Đăng nhập tài khoản Khách hàng: `customer1` / `123456`.
  - **Phân quyền Menu**: Khách hàng chỉ thấy 4 menu riêng biệt (Dashboard, Đặt sân trực tuyến, Lịch sử đặt sân, Điểm thưởng), ẩn toàn bộ menu quản trị của Admin, Manager, Staff.
  - **Đặt sân trực tuyến**: Chọn cơ sở, ngày, giờ -> Bấm "Tìm sân trống" -> Thẻ card sân rảnh xuất hiện kèm Live Pricing Preview (tự động giảm 10% cho Hạng Vàng theo BR-09, tính cọc 30% làm tròn lên 1.000 VNĐ theo BR-11) -> Bấm đặt sân nhận thông báo đơn `Pending` và nhắc chuyển khoản cọc trong vòng 2 giờ (A7/A8).
  - **Lịch sử đặt sân**: Chỉ hiển thị các booking của riêng `customer1`. Thử hủy đơn: hệ thống tự động kiểm tra mốc $\ge 24$ giờ để báo hoàn cọc hay mất cọc (BR-14). Thử đổi lịch: Drawer trượt yêu cầu trước giờ chơi $\ge 24$ giờ (A9).
  - **Điểm thưởng & Hạng thẻ**: Xem huy hiệu Hạng Vàng, số điểm 1.600, chiết khấu 10%, tiến trình thăng hạng và bảng lịch sử biến động điểm `PointTransaction` (BR-15, BR-16).
  - **Bảo vệ chống truy cập chéo ở Service**: Chặn đứng ở Service Layer khi khách hàng cố tra cứu hoặc can thiệp dữ liệu của khách khác (`UnauthorizedAccessException`). Kiểm thử tự động qua lệnh:
    ```powershell
    dotnet test --filter "FullyQualifiedName~CustomerAccessSecurityTests"
    ```
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
