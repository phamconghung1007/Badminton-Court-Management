using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.DTOs;

namespace BadmintonManager.Services.Interfaces;

public interface IFixedScheduleService
{
    Task<FixedScheduleResultDto> CreateFixedScheduleAsync(CreateFixedScheduleRequest request);
    Task CancelFixedScheduleAsync(int scheduleId);
    Task<List<FixedScheduleDto>> GetFixedSchedulesAsync(int? branchId = null, int? customerId = null, bool? isActive = null);
    Task<FixedScheduleDto?> GetFixedScheduleByIdAsync(int id);
}
