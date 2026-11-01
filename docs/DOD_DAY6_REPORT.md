# BÁO CÁO NGHIỆM THU ĐẠT TIÊU CHÍ NGÀY 6 (DEFINITION OF DONE - DAY 6)

**Dự án**: Badminton Court Management System (Hệ Thống Quản Lý Sân Cầu Lông)  
**Công nghệ**: .NET 8, WPF, MVVM (CommunityToolkit.Mvvm), Entity Framework Core 8, SQL Server  
**Ngày thực hiện**: Ngày 6 / 10 ngày trong lộ trình  
**Tiêu chí nghiệm thu (DoD Day 6)**:  
> *"Chạy trọn vòng đời 1 booking từ Pending đến Completed; hóa đơn in/preview ra đúng số"*

---

## 1. TỔNG QUAN KẾT QUẢ TRIỂN KHAI NGÀY 6

Toàn bộ các yêu cầu của Ngày 6 theo tài liệu đặc tả dự án (Section 7, 8, 9, 13, 16 và Phụ lục A7, A8, A9, A12) đã được hiện thực hóa và kiểm thử tự động hoàn tất:

### 1.1. Vòng đời toàn vẹn của Booking (Full Booking Lifecycle)
Hệ thống quản lý chặt chẽ trạng thái của đơn đặt sân qua các bước nghiệp vụ rõ ràng:
1. **Khởi tạo (`Pending`)**: Đơn đặt sân được tạo (khách đặt trực tuyến hoặc tại quầy nhưng chưa cọc).
2. **Xác nhận (`Confirmed`)**: 
   - Sau khi thu tiền cọc tối thiểu 30% (làm tròn lên bội số 1.000 VNĐ theo BR-11).
   - `PaymentService` tự động chuyển trạng thái đơn từ `Pending` sang `Confirmed` ngay khi ghi nhận khoản cọc.
3. **Điều chỉnh giờ / đổi sân (`Edit Booking`)**:
   - Nhân viên có thể linh hoạt đổi sân hoặc điều chỉnh giờ chơi thông qua `EditBookingDialog`.
   - Tính toán lại giá thời gian thực (`Live Price Recalculation`), áp dụng đúng % chiết khấu hội viên.
4. **Nhận sân (`CheckedIn`)**:
   - Khi khách đến cơ sở vào giờ chơi, nhân viên thực hiện thao tác Check-in trên ma trận lịch hoặc danh sách booking.
   - Đơn chuyển sang trạng thái đang chơi (`CheckedIn`), khối booking trên ma trận đổi sang màu xanh lá `#10B981`.
5. **Thanh toán nốt & Hoàn tất (`Completed`)**:
   - Khách trả sân, nhân viên mở `PaymentDialog` thu phần tiền còn lại (`RemainingAmount`).
   - Khi tổng thanh toán $\ge$ `FinalAmount`, hệ thống tự động:
     - Cập nhật trạng thái đơn thành `Completed` (BR-13).
     - Tích điểm hội viên tương ứng (BR-15): $\lfloor \text{FinalAmount} / 10.000 \rfloor$.
     - Tự động đánh giá và nâng hạng thẻ hội viên (BR-16) nếu tổng điểm đạt ngưỡng.
     - Toàn bộ quá trình diễn ra trong cùng một Transaction an toàn dữ liệu.

---

### 1.2. Nghiệp vụ Thanh toán đa hình thức & Phân quyền cơ sở (BR-11, BR-12, BR-13, BR-14)
- **Phương thức thanh toán**: Hỗ trợ Tiền mặt (`Cash`) và Chuyển khoản (`BankTransfer`).
- **Phân loại thanh toán (`PaymentType`)**:
  - `Deposit` (Đặt cọc): Tự động chuyển `Pending` $\to$ `Confirmed`.
  - `RemainingBalance` (Thu nốt khi check-out): Tự động chuyển sang `Completed` khi thanh toán đủ.
  - `FullPayment` (Thanh toán 1 lần toàn bộ): Hoàn tất trực tiếp đơn.
  - `Refund` (Hoàn cọc): Tự động tạo bản ghi hoàn tiền khi hủy đơn trước 24 giờ (BR-14).
- **Kiểm soát bảo mật & Phân quyền cơ sở**: Quản lý / Nhân viên chỉ có quyền thu tiền và xem thanh toán của các booking thuộc chi nhánh mình phụ trách; hành vi gian lận cơ sở bị chặn ngay tại tầng Service bằng `ForbiddenException`.

---

### 1.3. Tích điểm & Nâng hạng thẻ Hội viên tự động (BR-15, BR-16)
- **Tỷ lệ tích điểm**: Mỗi 10.000 VNĐ trên số tiền thanh toán cuối cùng (`FinalAmount`) tương ứng 1 điểm thưởng (`floor(FinalAmount / 10.000)`).
- **Lưu vết giao dịch điểm (`PointTransaction`)**: Ghi nhận chi tiết số điểm thay đổi, số dư điểm sau giao dịch, mã đơn đặt sân liên quan và thời gian thực hiện.
- **Tự động thăng hạng (`Auto-upgrade MembershipTier`)**:
  - Truy vấn danh sách các hạng thẻ đang kích hoạt (`IsActive = true`) sắp xếp theo `MinPoints` giảm dần.
  - Tự động nâng hạng thẻ mới nếu điểm tích lũy của khách hàng $\ge$ ngưỡng yêu cầu (Ví dụ: Từ hạng Bạc lên hạng Vàng).
  - Khách hàng không bị tụt hạng khi điểm chưa bị trừ.

---

