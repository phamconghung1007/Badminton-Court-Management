using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.DTOs;

namespace BadmintonManager.Services.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerDto>> SearchCustomersAsync(string? keyword);
    Task<CustomerDto?> GetCustomerByIdAsync(int id);
    Task<CustomerDetailDto?> GetCustomerDetailAsync(int id);
    Task<CustomerDto> CreateCustomerAsync(CustomerCreateDto request);
    Task<CustomerDto> UpdateCustomerAsync(CustomerUpdateDto request);
    Task<bool> ToggleCustomerStatusAsync(int id);
    Task DeleteCustomerAsync(int id);
}
