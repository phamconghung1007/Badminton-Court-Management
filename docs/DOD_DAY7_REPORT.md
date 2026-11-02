# BÁO CÁO NGHIỆM THU ĐẠT TIÊU CHÍ NGÀY 7 (DEFINITION OF DONE - DAY 7)

**Dự án**: Badminton Court Management System (Hệ Thống Quản Lý Sân Cầu Lông)  
**Công nghệ**: .NET 8, WPF, MVVM (CommunityToolkit.Mvvm), Entity Framework Core 8, SQL Server  
**Ngày thực hiện**: Ngày 7 / 10 ngày trong lộ trình  
**Tiêu chí nghiệm thu (DoD Day 7)**:  
> *"Tạo lịch cố định sinh đủ 4 booking, báo tuần trùng; vãng lai check-in được"*

---

## 1. TỔNG QUAN KẾT QUẢ TRIỂN KHAI NGÀY 7

Toàn bộ các yêu cầu của Ngày 7 theo tài liệu đặc tả dự án (Section 7, 8, 9, 13, 16 và Phụ lục A11, A12, BR-05, BR-18, BR-19, BR-20, BR-21) đã được hiện thực hóa, tích hợp mượt mà và kiểm thử tự động 100% đạt chuẩn:

### 1.1. Quản lý Khách hàng Toàn diện (`CustomerService` & `CustomerListPage`)
- **Quản lý danh bạ khách hàng**:
  - Tra cứu nhanh theo họ tên, số điện thoại hoặc mã thẻ thành viên.
  - Phân trang, hiển thị tổng chi tiêu, tổng điểm tích lũy, số lượng đơn đặt sân.
  - Thẻ thông tin KPI tổng hợp: Tổng số khách hàng, Hội viên VIP/Vàng/Bạc, Khách mới trong tháng, Tổng doanh thu tích lũy.
- **Ràng buộc nghiệp vụ khách hàng**:
  - **BR-20**: Kiểm tra tính duy nhất của Số điện thoại khách hàng trên toàn hệ thống (`CustomerPhoneUniqueRule`).
  - **BR-21**: Xóa mềm khách hàng (`IsActive = false`). Tuyệt đối không xóa cứng khỏi cơ sở dữ liệu để bảo toàn lịch sử hóa đơn, thanh toán và điểm thưởng.
- **Xem hồ sơ chi tiết (Drawer Chi Tiết Khách Hàng)**:
  - Xem thông tin hạng thẻ, % chiết khấu đang áp dụng.
  - Xem lịch sử giao dịch điểm tích lũy (`PointTransaction`).
  - Xem lịch sử đơn đặt sân của khách hàng kèm trạng thái.
  - Thao tác khóa / mở khóa tài khoản khách hàng trực tiếp.

---

### 1.2. Đặt lịch cố định theo tuần (`FixedScheduleService` - BR-18, BR-19, A11)
- **Quy tắc sinh lịch 4 tuần tới (BR-18 & A11)**:
  - Khi khách hàng đăng ký lịch chơi cố định (chọn Thứ trong tuần, Khung giờ, Sân đấu):
  - Hệ thống tự động tính toán và sinh đủ booking cho **4 tuần tiếp theo** (`FixedWeeksAhead = 4` cấu hình động trong `ISettingService`).
  - Mỗi booking được tạo ra đều chạy qua toàn bộ ma trận thẩm định quy tắc **BR-01..06**:
    - **BR-01**: Bắt buộc mốc :00 hoặc :30, trong khung giờ mở cửa 06:00 - 24:00.
    - **BR-02**: Thời lượng từ 60 đến 240 phút.
    - **BR-03**: Chống trùng lịch (bỏ qua các đơn đã hủy `Cancelled`).
    - **BR-04**: Tuân thủ quy tắc khoảng trống 30 phút giữa các đơn đặt sân.
    - **BR-05**: Không đặt trong quá khứ.
  - **Cơ chế Bỏ qua tuần trùng & Báo cáo chi tiết**:
    - Nếu có tuần bị xung đột (trùng lịch hoặc vi phạm khoảng trống 30 phút), hệ thống **tự động bỏ qua tuần đó** và tiếp tục sinh booking cho các tuần còn lại.
    - Trả về danh sách chi tiết các tuần/ngày bị bỏ qua kèm lý do cụ thể (`SkippedDates` & `SkippedDetails`) để nhân viên tư vấn khách chuyển khung giờ hoặc đổi sân khác.
  - Booking sinh ra có loại `BookingType = Fixed`, gắn `FixedScheduleId`, tự động tính giá và chiết khấu hạng thẻ theo BR-07, BR-08, BR-09, BR-11.
- **Quy tắc hủy lịch cố định (BR-19)**:
  - Khi hủy lịch cố định (`IsActive = false`), hệ thống tự động quét và **hủy tất cả các booking tương lai chưa check-in** thuộc lịch cố định đó (`StartTime > DateTime.Now` và trạng thái `Pending` hoặc `Confirmed`).
  - Các booking trong quá khứ hoặc đã hoàn thành/đang chơi (`CheckedIn`, `Completed`) được bảo lưu nguyên vẹn để đối soát doanh thu.
