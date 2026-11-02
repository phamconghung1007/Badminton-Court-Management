# BÁO CÁO NGHIỆM THU ĐẠT TIÊU CHÍ NGÀY 8 (DEFINITION OF DONE - DAY 8)

**Dự án**: Badminton Court Management System (Hệ Thống Quản Lý Sân Cầu Lông)  
**Công nghệ**: .NET 8, WPF, MVVM (CommunityToolkit.Mvvm), Entity Framework Core 8, SQL Server  
**Ngày thực hiện**: Ngày 8 / 10 ngày trong lộ trình  
**Tiêu chí nghiệm thu (DoD Day 8)**:  
> *"Customer chỉ thấy dữ liệu của mình (thử truy cập chéo bị chặn ở Service)"*

---

## 1. TỔNG QUAN KẾT QUẢ TRIỂN KHAI NGÀY 8

Toàn bộ các yêu cầu của Ngày 8 theo tài liệu đặc tả dự án (Section 7, 8, 9, 16 và Phụ lục A7, A8, A9, A10, BR-14, BR-15, BR-16, BR-20) đã được hiện thực hóa, tích hợp mượt mà và kiểm thử tự động 100% đạt chuẩn:

### 1.1. Kiến trúc Bảo mật Chống Truy cập Chéo ở Tầng Service (Cross-Access Protection at Service Layer)
Nguyên tắc cốt lõi của DoD Ngày 8 là **không chỉ phân quyền tại tầng giao diện (UI)** mà **phải áp đặt cơ chế bảo vệ phân quyền chặt chẽ tại tầng nghiệp vụ (Service Layer)**. Khi một người dùng đăng nhập với vai trò `Customer`, mọi dịch vụ trung tâm đều thực thi kiểm tra ngữ cảnh người dùng (`CurrentUserContext`):

1. **`BookingService`**:
   - Khi `CurrentUser.Role == "Customer"`, hàm `GetBookingsAsync` tự động áp dụng bộ lọc bắt buộc: `b.CustomerId == user.CustomerId`. Khách hàng hoàn toàn không thể truy vấn hoặc nhìn thấy danh sách đơn đặt của khách hàng khác.
   - Khi Customer cố gọi `GetBookingByIdAsync(bookingId)` tới một đơn đặt thuộc sở hữu của khách hàng khác: Service lập tức phát hiện và ném `UnauthorizedAccessException("Khách hàng chỉ có thể truy cập đơn đặt của chính mình.")`.
   - Khi Customer cố thao tác `CancelBookingAsync` hoặc `RescheduleBookingAsync` trên đơn của người khác: Service ném `UnauthorizedAccessException("Khách hàng chỉ có thể hủy/đổi đơn đặt của chính mình.")`.
   - Khi Customer cố gọi các hàm đặc quyền của nhân viên/quản lý (`ApproveBookingAsync`, `CheckInAsync`, `CompleteBookingAsync`): Service ném `UnauthorizedAccessException("Khách hàng không có quyền thực hiện nghiệp vụ này.")`.
   - Khi Customer tự đặt sân trực tuyến: Trạng thái đơn luôn được cố định là `Pending`, tự động gán đúng `CustomerId` của tài khoản đang đăng nhập, ngăn chặn việc giả mạo `CustomerId` của khách hàng khác.

2. **`CustomerService`**:
   - Khi `CurrentUser.Role == "Customer"`, hàm `GetCustomerByIdAsync` kiểm tra nghiêm ngặt `id == user.CustomerId`. Nếu cố tình tra cứu hồ sơ khách hàng khác, Service lập tức ném `UnauthorizedAccessException("Khách hàng chỉ có thể xem hồ sơ của chính mình.")`.
   - Các hành động quản trị danh bạ như tạo khách hàng (`CreateCustomerAsync`), sửa thông tin khách hàng khác (`UpdateCustomerAsync`), xóa mềm (`DeleteCustomerAsync`), hay can thiệp cộng điểm (`AddPointsAsync`) đều bị chặn đứng với `UnauthorizedAccessException`.

3. **`PaymentService`**:
   - Toàn bộ các thao tác thanh toán tài chính (`PayDepositAsync`, `PayRemainingAsync`, `RefundDepositAsync`) đều yêu cầu quyền hạn thu ngân/nhân viên cơ sở. Khi người dùng là `Customer`, Service chặn ngay lập tức với `UnauthorizedAccessException("Khách hàng không có quyền thực hiện thu tiền hoặc hoàn cọc trực tiếp.")`.