### 1.4. In hóa đơn & Xem trước hóa đơn (`FlowDocument` + `PrintDialog`)
- **Tạo tài liệu in chuẩn (`InvoicePrinter.BuildDocument`)**:
  - Thiết kế `FlowDocument` chuyên nghiệp khổ in tiêu chuẩn 80mm / A5:
    - Tiêu đề thương hiệu hệ thống và thông tin chi nhánh (Tên, Địa chỉ, Số điện thoại).
    - Mã hóa đơn chuẩn hóa: `INV-yyyyMMdd-seq`.
    - Thông tin khách hàng, số điện thoại, hạng hội viên.
    - Bảng chi tiết dịch vụ: Tên sân, khung giờ chơi, đơn giá, thành tiền.
    - Tổng tiền sân, chiết khấu hạng thẻ, số tiền đã cọc, số tiền thanh toán đợt này và dư nợ còn lại (0 đ nếu đã hoàn tất).
    - Lời cảm ơn và mã vạch / lời chúc kết thúc.
- **Xem trước tương tác (`InvoicePreviewDialog`)**:
  - Sử dụng `FlowDocumentScrollViewer` tích hợp sẵn trong WPF để nhân viên duyệt trước hóa đơn.
  - Tích hợp nút **In hóa đơn** (`PrintDialog`) để chuyển lệnh in trực tiếp tới máy in hóa đơn nhiệt hoặc máy in văn phòng.

---

## 2. MINH CHỨNG KIỂM THỬ TỰ ĐỘNG (AUTOMATED TESTS VERIFICATION)

Toàn bộ **136/136 bài unit test** trong Solution đã vượt qua thành công 100%:

### 2.1. Danh mục Unit Test Trọng Tâm Ngày 6:
| STT | Tên Test Case | Mục đích kiểm tra | Kết quả |
|:---:|:---|:---|:---:|
| 1 | `BookingLifecycle_FullJourney_FromPendingToCompleted_WithMembershipPointsAndInvoice` | **[DoD Trọng Tâm]** Kiểm thử toàn bộ vòng đời: Khách đặt `Pending` $\to$ Cọc chuyển sang `Confirmed` $\to$ Đổi giờ sân $\to$ Check-in $\to$ Thanh toán nốt đủ chuyển sang `Completed` $\to$ Tích điểm $\to$ Lên hạng thẻ $\to$ Sinh hóa đơn in đúng 100% số tiền. | **PASS** |
| 2 | `ProcessPayment_Deposit_ChangesStatusToConfirmed_CalculatesRemaining` | Thanh toán cọc thành công tự động đổi trạng thái đơn từ `Pending` sang `Confirmed` và tính đúng số tiền còn nợ. | **PASS** |
| 3 | `ProcessPayment_RemainingBalance_ChangesStatusToCompleted_WhenFullyPaid` | Thu nốt tiền còn thiếu thành công, đơn tự động hoàn tất `Completed`. | **PASS** |
| 4 | `ProcessPayment_CompletedBooking_AwardsPointsAndAutoUpgradesTier` | Hoàn tất đơn tự động tích điểm theo BR-15 ($10.000\text{ đ} = 1\text{ điểm}$) và nâng hạng khách hàng từ Bạc lên Vàng (BR-16). | **PASS** |
| 5 | `ProcessPayment_CrossBranchAccess_ThrowsForbiddenException` | Nhân viên cơ sở này cố tình thu tiền cho đơn cơ sở khác bị từ chối với `ForbiddenException`. | **PASS** |
| 6 | `ProcessPayment_Overpayment_ThrowsBusinessException` | Thu tiền vượt quá số tiền còn nợ bị hệ thống chặn với thông báo tiếng Việt. | **PASS** |
| 7 | `InvoiceService_GetInvoiceByBookingId_ReturnsCorrectInvoiceDto` | Trích xuất dữ liệu hóa đơn đúng số tiền, chiết khấu, cọc và dư nợ. | **PASS** |
| 8 | `InvoicePrinter_BuildDocument_GeneratesValidFlowDocumentWithCorrectTotals` | Tạo `FlowDocument` in ấn chứa đầy đủ thông tin chi nhánh, mã hóa đơn, số tiền format chuẩn xác. | **PASS** |
| 9 | `InvoiceService_CrossBranchAccess_ThrowsForbiddenException` | Bảo mật: Không cho phép nhân viên chi nhánh khác in hóa đơn của cơ sở khác. | **PASS** |
| 10 | `InvoiceService_BookingNotFound_ThrowsNotFoundException` | Truy vấn hóa đơn với ID không tồn tại xử lý lỗi chuẩn xác. | **PASS** |

### 2.2. Kết quả thực thi `dotnet test`:
```text
Passed!  - Failed:     0, Passed:   136, Skipped:     0, Total:   136, Duration: 31 s - BadmintonManager.Tests.dll (net8.0)
```

---

## 3. KẾT LUẬN

Hệ thống đã đạt đầy đủ và vượt trội tiêu chí **Definition of Done (DoD) của Ngày 6**:
- Vòng đời booking chạy mượt mà từ khi khởi tạo `Pending`, qua đặt cọc `Confirmed`, sửa đổi giờ, nhận sân `CheckedIn`, đến khi thanh toán đủ và chuyển sang `Completed`.
- Cơ chế tích điểm và nâng hạng thẻ hội viên tự động vận hành an toàn trong transaction.
- Hóa đơn in ấn và bản xem trước `FlowDocument` hiển thị sắc nét, tính toán chuẩn từng đồng tiền.
- Mã nguồn tuân thủ nghiêm ngặt mô hình MVVM, Dependency Injection, Clean Architecture và nguyên tắc bảo mật đa chi nhánh.
