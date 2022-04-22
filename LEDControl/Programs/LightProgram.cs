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
    private LightProgramSettings _settings;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly int _ledCount = 300;
    private Task _runningTask;
    public LightProgram(LightProgramSettings settings)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _settings = settings;
    }
    
    public void Init(IServiceProvider serviceProvider)
    {
        _settingsService = serviceProvider.GetService<SettingsService>();
        if(_settingsService != null)
            _settingsService.LightSettingsEvent += (_, settings) =>
            {
                _settings = settings;
                SendColor(_settings.Color);
            };
    }

    private void SendColor(Color color)
    {
        Console.WriteLine(color);
    }

    private async Task Update(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            SendColor(_settings.Color);
            await Task.Delay(_settings.UpdateInterval, token);
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