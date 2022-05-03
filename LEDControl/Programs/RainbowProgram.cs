using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using LEDControl.Programs.Settings;
using LEDControl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LEDControl.Programs;

public class RainbowProgram : IProgram
{
    private SettingsService _settingsService;
    private RainbowProgramSettings Settings => _settingsService.RainbowProgramSettings;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly int _ledCount = 300;
    private Task _runningTask;
    public RainbowProgram()
    {
        _cancellationTokenSource = new CancellationTokenSource();
    }
    
    public void Init(IServiceProvider serviceProvider)
    {
        _settingsService = serviceProvider.GetService<SettingsService>();
    }
    

    private async Task RunCycle(CancellationToken token)
    {
        var colorArray = new Color[_ledCount];
        while (!token.IsCancellationRequested)
        {
            for (var i = 0; i < 256; i++)
            {
                if (token.IsCancellationRequested)
                    return;
                for (int ii = 0; ii < _ledCount; ii++)
                {
                    if (token.IsCancellationRequested)
                        return;
                    colorArray[ii] = getWheelColor(((ii * 256 / _ledCount) + i) % 256);
                }
                Console.WriteLine(string.Join(",", colorArray));
                await Task.Delay(Settings.Speed, token);
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
    }
}