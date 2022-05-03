using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using LEDControl.Programs.Settings;
using LEDControl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LEDControl.Programs;

public class LightProgram : IProgram
{
    private SettingsService _settingsService;
    private LightProgramSettings Settings => _settingsService.LightProgramSettings;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task _runningTask;
    public LightProgram()
    {
        _cancellationTokenSource = new CancellationTokenSource();
    }
    
    public void Init(IServiceProvider serviceProvider)
    {
        _settingsService = serviceProvider.GetRequiredService<SettingsService>();
        _settingsService.SettingsChangedEvent += sender =>
        {
            SendColor();
        };
    }

    private void SendColor()
    {
        Console.WriteLine(_settingsService.LightProgramSettings.Color);
    }

    private async Task Update(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            SendColor();
            await Task.Delay(Settings.UpdateInterval, token);
        }
    }

    public void Run()
    {
        _runningTask = Update(_cancellationTokenSource.Token);
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }
}