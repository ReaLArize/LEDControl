using System;
using System.Threading;
using System.Threading.Tasks;
using LEDControl.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace LEDControl.Services;

public class ConvertService : BackgroundService
{
    private readonly IHubContext<ConvertHub> _hubContext;
    
    public ConvertService(IHubContext<ConvertHub> hubContext)
    {
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DoWork();
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        };
    }

    private async Task DoWork()
    {
        return;
    }
}