using System;
using System.Collections.Generic;

namespace BadmintonManager.DTOs;

public record InvoiceDto
{
    public int BookingId { get; init; }
    public string BookingCode { get; init; } = string.Empty;

    // Chi nhánh
    public string BranchName { get; init; } = string.Empty;
    public string BranchAddress { get; init; } = string.Empty;
    public string BranchPhone { get; init; } = string.Empty;

    // Sân & Giờ
    public string CourtName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public int DurationMinutes { get; init; }
    public string TimeRangeText => $"{StartTime:dd/MM/yyyy HH:mm} - {EndTime:HH:mm} ({DurationMinutes} phút)";

    // Khách hàng
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string? CustomerTierName { get; init; }

    // Tài chính
    public decimal SubTotal { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public decimal DepositAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount => Math.Max(0, FinalAmount - PaidAmount);

    // Thông tin thanh toán & in ấn
    public string PaymentMethod { get; init; } = "Tiền mặt";
    public string? ReferenceNo { get; init; }
    public string CashierName { get; init; } = "Nhân viên ca trực";
    public DateTime PrintedAt { get; init; } = DateTime.Now;
    public string StatusText { get; init; } = string.Empty;

    // Danh sách các lần thanh toán
    public List<PaymentItemDto> Payments { get; init; } = new();
}