- **Hộp thoại Đặt Lịch Cố Định (`FixedScheduleDialog`)**:
  - Giao diện thân thiện, chọn thứ 2 đến Chủ nhật, giờ bắt đầu, giờ kết thúc, sân đấu.
  - Hiển thị trực quan kết quả sinh lịch: số booking tạo thành công và danh sách các tuần bị bỏ qua kèm lý do vi phạm.

---

### 1.3. Khách vãng lai & Check-in Nhanh tại Quầy (`Quick Walk-in` - A12 & BR-05)
- **Đặc tả nghiệp vụ khách vãng lai (A12 & BR-05)**:
  - Dành cho khách đến đột xuất không đặt trước, nhân viên lễ tân xếp ngay vào sân đang trống tại thời điểm hiện tại.
  - **Khung giờ tự động làm tròn**: Giờ bắt đầu tự động làm tròn xuống mốc 30 phút gần nhất theo chuẩn BR-05 (Ví dụ: khách đến lúc `08:15` -> hệ thống tự động điền bắt đầu từ `08:00`).
  - Thời lượng mặc định gợi ý 60 phút (tối thiểu theo BR-02).
  - Không bắt buộc phải có tài khoản hoặc hồ sơ khách hàng trước đó; chỉ cần nhập tên khách hoặc để mặc định "Khách vãng lai".
  - Trạng thái ban đầu của đơn là **Đang chơi (`CheckedIn`) ngay lập tức**, không cần trải qua bước duyệt hay đặt cọc trước.
- **Tích hợp trên Giao diện Lịch sân (`SchedulePage` & `ScheduleViewModel`)**:
  - Nút bấm nổi bật **"Khách vãng lai"** với biểu tượng sấm sét (`⚡`) ngay trên thanh công cụ Lịch sân.
  - Khi bấm nút, hệ thống tự động quét sân đang trống, điền sẵn giờ chơi hiện tại, mở Drawer cho nhân viên bấm "Xác nhận nhận sân" chỉ với 1 click.
  - Khối booking hiển thị tức thì trên ma trận lịch với màu xanh lá `#10B981` (`CheckedIn`).

---

## 2. KẾT QUẢ KIỂM THỬ TỰ ĐỘNG (UNIT TESTS)

Toàn bộ **156/156 bài kiểm thử tự động (Unit Tests)** đều chạy thành công mỹ mãn (**100% Passed**, không có bất kỳ test nào thất bại hoặc bị bỏ qua).

```
Test run for BadmintonManager.Tests.dll (.NETCoreApp,Version=v8.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   156, Skipped:     0, Total:   156, Duration: 1 m 6 s
```

### 2.1. Phân bổ các nhóm bài kiểm thử trọng tâm Ngày 7:
1. **`FixedScheduleServiceTests.cs`** (5 bài tests chuyên sâu cho BR-18, BR-19):
   - `Day7_DoD_CreateFixedSchedule_Generates4Bookings_AndSkipsConflictingWeeks`: Kiểm chứng tạo lịch cố định sinh đủ 4 booking, phát hiện tuần trùng và báo chi tiết tuần bị bỏ qua.
   - `Day7_DoD_CancelFixedSchedule_CancelsAllFuturePendingAndConfirmedBookings`: Kiểm chứng hủy lịch cố định sẽ tự động hủy các booking tương lai chưa check-in theo BR-19.
   - `CreateFixedSchedule_WhenAllWeeksConflict_ThrowsBusinessException`: Kiểm chứng ném ngoại lệ khi tất cả các tuần đều bị trùng giờ.
   - `CreateFixedSchedule_WhenOutsideBranchAccess_ThrowsForbiddenException`: Kiểm chứng bảo mật phân quyền cơ sở khi tạo lịch cố định trái chi nhánh.
   - `CreateFixedSchedule_ValidatesDurationAndBookingRules`: Kiểm chứng kiểm tra thời lượng < 60 phút hoặc > 240 phút bị từ chối theo BR-02.

2. **`CustomerServiceTests.cs`** (6 bài tests chuyên sâu cho BR-20, BR-21):
   - `CreateCustomer_WhenPhoneAlreadyExists_ThrowsBusinessException_BR20`: Kiểm tra SĐT duy nhất toàn hệ thống.
   - `CreateCustomer_ValidData_CreatesSuccessfully`: Tạo mới khách hàng thành công.
   - `DeleteCustomer_PerformsSoftDelete_BR21`: Kiểm tra xóa mềm (chuyển `IsActive = false`, không xóa khỏi DB).
   - `GetCustomerDetailAsync_ReturnsProfileWithTransactionsAndBookings`: Lấy chi tiết hồ sơ, lịch sử điểm và booking.
   - `SearchCustomersAsync_FiltersByNameOrPhone`: Tìm kiếm linh hoạt theo từ khóa.
   - `UpdateCustomer_WhenPhoneConflicts_ThrowsBusinessException`: Kiểm tra cập nhật trùng SĐT với người khác bị chặn.

