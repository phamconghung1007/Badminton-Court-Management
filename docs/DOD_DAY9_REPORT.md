# BÁO CÁO NGHIỆM THU ĐẠT TIÊU CHÍ NGÀY 9 (DEFINITION OF DONE - DAY 9)

**Dự án**: Badminton Court Management System (Hệ Thống Quản Lý Sân Cầu Lông)  
**Công nghệ**: .NET 8, WPF, MVVM (CommunityToolkit.Mvvm), Entity Framework Core 8, SQL Server, LiveCharts2  
**Ngày thực hiện**: Ngày 9 / 10 ngày trong lộ trình  
**Tiêu chí nghiệm thu (DoD Day 9)**:  
> *"Số liệu báo cáo khớp với dữ liệu seed khi đối chiếu tay bằng SQL"*

---

## 1. TỔNG QUAN KẾT QUẢ TRIỂN KHAI NGÀY 9

Toàn bộ các yêu cầu của Ngày 9 theo tài liệu đặc tả dự án (Section 2, 4, 7, 8, 9, 11, 13, 16 và Quyết định đã chốt số 4 trang 17) đã được hiện thực hóa, tích hợp mượt mà và kiểm thử tự động 100% đạt chuẩn:

### 1.1. Kiến trúc Báo cáo Doanh thu & Hiệu suất (`ReportService`, `ReportDtos`, `IReportService`)
Dịch vụ báo cáo trung tâm `ReportService` được thiết kế chặt chẽ, tối ưu truy vấn EF Core (`AsNoTracking()`, gom nhóm dữ liệu trong bộ nhớ sau khi truy vấn) và áp đặt bảo vệ phân quyền đa tầng:

1. **Phân quyền và Scoping dữ liệu (Section 2 & 4)**:
   - **Quản trị viên (`Admin`)**: Có toàn quyền tra cứu báo cáo trên toàn hệ thống hoặc lọc theo từng cơ sở cụ thể; độc quyền sử dụng các báo cáo so sánh đa cơ sở (`GetBranchRevenueComparisonAsync`, `GetBranchOccupancyComparisonAsync`).
   - **Quản lý (`Manager`)**: Bị giới hạn cứng theo chi nhánh quản lý (`_currentUser.BranchId`). Nếu cố tình truyền `BranchId` khác để xem dữ liệu cơ sở khác, Service lập tức chặn đứng và ném `ForbiddenException("Quản lý chỉ có quyền xem báo cáo của cơ sở mình trực thuộc.")`. Nếu gọi hàm so sánh cơ sở của Admin, Service ném `ForbiddenException("Chỉ Quản trị viên mới có quyền xem báo cáo so sánh giữa các cơ sở.")`.
   - **Nhân viên (`Staff`) & Khách hàng (`Customer`)**: Bị chặn hoàn toàn quyền xem báo cáo tại tầng Service với `UnauthorizedAccessException("Bạn không có quyền truy cập báo cáo thống kê.")`.

2. **Thuật toán Tỷ lệ lấp đầy sân (Occupancy Rate - Trang 13)**:
   - Áp dụng chuẩn xác công thức đặc tả:
     $$\text{Tỷ lệ lấp đầy (\%)} = \frac{\sum \text{Tổng số phút đã đặt (Completed, CheckedIn, Confirmed)}}{\text{Số sân} \times \text{Số ngày trong kỳ} \times 18 \times 60} \times 100\%$$
   - Giờ mở cửa hoạt động theo quy định **A1** và **BR-01**: `06:00` đến `24:00` = 18 giờ/ngày = 1.080 phút/ngày.

3. **Thuật toán Thống kê Top Khách Hàng (Trang 14)**:
   - Gom nhóm các đơn đặt sân đã hoàn thành (`Status == Completed`), tính tổng số lượt chơi và tổng doanh số thực tế chi trả để vinh danh các khách hàng thân thiết nhất.

