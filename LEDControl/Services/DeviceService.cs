using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LEDControl.Database;
using LEDControl.Database.Models;
using LEDControl.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LEDControl.Services;

public class DeviceService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public List<Device> Devices { get; private set; }

    public DeviceService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        Init().Wait();
    }

    private async Task Init()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        Devices = await dataContext.Devices.AsNoTracking().ToListAsync();
        foreach (var device in Devices)
        {
            device.LightRequest = new LightRequest(device.NumLeds);
        }
    }

    public async Task<Device> AddDevice(Device device)
    {
        device.Id = Guid.NewGuid();
        
        using var scope = _serviceScopeFactory.CreateScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        await dataContext.Devices.AddAsync(device);
        await dataContext.SaveChangesAsync();
        
        device.LightRequest = new LightRequest(device.NumLeds);
        Devices.Add(device);
        return device;
    }

    public async Task<bool> DeleteDevice(Guid id)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        var device = await dataContext.Devices.FirstOrDefaultAsync(p => p.Id == id);
        if (device is null)
            return false;
        
        dataContext.Devices.Remove(device);
        await dataContext.SaveChangesAsync();
        Devices.RemoveAt(Devices.FindIndex(p => p.Id == id));
        return true;
    }
}