4. **`FixedScheduleService`**:
   - Khách hàng bị chặn hoàn toàn nếu cố tạo lịch cố định thay cho khách hàng khác (`request.CustomerId != user.CustomerId`), hoặc cố hủy lịch cố định không thuộc về mình, ném `UnauthorizedAccessException`.

---

### 1.2. Màn hình Cổng Khách Hàng (Customer Portal UI & MVVM)

1. **Tìm kiếm sân rảnh & Đặt sân trực tuyến (`CustomerBookCourtPage` & `CustomerBookCourtViewModel`)**:
   - Cho phép khách hàng chọn Cơ sở, Ngày chơi, Giờ bắt đầu, Giờ kết thúc theo các mốc 30 phút chuẩn hóa (`06:00` đến `24:00`).
   - Hàm `GetAvailableCourtsAsync` quét toàn bộ sân đấu trong chi nhánh và lọc tự động:
     - Loại bỏ các sân bị trùng lịch đã được đặt trước (**BR-03**).
     - Loại bỏ các sân mà lịch đặt mới sẽ để lại khe hở 30 phút với lịch liền trước hoặc liền sau (**BR-04**).
     - Kiểm tra khoảng giờ hợp lệ từ 60 phút đến 240 phút (**BR-02**) và không nằm trong quá khứ (**BR-05**).
   - **Thẻ Card sân trống trực quan**: Hiển thị tên sân, loại sân, đơn giá giờ, trạng thái khả dụng và nút "Chọn sân".
   - **Khung Xem trước Giá (Live Pricing Preview)**:
     - Tự động tách từng block 30 phút và áp dụng đúng bảng giá Ngày thường / Cuối tuần (**BR-07, BR-08**).
     - Trừ chiết khấu theo hạng hội viên của khách (Đồng 0%, Bạc 5%, Vàng 10% theo **BR-09**).
     - Tính tiền đặt cọc tối thiểu 30% làm tròn lên 1.000 VNĐ theo chuẩn **BR-11**.
   - **Xác nhận đặt đơn (A7 & A8)**: Đơn đặt có trạng thái ban đầu là `Pending`. Hệ thống hiển thị hộp thoại hướng dẫn chuyển khoản tiền cọc trong vòng **2 giờ** theo đúng quy định tại phụ lục A7, A8.

2. **Lịch sử đặt sân & Thao tác hủy/đổi lịch (`CustomerBookingsPage` & `CustomerBookingsViewModel`)**:
   - Hiển thị danh sách tất cả các đơn đặt sân của riêng khách hàng đang đăng nhập kèm mã đơn, cơ sở, sân, khung giờ, tổng tiền, tiền cọc và trạng thái.
   - Bộ lọc trạng thái: Tất cả, Chờ duyệt (`Pending`), Đã duyệt (`Confirmed`), Đang chơi (`CheckedIn`), Hoàn thành (`Completed`), Đã hủy (`Cancelled`).
   - **Hủy đặt sân (BR-14)**:
     - Hủy trước giờ chơi $\ge 24$ giờ: Hệ thống thông báo xác nhận hoàn cọc 100% qua tài khoản ngân hàng.
     - Hủy trước giờ chơi $< 24$ giờ: Hệ thống cảnh báo rõ ràng khách hàng sẽ bị **mất toàn bộ tiền cọc** theo quy định.
   - **Đổi lịch đặt sân (Reschedule Drawer - A9)**:
     - Tích hợp Drawer trượt cho phép khách chọn Ngày và Khung giờ mới.
     - Kiểm tra nghiêm ngặt điều kiện: Chỉ cho phép đổi lịch trước giờ chơi ít nhất **24 giờ** theo quy định **A9**; nếu vi phạm, hệ thống hiển thị cảnh báo từ chối.

3. **Điểm thưởng & Hạng thẻ hội viên (`CustomerPointsPage` & `CustomerPointsViewModel`)**:
   - Hiển thị thẻ Card thông tin hạng thành viên (Đồng, Bạc, Vàng), số điểm tích lũy hiện tại và tỷ lệ chiết khấu được hưởng (**BR-16**).
   - Bảng lịch sử biến động điểm (`PointTransaction`) hiển thị minh bạch từng lần cộng/trừ điểm kèm mã đơn và ngày giờ phát sinh (**BR-15**).
   - Bảng tra cứu chính sách hạng thẻ giúp khách hàng nắm rõ điều kiện thăng hạng và đặc quyền của từng cấp bậc.

