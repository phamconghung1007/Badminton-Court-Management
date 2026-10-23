using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.Data;
using BadmintonManager.Models.Entities;
using BadmintonManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Services;

public class SettingService : ISettingService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public SettingService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<string> GetSettingAsync(string key, string defaultValue = "")
    {
        await using var db = await _factory.CreateDbContextAsync();
        var setting = await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value ?? defaultValue;
    }

    public async Task<int> GetIntSettingAsync(string key, int defaultValue = 0)
    {
        string val = await GetSettingAsync(key, defaultValue.ToString());
        return int.TryParse(val, out int result) ? result : defaultValue;
    }

    public async Task<decimal> GetDecimalSettingAsync(string key, decimal defaultValue = 0m)
    {
        string val = await GetSettingAsync(key, defaultValue.ToString());
        return decimal.TryParse(val, out decimal result) ? result : defaultValue;
    }

    public async Task SetSettingAsync(string key, string value)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new SystemSetting { Key = key, Value = value };
            db.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
        }

        await db.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.SystemSettings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value);
    }
}
