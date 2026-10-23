using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.Services.Interfaces;

public interface IPaymentService
{
    Task<Payment> ProcessPaymentAsync(int bookingId, decimal amount, PaymentMethod method, PaymentType type, string? referenceNo, string? note);
    Task<List<Payment>> GetPaymentsByBookingIdAsync(int bookingId);
}