4. **Biểu đồ So sánh 3 Cơ Sở Cạnh Nhau (Quyết định số 4 trang 17)**:
   - Báo cáo của Admin tổng hợp doanh thu và tỷ lệ lấp đầy của cả 3 cơ sở (Cơ sở Cầu Giấy, Cơ sở Đống Đa, Cơ sở Thanh Xuân) cạnh nhau trên cùng một biểu đồ để ban lãnh đạo dễ dàng so sánh hiệu quả kinh doanh.

---

### 1.2. Màn hình Báo cáo Toàn Hệ Thống cho Admin (`AdminReportsPage` & `AdminReportsViewModel`)

- **Thư viện Biểu đồ LiveCharts2 (`LiveChartsCore.SkiaSharpView.WPF`)**:
  - Biểu đồ cột doanh thu theo ngày (`RevenueSeries`) với nhãn ngày xoay góc trực quan.
  - Biểu đồ cột so sánh doanh thu 3 cơ sở cạnh nhau (`BranchComparisonSeries`).
  - Biểu đồ cột tỷ lệ lấp đầy top sân thể thao (`OccupancySeries`).
- **Thanh công cụ lọc đa năng**:
  - Bộ chọn ngày `FromDate` và `ToDate` kèm phím tắt nhanh: *Hôm nay*, *Tuần này*, *Tháng này*, *30 ngày qua*.
  - Bộ chọn chi nhánh: *Tất cả cơ sở* hoặc từng cơ sở cụ thể.
- **4 Thẻ KPI Tổng Quan**:
  - 💵 **Tổng Doanh Thu**: Định dạng tiền tệ VNĐ chuẩn xác.
  - 📋 **Tổng Lượt Đặt**: Tổng số đơn trong kỳ.
  - ⏱️ **Giờ Hoạt Động**: Tổng số giờ sân đã phục vụ.
  - 📊 **Tỷ Lệ Lấp Đầy**: Tỷ lệ % bình quân toàn hệ thống.
- **4 Tab Dữ liệu Chi tiết**:
  - Tab 1: **Doanh thu theo ngày** (Biểu đồ + Bảng DataGrid chi tiết).
  - Tab 2: **So sánh 3 cơ sở** (Biểu đồ so sánh + Bảng tổng hợp chi nhánh).
  - Tab 3: **Tỷ lệ lấp đầy sân** (Biểu đồ + Bảng chi tiết 18 sân đấu).
  - Tab 4: **Top khách hàng thân thiết** (Bảng xếp hạng khách chi tiêu cao nhất).

---

### 1.3. Màn hình Báo cáo Cơ Sở cho Quản Lý (`ManagerReportsPage` & `ManagerReportsViewModel`)

- **Khóa cứng theo chi nhánh**:
  - Tự động lấy `BranchId` và tên chi nhánh từ phiên đăng nhập của Quản lý.
  - Hiển thị huy hiệu cơ sở trực thuộc nổi bật trên thanh tiêu đề.
  - Tuyệt đối không có ô chọn chi nhánh khác; chỉ truy vấn và hiển thị dữ liệu sân bãi và khách hàng phát sinh tại cơ sở mình.
- **Biểu đồ LiveCharts2 theo phong cách Quản lý**:
  - Tông màu chủ đạo Xanh hải quân (`#1E3A8A`) và Xanh mòng két (`#0D9488`).
  - Biểu đồ doanh thu ngày của cơ sở.
  - Biểu đồ so sánh tỷ lệ lấp đầy giữa 6 sân trực thuộc.
- **3 Tab Dữ liệu**: Doanh thu cơ sở, Hiệu suất từng sân, Khách hàng thường xuyên của cơ sở.

---

## 2. KẾT QUẢ ĐỐI SOÁT SỐ LIỆU VỚI CƠ SỞ DỮ LIỆU SEED (DoD VERIFICATION)

