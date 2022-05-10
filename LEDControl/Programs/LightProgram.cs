using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using LEDControl.Database.Models;
using LEDControl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LEDControl.Programs;

public class LightProgram : IProgram
{
    private SettingsService _settingsService;
    private DeviceService _deviceService;
    private UdpClient _udpClient;
    private List<Device> _devices;

    public void Init(IServiceProvider serviceProvider)
    {
        _udpClient = new UdpClient();
        _settingsService = serviceProvider.GetRequiredService<SettingsService>();
        _deviceService = serviceProvider.GetRequiredService<DeviceService>();
        _settingsService.SettingsChangedEvent += SendColor;
        _devices = _deviceService.Devices.Where(p => p.Mode == DeviceMode.Light).ToList();
    }

    private void SendColor(object sender)
    {
        foreach (var device in _devices)
        {
            device.LightRequest.FullColor(_settingsService.LightProgramSettings.Color);
            var data = device.LightRequest.ToByteArray();
            _udpClient.Send(data, data.Length, device.Hostname, device.Port);
        }
    }

    public void Run()
    {
        SendColor(null);
    }

    public void Stop()
    {
        _settingsService.SettingsChangedEvent -= SendColor;
    }
}