3. **`CustomerListViewModelTests.cs`** (6 bài tests UI MVVM):
   - `LoadCustomers_LoadsDataAndCalculatesKPIs`: Nạp danh sách và tính đúng các chỉ số KPI.
   - `SearchCustomers_FiltersList`: Lọc theo từ khóa tìm kiếm.
   - `SaveCustomer_WhenNew_AddsToDatabase`: Thêm mới khách hàng từ form Drawer.
   - `SaveCustomer_WhenEdit_UpdatesCustomer`: Chỉnh sửa thông tin khách hàng từ Drawer.
   - `ToggleCustomerStatus_TogglesActiveState`: Bật/tắt trạng thái hoạt động của khách hàng.
   - `OpenCustomerDetail_LoadsProfileAndPointHistory`: Mở Drawer xem chi tiết hồ sơ và lịch sử điểm thưởng.

4. **`ScheduleServiceTests.cs`** (Bổ sung 2 bài tests tích hợp DoD Ngày 7):
   - `Day7_DoD_ScheduleViewModel_QuickWalkIn_PrefillsNowAndCreatesCheckedInBooking`: Kiểm chứng luồng bấm "Khách vãng lai" -> tự làm tròn mốc 30 phút -> tạo booking ở trạng thái `CheckedIn` ngay lập tức.
   - `Day7_DoD_ScheduleViewModel_CancelFixedSchedule_CancelsFutureBookings`: Kiểm chứng thao tác bấm nút "Hủy Toàn Bộ Lịch Cố Định" trên Drawer chi tiết booking -> hủy thành công các lịch tương lai.

---

## 3. ĐỐI CHIẾU TIÊU CHUẨN NGHIỆM THU (DEFINITION OF DONE VERIFICATION)

| Tiêu chí DoD Ngày 7 | Hiện thực trong mã nguồn | Minh chứng kiểm thử tự động | Trạng thái |
|---|---|---|:---:|
| **1. Tạo lịch cố định sinh đủ 4 booking** | `FixedScheduleService.CreateFixedScheduleAsync` tính toán ngày 4 tuần tới (`FixedWeeksAhead = 4`), chạy qua BR-01..06, tạo các booking loại `Fixed`. | Test `Day7_DoD_CreateFixedSchedule_Generates4Bookings_AndSkipsConflictingWeeks` (PASSED) | **ĐẠT** |
| **2. Báo tuần trùng lịch chi tiết** | Nếu tuần nào vi phạm BR-03 (trùng lịch) hoặc BR-04 (khoảng trống 30p), tuần đó tự động được bỏ qua và lưu vào `SkippedDetails` giải thích rõ lý do. | Test `Day7_DoD_CreateFixedSchedule_Generates4Bookings_AndSkipsConflictingWeeks` (PASSED) | **ĐẠT** |
| **3. Khách vãng lai check-in được ngay** | `ScheduleViewModel.OpenQuickWalkInAsync` tự động làm tròn thời gian hiện tại xuống mốc :00/:30 (BR-05), chọn sân trống và khởi tạo đơn với trạng thái `CheckedIn` (A12). | Test `Day7_DoD_ScheduleViewModel_QuickWalkIn_PrefillsNowAndCreatesCheckedInBooking` (PASSED) | **ĐẠT** |
| **4. Hủy lịch cố định tương lai (BR-19)** | `FixedScheduleService.CancelFixedScheduleAsync` và nút "Hủy Toàn Bộ Lịch Cố Định" trên UI tự động hủy tất cả booking tương lai chưa check-in. | Test `Day7_DoD_CancelFixedSchedule_CancelsAllFuturePendingAndConfirmedBookings` & `Day7_DoD_ScheduleViewModel_CancelFixedSchedule_CancelsFutureBookings` (PASSED) | **ĐẠT** |
| **5. Quản lý khách hàng (BR-20, BR-21)** | SĐT duy nhất toàn hệ thống (`BR-20`), xóa mềm giữ lịch sử giao dịch (`BR-21`), phân loại thẻ và quản lý điểm. | 12 bài tests trong `CustomerServiceTests` & `CustomerListViewModelTests` (PASSED) | **ĐẠT** |

---

## 4. KẾT LUẬN

Mọi hạng mục của **Ngày 7** đã được triển khai đầy đủ, chuẩn xác theo yêu cầu đặc tả dự án, đảm bảo kiến trúc sạch sẽ, bảo mật cơ sở nhiều chi nhánh, giao diện người dùng trực quan, thân thiện và 100% kiểm thử tự động thành công. Hệ thống đã sẵn sàng bước vào Ngày 8 (Mô-đun Dịch vụ đi kèm, F&B, Thuê dụng cụ & Thanh toán tổng hợp).
