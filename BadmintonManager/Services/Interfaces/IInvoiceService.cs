using System.Threading.Tasks;
using BadmintonManager.DTOs;

namespace BadmintonManager.Services.Interfaces;

public interface IInvoiceService
{
    Task<InvoiceDto> GetInvoiceDataAsync(int bookingId, string? cashierName = null);
}
