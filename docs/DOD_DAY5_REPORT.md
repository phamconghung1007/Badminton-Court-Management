# BÁO CÁO NGHIỆM THU ĐẠT TIÊU CHÍ NGÀY 5 (DEFINITION OF DONE - DAY 5)

**Dự án**: Badminton Court Management System (Hệ Thống Quản Lý Sân Cầu Lông)  
**Công nghệ**: .NET 8, WPF, MVVM (CommunityToolkit.Mvvm), Entity Framework Core 8, SQL Server  
**Ngày thực hiện**: Ngày 5 / 10 ngày trong lộ trình  
**Tiêu chí nghiệm thu (DoD Day 5)**:  
> *"Staff đặt được trên lịch, thấy khối booking đúng chỗ; đặt trùng bị chặn với thông báo tiếng Việt"*

---

## 1. TỔNG QUAN KẾT QUẢ ĐẠT ĐƯỢC

Toàn bộ các yêu cầu của Ngày 5 theo tài liệu đặc tả dự án (Section 7, 8, 9, 13 và Phụ lục A1, A2, A7, A12) đã được triển khai hoàn chỉnh:

1. **Ma trận Lịch sân (Schedule Grid)**:
   - 36 khung thời gian cố định 30 phút từ `06:00` đến `24:00` (theo A1, A2).
   - $N$ cột sân tương ứng với số lượng sân của từng chi nhánh (6 sân mỗi chi nhánh).
   - Tự động tính toán vị trí `RowIndex`, `RowSpan`, `ColumnIndex` và tọa độ pixel `(X, Y, Width, Height)` trên Canvas để hiển thị mượt mà.
   - Bảng màu trạng thái chuẩn UI/UX:
     - 🟡 **Chờ duyệt (Pending)**: `#F59E0B`
     - 🔵 **Đã duyệt (Confirmed)**: `#2563EB`
     - 🟢 **Đang chơi (Checked-in)**: `#10B981`
     - ⚫ **Hoàn thành (Completed)**: `#6B7280`
     - 🔴 **Vắng mặt (No-show)**: `#EF4444`
     - ⚪ **Ô trống**: `#F9FAFB` (Hiệu ứng hover xanh nhạt `#F0FDF4` với biểu tượng `+`).

2. **Form Đặt Sân Tích Hợp (Create Booking Drawer)**:
   - Click ô trống trên lịch tự động prefill sân và khung giờ tương ứng.
   - Hỗ trợ cả 2 luồng: **Khách vãng lai** (Walk-in A12) và **Hội viên** (Customer search).
   - **Live Pricing Preview**: Tự động tính lại giá tức thì khi thay đổi giờ hoặc chọn hội viên (tính giá theo từng ô 30 phút BR-07, chiết khấu hạng thẻ BR-09, tiền cọc 30% làm tròn lên bội số 1.000 VNĐ BR-11).
   - Bắt lỗi tiếng Việt thân thiện:
     - Trùng lịch (BR-03): *"Khung giờ này đã có người đặt trên sân... Vui lòng chọn khung giờ hoặc sân khác."*
     - Vi phạm khoảng trống 30 phút (BR-04): *"Quy tắc khoảng trống: Không được để lại khoảng trống đúng 30 phút giữa các lịch đặt..."*
     - Đặt trong quá khứ (BR-05): *"Không thể đặt sân trong quá khứ."*
     - Sai mốc thời gian (BR-01): *"Thời gian bắt đầu và kết thúc phải đúng mốc 00 hoặc 30 phút."*

3. **Drawer Chi Tiết Booking (Booking Details Drawer)**:
   - Hiển thị đầy đủ thông tin: Mã đơn `#BK...`, Sân, Chi nhánh, Thời gian, Thông tin khách hàng, Tổng tiền, Giảm giá, Đã cọc, Đã thanh toán.
   - Nút hành động trực tiếp cho nhân viên ca trực:
     - **Duyệt đơn** (`Confirm`): Chuyển trạng thái sang `Confirmed`.
     - **Check-in vào sân** (`Check-in`): Chuyển trạng thái sang `CheckedIn`.
     - **Hủy đơn** (`Cancel`): Xác nhận với cảnh báo hoàn cọc trước 24 giờ.

4. **Trang Danh Sách Đặt Sân (BookingListPage)**:
   - Thống kê KPI: Tổng đơn, Chờ duyệt, Đã duyệt, Đang chơi, Doanh số hiện tại.
   - Bộ lọc linh hoạt: Lọc theo Ngày, Trạng thái, Sân và Ô tìm kiếm từ khóa.
   - DataGrid chuyên nghiệp với badge màu trạng thái và các nút duyệt/check-in/hủy nhanh.

