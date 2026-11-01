using System;
using System.Collections.Generic;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.DTOs;

public record ProcessPaymentRequest
{
    public int BookingId { get; init; }
    public decimal Amount { get; init; }
    public PaymentMethod Method { get; init; } = PaymentMethod.Cash;
    public PaymentType Type { get; init; } = PaymentType.Final;
    public string? ReferenceNo { get; init; }
    public string? Note { get; init; }
}

public record PaymentItemDto
{
    public int Id { get; init; }
    public int BookingId { get; init; }
    public decimal Amount { get; init; }
    public PaymentMethod Method { get; init; }
    public string MethodName => Method switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        _ => Method.ToString()
    };
    public PaymentType Type { get; init; }
    public string TypeName => Type switch
    {
        PaymentType.Deposit => "Đặt cọc",
        PaymentType.Final => "Thanh toán",
        PaymentType.Refund => "Hoàn tiền",
        _ => Type.ToString()
    };
    public string? ReferenceNo { get; init; }
    public DateTime PaidAt { get; init; }
    public string ReceivedByUserName { get; init; } = string.Empty;
    public string? Note { get; init; }
}

public record PaymentSummaryDto
{
    public int BookingId { get; init; }
    public string BookingCode { get; init; } = string.Empty;
    public string BranchName { get; init; } = string.Empty;
    public string CourtName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerTierName { get; init; }
    public BookingStatus Status { get; init; }
    public string StatusText => Status switch
    {
        BookingStatus.Pending => "Chờ duyệt",
        BookingStatus.Confirmed => "Đã duyệt",
        BookingStatus.CheckedIn => "Đang chơi",
        BookingStatus.Completed => "Hoàn thành",
        BookingStatus.Cancelled => "Đã hủy",
        BookingStatus.NoShow => "Vắng mặt",
        _ => Status.ToString()
    };

    public decimal SubTotal { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public decimal DepositAmount { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal TotalRefunded { get; init; }
    public decimal NetPaid => TotalPaid - TotalRefunded;
    public decimal RemainingAmount => Math.Max(0, FinalAmount - NetPaid);
    public bool IsPaidInFull => NetPaid >= FinalAmount;

    public List<PaymentItemDto> Payments { get; init; } = new();
}

public record PaymentResultDto
{
    public int PaymentId { get; init; }
    public int BookingId { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal NetPaid { get; init; }
    public decimal RemainingAmount { get; init; }
    public bool IsCompleted { get; init; }
    public BookingStatus NewStatus { get; init; }
    public int PointsEarned { get; init; }
    public bool IsTierUpgraded { get; init; }
    public string? NewTierName { get; init; }
    public string Message { get; init; } = string.Empty;
}
