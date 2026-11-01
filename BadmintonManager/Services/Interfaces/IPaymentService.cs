using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentRequest request);
    Task<Payment> ProcessPaymentAsync(int bookingId, decimal amount, PaymentMethod method, PaymentType type, string? referenceNo, string? note);
    Task<PaymentSummaryDto> GetPaymentSummaryAsync(int bookingId);
    Task<List<PaymentItemDto>> GetPaymentItemDtosByBookingIdAsync(int bookingId);
    Task<List<Payment>> GetPaymentsByBookingIdAsync(int bookingId);
}