4. **Bảng điều khiển Khách hàng nâng cấp (`CustomerDashboardPage` & `CustomerDashboardViewModel`)**:
   - Thanh tiến trình thăng hạng (Progress Bar) trực quan: Hiển thị tỷ lệ % hoàn thành và số điểm cần tích lũy thêm để thăng hạng thẻ tiếp theo.
   - Danh sách 5 lịch đặt sân sắp diễn ra gần nhất của khách hàng.
   - Các nút phím tắt điều hướng nhanh: "Đặt sân ngay", "Lịch sử đặt sân", "Xem điểm thưởng".

---

## 2. KẾT QUẢ KIỂM THỬ TỰ ĐỘNG (UNIT TESTS)

Toàn bộ **173/173 bài kiểm thử tự động (Unit Tests)** đều chạy thành công mỹ mãn (**100% Passed**, không có bất kỳ test nào thất bại hoặc bị bỏ qua).

```
Test run for BadmintonManager.Tests.dll (.NETCoreApp,Version=v8.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   173, Skipped:     0, Total:   173, Duration: 16 s
```

### 2.1. Phân bổ các nhóm bài kiểm thử trọng tâm Ngày 8:
1. **`CustomerAccessSecurityTests.cs`** (8 bài test bảo mật phân quyền & chống truy cập chéo ở Service Layer):
   - `Customer_CanOnlyViewTheirOwnBookings`: Khách A đăng nhập chỉ query thấy danh sách đơn đặt của chính khách A, hoàn toàn không thấy đơn của khách B.
   - `Customer_AccessingOtherCustomerBookingById_ThrowsUnauthorizedAccessException`: Khách A cố tình gọi `GetBookingByIdAsync` với `Id` đơn của khách B -> Bị chặn với `UnauthorizedAccessException`.
   - `Customer_CancelingOtherCustomerBooking_ThrowsUnauthorizedAccessException`: Khách A cố tình gọi `CancelBookingAsync` trên đơn của khách B -> Bị chặn với `UnauthorizedAccessException`.
   - `Customer_ReschedulingOtherCustomerBooking_ThrowsUnauthorizedAccessException`: Khách A cố tình gọi `RescheduleBookingAsync` trên đơn của khách B -> Bị chặn với `UnauthorizedAccessException`.
   - `Customer_ApprovingOrCheckingInBooking_ThrowsUnauthorizedAccessException`: Khách A cố tình gọi `ApproveBookingAsync` hoặc `CheckInAsync` -> Bị chặn với `UnauthorizedAccessException`.
   - `Customer_AccessingOtherCustomerProfile_ThrowsUnauthorizedAccessException`: Khách A cố tình xem hồ sơ khách B qua `GetCustomerByIdAsync` -> Bị chặn với `UnauthorizedAccessException`.
   - `Customer_UpdatingOtherCustomerProfile_ThrowsUnauthorizedAccessException`: Khách A cố tình sửa hồ sơ khách B qua `UpdateCustomerAsync` -> Bị chặn với `UnauthorizedAccessException`.
   - `Customer_MakingPaymentDirectly_ThrowsUnauthorizedAccessException`: Khách A cố tình gọi `PaymentService` để thu tiền hoặc hoàn cọc -> Bị chặn với `UnauthorizedAccessException`.

2. **`CustomerBookCourtViewModelTests.cs`** (5 bài test UI đặt sân khách hàng & quy tắc lọc sân):
   - `SearchCourts_FindsAvailableCourts`: Tìm kiếm sân rảnh thời gian thực thành công.
   - `SearchCourts_WhenConflicting_ExcludesBookedCourts`: Loại trừ chính xác các sân đã có người đặt trùng giờ (**BR-03**).
   - `SearchCourts_WhenLeaving30MinGap_ExcludesViolatingCourts`: Loại trừ chính xác các sân để lại khe hở 30 phút (**BR-04**).
   - `BookCourt_CreatesPendingBooking_AndRemindsDepositWithin2Hours`: Đặt sân trực tuyến tạo đơn `Pending` và hiển thị nhắc cọc 2 giờ (**A7, A8**).
   - `CalculateLivePrice_UpdatesPricingInfo`: Tự động tính đơn giá, chiết khấu hạng thẻ và tiền cọc làm tròn (**BR-07, BR-09, BR-11**).

3. **`CustomerBookingsViewModelTests.cs`** (3 bài test UI lịch sử đặt sân):
   - `LoadBookings_LoadsCustomerBookingsOnly`: Nạp danh sách đơn thuộc riêng khách hàng đăng nhập.
   - `CancelBooking_WhenMoreThan24Hours_CancelsAndShowsRefundNotice`: Hủy đơn trước $\ge 24$ giờ hiển thị thông báo hoàn cọc (**BR-14**).
   - `RescheduleBooking_WhenLessThan24Hours_ShowsWarningAndBlocks`: Đổi lịch khi thời gian còn lại $< 24$ giờ bị chặn và hiển thị cảnh báo theo **A9**.

