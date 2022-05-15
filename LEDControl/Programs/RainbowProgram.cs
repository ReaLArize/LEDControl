using System;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LEDControl.Database.Models;
using LEDControl.Dtos;
using LEDControl.Programs.Settings;
using LEDControl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LEDControl.Programs;

public class RainbowProgram : IProgram
{
    private SettingsService _settingsService;
    private DeviceService _deviceService;
    private readonly UdpClient _udpClient;
    private RainbowProgramSettings Settings => _settingsService.RainbowProgramSettings;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task _runningTask;

    public RainbowProgram()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _udpClient = new UdpClient();
    }

    public void Init(IServiceProvider serviceProvider)
    {
        _settingsService = serviceProvider.GetRequiredService<SettingsService>();
        _deviceService = serviceProvider.GetRequiredService<DeviceService>();
    }

    private async Task RunCycle(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (token.IsCancellationRequested)
                return;

            for (var i = 0; i < 256; i++)
            {
                foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Light))
                {
                    device.LightRequest.Mode = LightRequestMode.Color;
                    if (token.IsCancellationRequested)
                        return;
                    for (var ii = 0; ii < device.NumLeds; ii++)
                    {
                        if (token.IsCancellationRequested)
                            return;
                        device.LightRequest.Colors[ii] = getWheelColor(((ii * 256 / device.NumLeds) + i) % 256);
                    }

                    var data = device.LightRequest.ToByteArray();
                    await _udpClient.SendAsync(data, data.Length, device.Hostname, device.Port);
                    await Task.Delay(Settings.Speed, token);
                }
            }
        }
    }

    private Color getWheelColor(int wheelpos)
    {
        if (wheelpos < 85)
        {
            return Color.FromArgb(0, wheelpos * 3, 255 - wheelpos * 3);
        }

        if (wheelpos < 170)
        {
            wheelpos -= 85;
            return Color.FromArgb(wheelpos * 3, 255 - wheelpos * 3, 0);
        }

        wheelpos -= 170;
        return Color.FromArgb(255 - wheelpos * 3, 0, wheelpos * 3);
    }

    public void Run()
    {
        _runningTask = RunCycle(_cancellationTokenSource.Token);
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        if (!_runningTask.IsCompleted)
            _runningTask.Wait();
        Task.Delay(50).Wait();
    }
}