---

## 2. MINH CHỨNG KIỂM THỬ TỰ ĐỘNG (AUTOMATED TEST VERIFICATION)

Bộ kiểm thử tự động xUnit bao gồm **126 tests** (tăng thêm 20 tests trong Ngày 5), 100% vượt qua thành công:

### Danh mục Unit Test Trọng Tâm Ngày 5:
| STT | Tên Test Case | Mục đích kiểm tra | Kết quả |
|:---:|:---|:---|:---:|
| 1 | `ScheduleViewModel_DoD_StaffCreatesBookingOnSchedule_AppearsOnGridCorrectly` | **[DoD]** Staff đặt sân trên lịch, đơn tạo thành công và xuất hiện đúng vị trí hàng/cột trên ma trận lịch. | **PASS** |
| 2 | `ScheduleViewModel_DoD_StaffCreatesOverlappingBooking_BlockedWithVietnameseMessage` | **[DoD]** Staff đặt trùng giờ bị hệ thống chặn tức thì với thông báo tiếng Việt chuẩn BR-03. | **PASS** |
| 3 | `ScheduleViewModel_OpenCreateBookingForm_FromSlot_PrefillsCourtAndTime` | Click ô trống tự động điền đúng Sân, Giờ bắt đầu và Giờ kết thúc vào form đặt. | **PASS** |
| 4 | `ScheduleViewModel_RecalculatePricePreview_WithGoldCustomer_AppliesDiscount` | Chọn khách hàng Hạng Vàng tự động chiết khấu 10% trên Live Pricing Card. | **PASS** |
| 5 | `ScheduleViewModel_OpenBookingDetails_ShowsBookingInformation` | Click vào khối booking mở Drawer chi tiết với đầy đủ thông tin tài chính và giờ chơi. | **PASS** |
| 6 | `ScheduleMatrixHelper_GenerateTimeSlots_ShouldReturn36Slots` | Sinh chính xác 36 time slots từ 06:00 đến 24:00 (bước 30 phút). | **PASS** |
| 7 | `ScheduleMatrixHelper_CalculateRowIndexAndSpan_Standard1Hour_ReturnsCorrectRowAndSpan` | Tính toán chính xác `RowIndex` và `RowSpan` cho đơn 60 phút. | **PASS** |
| 8 | `ScheduleMatrixHelper_BuildBookingBlock_MapsColorsByStatus` | Ánh xạ chính xác mã màu nền/viền cho 5 trạng thái theo quy định. | **PASS** |
| 9 | `ScheduleMatrixHelper_BuildEmptySlots_GeneratesAllRemainingSlots` | Sinh danh sách ô trống chính xác (36 slots $\times$ 6 sân trừ đi các slots đã có booking). | **PASS** |
| 10 | `BookingListViewModel_InitializeAsync_LoadsCourtsAndDefaultBookings` | Màn hình danh sách nạp đầy đủ bộ lọc sân và danh sách đơn đặt sân của cơ sở. | **PASS** |
| 11 | `BookingListViewModel_SearchKeyword_FiltersByCustomerOrCode` | Tìm kiếm chính xác theo mã đơn hoặc tên khách hàng. | **PASS** |
| 12 | `BookingListViewModel_ConfirmAndCheckInBooking_WorkflowSucceeds` | Luồng Duyệt đơn (Pending $\to$ Confirmed) và Check-in (Confirmed $\to$ CheckedIn) hoạt động hoàn hảo. | **PASS** |
| 13 | `BookingListViewModel_CancelBooking_SetsStatusToCancelled` | Hủy đơn thành công và chuyển trạng thái sang `Cancelled`. | **PASS** |
| 14 | `ShellViewModel_Staff_NavigatesToBookingList_ResolvesBookingListViewModel` | Điều hướng menu Staff sang danh sách booking resolve đúng ViewModel. | **PASS** |

### Kết quả chạy kiểm thử toàn hệ thống:
```text
Passed!  - Failed: 0, Passed: 126, Skipped: 0, Total: 126, Duration: 25 s - BadmintonManager.Tests.dll (net8.0)
```

---

## 3. KẾT LUẬN

Tiêu chí nghiệm thu Ngày 5 (**Definition of Done - Day 5**) đã được **hoàn thành xuất sắc 100%**:
- Lễ tân (Staff) đặt được trên lịch, khối booking xuất hiện đúng ô và trải dài chuẩn xác.
- Đặt trùng bị chặn tức thì với thông báo tiếng Việt theo quy chuẩn BR-03.
- Toàn bộ kiến trúc tuân thủ nghiêm ngặt MVVM, Dependency Injection, EF Core Code First và Material Design.