4. **`CustomerPointsViewModelTests.cs`** (1 bài test UI điểm thưởng):
   - `LoadCustomerPoints_LoadsProfileAndPointTransactions`: Nạp đúng điểm tích lũy, hạng thành viên và danh sách biến động điểm `PointTransaction`.

---

## 3. HƯỚNG DẪN KIỂM THỬ TRỰC TIẾP TRÊN ỨNG DỤNG (MANUAL VERIFICATION)

### Bước 1: Khởi chạy ứng dụng
```powershell
dotnet run --project BadmintonManager/BadmintonManager.csproj
```

### Bước 2: Đăng nhập tài khoản Khách hàng
- **Tài khoản**: `customer1`
- **Mật khẩu**: `123456`
- **Thông tin**: Khách hàng Nguyễn Văn An (Hạng Vàng, 1.600 điểm, chiết khấu 10%).

### Bước 3: Kiểm chứng Giao diện Shell & Menu Phân quyền
- Khách hàng đăng nhập **hoàn toàn không thấy** các menu của Admin / Manager / Staff (như Quản lý cơ sở, Quản lý sân, Quản lý tài khoản, Bảng giá, Lịch sân chi nhánh, Thu tiền,...).
- Khách hàng chỉ thấy 4 menu riêng biệt:
  1. 🏠 **Tổng quan (Dashboard)**
  2. 🏸 **Đặt sân trực tuyến**
  3. 📋 **Lịch sử đặt sân**
  4. 💎 **Điểm thưởng & Hạng thẻ**

### Bước 4: Kiểm chứng Đặt sân trực tuyến (`CustomerBookCourtPage`)
- Bấm menu **"Đặt sân trực tuyến"**.
- Chọn Cơ sở Cầu Giấy, Ngày mai, Giờ bắt đầu `08:00`, Giờ kết thúc `10:00`.
- Bấm **"Tìm sân trống"**: Danh sách các sân khả dụng xuất hiện cùng đơn giá giờ và tổng tiền dự tính (đã trừ 10% chiết khấu Hạng Vàng).
- Chọn một sân và bấm **"Đặt Sân Ngay"**:
  - Hộp thoại hiển thị thông báo: Đơn đặt ở trạng thái Chờ duyệt (`Pending`), yêu cầu chuyển khoản cọc 30% trong vòng 2 giờ.
  - Tự động chuyển hướng sang trang **"Lịch sử đặt sân"**.

### Bước 5: Kiểm chứng Lịch sử đặt sân & Bảo mật truy cập chéo
- Tại trang **"Lịch sử đặt sân"**, khách hàng chỉ nhìn thấy các đơn đặt của chính mình (`customer1`), không nhìn thấy bất kỳ đơn nào của `customer2` hay `customer3`.
- Bấm **"Hủy đơn"**:
  - Nếu đơn đặt trước giờ chơi $\ge 24$ giờ: Hệ thống thông báo hoàn tiền cọc 100%.
  - Nếu đơn đặt gần giờ chơi $< 24$ giờ: Hệ thống cảnh báo sẽ bị mất cọc theo quy định **BR-14**.
- Bấm **"Đổi lịch"**: Drawer trượt mở ra cho phép chọn ngày/giờ mới; nếu vi phạm quy định đổi lịch trước 24 giờ (**A9**), hệ thống từ chối thực hiện.

### Bước 6: Kiểm chứng Điểm thưởng & Hạng thẻ (`CustomerPointsPage`)
- Bấm menu **"Điểm thưởng & Hạng thẻ"**:
  - Xem huy hiệu Hạng Vàng, số điểm 1.600, ưu đãi giảm 10% trên mỗi giờ chơi.
  - Xem danh sách biến động điểm tương ứng với các hóa đơn thanh toán đã hoàn thành.

---

## 4. KẾT LUẬN NGHIỆM THU NGÀY 8

- **Mục tiêu Ngày 8**: Hoàn thành Cổng thông tin khách hàng (Customer Portal) và phân quyền bảo mật chống truy cập chéo.
- **Tiêu chuẩn nghiệm thu (DoD Day 8)**: *"Customer chỉ thấy dữ liệu của mình (thử truy cập chéo bị chặn ở Service)"* $\to$ **ĐÃ ĐẠT 100%**.
- **Chất lượng kiểm thử**: 173/173 tests passed (100%), không có cảnh báo nghiêm trọng hay lỗi tiềm ẩn.