Hệ thống đã xây dựng script SQL chuyên dụng [`scripts/verify_day9_reports.sql`](file:///d:/Ki5/PRN212\ASM\Badminton-Court-Management\scripts\verify_day9_reports.sql) để đối chiếu tay các chỉ số báo cáo với dữ liệu seed trong CSDL `BadmintonDb`:

| STT | Chỉ số báo cáo | Kết quả tính toán từ `ReportService` | Kết quả truy vấn SQL thủ công | Đánh giá |
|:---:|---|:---:|:---:|:---:|
| **1** | **Tổng doanh thu 30 ngày qua** | Khớp chính xác $\sum$ `FinalAmount` của các đơn `Completed`, `CheckedIn`, `Confirmed` | Khớp 100% đến từng đồng | **ĐẠT (PASS)** |
| **2** | **Tổng lượt booking hợp lệ** | Khớp chính xác số dòng booking không bị hủy (`Status != Cancelled`) | Khớp 100% | **ĐẠT (PASS)** |
| **3** | **Doanh thu từng cơ sở (3 cơ sở)** | Phân bổ chính xác theo `Court.BranchId` của 3 chi nhánh | Khớp 100% | **ĐẠT (PASS)** |
| **4** | **Tỷ lệ lấp đầy từng sân** | $\frac{\text{Phút đặt}}{31 \times 18 \times 60} \times 100\%$ | Khớp 100% | **ĐẠT (PASS)** |
| **5** | **Top khách hàng chi tiêu** | Gom nhóm theo `CustomerId` các đơn `Completed`, sắp xếp giảm dần | Khớp 100% | **ĐẠT (PASS)** |

---

## 3. KẾT QUẢ KIỂM THỬ TỰ ĐỘNG (UNIT TESTS)

Toàn bộ **191/191 bài kiểm thử tự động (Unit Tests)** đều chạy thành công mỹ mãn (**100% Passed**, không có bất kỳ test nào thất bại hoặc bị bỏ qua).

```
Test run for BadmintonManager.Tests.dll (.NETCoreApp,Version=v8.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   191, Skipped:     0, Total:   191, Duration: 16 s
```

### 3.1. Phân bổ các nhóm bài kiểm thử trọng tâm Ngày 9 (18 bài test mới & cập nhật):
1. **`ReportServiceTests.cs`** (8 bài test nghiệp vụ & bảo mật Service Layer):
   - `Admin_CanGetOverviewSummary_AndDailyRevenue_AcrossAllBranches`: Admin xem báo cáo tổng hợp toàn hệ thống.
   - `Admin_CanGetBranchRevenueComparison_AndOccupancyComparison`: Admin xem so sánh doanh thu và tỷ lệ lấp đầy 3 cơ sở.
   - `Manager_CanGetReports_ForOwnBranch`: Manager xem báo cáo cơ sở mình.
   - `Manager_AccessingOtherBranch_ThrowsForbiddenException`: Manager cố xem chi nhánh khác bị ném `ForbiddenException`.
   - `Manager_CallingBranchComparison_ThrowsForbiddenException`: Manager cố xem so sánh đa cơ sở bị chặn `ForbiddenException`.
   - `Staff_Or_Customer_CallingReports_ThrowsUnauthorizedAccessException`: Staff và Customer cố xem báo cáo bị chặn `UnauthorizedAccessException`.
   - `CourtUtilization_CalculatesCorrectRates`: Tính đúng tỷ lệ lấp đầy theo công thức 18 giờ/ngày.
   - `TopCustomers_ReturnsOrderedByTotalSpent`: Sắp xếp đúng khách hàng chi tiêu nhiều nhất.

2. **`AdminReportsViewModelTests.cs`** (4 bài test UI MVVM Admin):
   - `Initialize_LoadsBranches_AndInitialReports`: Tự động nạp danh sách cơ sở và báo cáo ban đầu.
   - `FilterQuickButtons_UpdatesDateRanges`: Các nút chọn nhanh cập nhật khoảng ngày chính xác.
   - `LoadReports_WhenFromDateGreaterThanToDate_ShowsWarning`: Cảnh báo khi ngày bắt đầu > ngày kết thúc.
   - `FilterByBranch_LoadsScopedReports`: Chọn cơ sở cụ thể lọc đúng số liệu của cơ sở đó.

3. **`ManagerReportsViewModelTests.cs`** (4 bài test UI MVVM Manager):
   - `Initialize_LoadsBranchReports_LockedToOwnBranch`: Khóa cứng theo cơ sở trực thuộc của Quản lý.
   - `Manager_DoesNotSeeOtherBranchData`: Tuyệt đối không chứa dữ liệu của các cơ sở khác.
   - `FilterQuickButtons_UpdatesDateRanges`: Nút chọn nhanh cập nhật ngày chính xác.
   - `LoadReports_WhenFromDateGreaterThanToDate_ShowsWarning`: Cảnh báo ngày không hợp lệ.

4. **`ShellNavigationTests.cs`** (2 bài test cập nhật điều hướng):
   - `ShellViewModel_NavigateToAdminReports_ResolvesAdminReportsViewModel`: Điều hướng menu "AdminReports" nạp đúng ViewModel.
   - `ShellViewModel_NavigateToManagerReports_ResolvesManagerReportsViewModel`: Điều hướng menu "ManagerReports" nạp đúng ViewModel.

---

## 4. HƯỚNG DẪN KIỂM CHỨNG TRỰC TIẾP TRÊN ỨNG DỤNG (MANUAL VERIFICATION)

### Bước 1: Khởi chạy ứng dụng
```powershell
dotnet run --project BadmintonManager/BadmintonManager.csproj
```

### Bước 2: Kiểm chứng Báo cáo Quản trị viên (Admin)
- Đăng nhập: `admin` / `123456`.
- Bấm menu **"Báo cáo doanh thu"**:
  - 4 Thẻ KPI hiển thị tổng doanh thu toàn hệ thống, số lượt booking, tổng giờ chơi và tỷ lệ lấp đầy.
  - Tab **"Doanh Thu Theo Ngày"**: Biểu đồ LiveCharts hiển thị các cột doanh thu ngày rõ nét.
  - Tab **"So Sánh 3 Cơ Sở"**: Biểu đồ cột hiển thị doanh thu 3 cơ sở cạnh nhau: Cơ sở Cầu Giấy, Cơ sở Đống Đa, Cơ sở Thanh Xuân.
  - Tab **"Tỷ Lệ Lấp Đầy Sân"**: Biểu đồ hiển thị top sân đông khách nhất kèm bảng 18 sân.
  - Tab **"Top Khách Hàng Thân Thiết"**: Bảng xếp hạng khách hàng có chi tiêu cao nhất.
  - Thử chọn cơ sở "Cơ sở Cầu Giấy" và bấm "Xem Báo Cáo" -> Số liệu và biểu đồ tự động cập nhật riêng cho cơ sở Cầu Giấy.

### Bước 3: Kiểm chứng Báo cáo Quản lý cơ sở (Manager)
- Đăng nhập: `manager1` / `123456` (Quản lý Cơ sở Cầu Giấy).
- Bấm menu **"Báo cáo doanh thu cơ sở"**:
  - Huy hiệu hiển thị rõ "Cơ sở Cầu Giấy", không có ComboBox chọn cơ sở khác.
  - Các chỉ số KPI, biểu đồ cột và bảng số liệu chỉ hiển thị các đơn đặt và 6 sân thuộc Cơ sở Cầu Giấy.
  - Không có Tab so sánh đa cơ sở.

### Bước 4: Kiểm chứng Đối soát bằng SQL
- Mở SQL Server Management Studio (SSMS), kết nối tới `BadmintonDb` và chạy file script:
  ```powershell
  # Hoặc chạy qua sqlcmd:
  sqlcmd -S localhost -d BadmintonDb -i scripts/verify_day9_reports.sql
  ```
- Kết quả đối chiếu tay khớp 100% với các số liệu hiển thị trên ứng dụng.

---

## 5. KẾT LUẬN NGHIỆM THU NGÀY 9

- **Mục tiêu Ngày 9**: Báo cáo & Thống kê doanh thu, biểu đồ LiveCharts2, so sánh 3 cơ sở cho Admin, khóa chi nhánh cho Manager.
- **Tiêu chuẩn nghiệm thu (DoD Day 9)**: *"Số liệu báo cáo khớp với dữ liệu seed khi đối chiếu tay bằng SQL"* $\to$ **ĐÃ ĐẠT 100%**.
- **Chất lượng kiểm thử**: 191/191 tests passed (100%), không có cảnh báo nghiêm trọng hay lỗi tiềm ẩn.
