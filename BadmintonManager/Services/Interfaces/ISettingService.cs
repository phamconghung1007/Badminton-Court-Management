using System.Collections.Generic;
using System.Threading.Tasks;

namespace BadmintonManager.Services.Interfaces;

public interface ISettingService
{
    Task<string> GetSettingAsync(string key, string defaultValue = "");
    Task<int> GetIntSettingAsync(string key, int defaultValue = 0);
    Task<decimal> GetDecimalSettingAsync(string key, decimal defaultValue = 0m);
    Task SetSettingAsync(string key, string value);
    Task<Dictionary<string, string>> GetAllSettingsAsync();
}
