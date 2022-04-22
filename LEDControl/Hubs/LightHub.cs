using System.Drawing;
using System.Threading.Tasks;
using LEDControl.Programs;
using LEDControl.Programs.Settings;
using LEDControl.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LEDControl.Hubs;

public class LightHub : Hub
{
    private readonly ILogger<LightHub> _logger;
    private readonly ProgramService _programService;
    private readonly SettingsService _settingsService;
    public LightHub(ILogger<LightHub> logger, ProgramService programService, SettingsService settingsService)
    {
        _settingsService = settingsService;
        _programService = programService;
        _logger = logger;
    }

    public Task ChangeLight(int r, int g, int b)
    {
        var settings = new LightProgramSettings() { Color = Color.FromArgb(r, g, b) };
        if(_programService.CurrentProgram is LightProgram)
            _settingsService.RaiseLightSettingsEvent(settings);
        else
            _programService.Start(new LightProgram(settings));
        return Task.CompletedTask;
    }

    public Task Off()
    {
        _programService.Start(new LightProgram(new LightProgramSettings()));
        return Task.CompletedTask;
    }
}