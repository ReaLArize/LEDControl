using System.Drawing;
using System.Threading.Tasks;
using LEDControl.Dtos;
using LEDControl.Programs;
using LEDControl.Services;
using Microsoft.AspNetCore.SignalR;

namespace LEDControl.Hubs;

public class LightHub : Hub
{
    private readonly ProgramService _programService;
    private readonly SettingsService _settingsService;
    private readonly ColorConverter _colorConverter;
    
    public LightHub(ProgramService programService, SettingsService settingsService)
    {
        _settingsService = settingsService;
        _programService = programService;
        _colorConverter = new ColorConverter();
    }

    public override async Task OnConnectedAsync()
    {
        var color = _settingsService.LightProgramSettings.Color;
        var light = new Light
        {
            HexString = $"#{color.R:X2}{color.G:X2}{color.B:X2}",
            RainbowOn = _programService.CurrentProgram is RainbowProgram
        };
        await Clients.Caller.SendAsync("UpdateLight", light);
        await base.OnConnectedAsync();
    }

    public async Task ChangeLight(string hexString)
    {
        _settingsService.LightProgramSettings.Color = (Color)(_colorConverter.ConvertFromString(hexString) ?? Color.Black);
        _settingsService.RaiseSettingsChangedEvent();
        if(_programService.CurrentProgram is not LightProgram)
            _programService.Start(new LightProgram());
        await UpdateLight();
    }

    public async Task Off()
    {
        _settingsService.LightProgramSettings.Color = Color.Black;
        _settingsService.RaiseSettingsChangedEvent();
        if(_programService.CurrentProgram is not LightProgram)
            _programService.Start(new LightProgram());
        await UpdateLight(true);
    }

    public async Task Rainbow()
    {
        _programService.Stop();
        _programService.Start(new RainbowProgram());
        await UpdateLight(true);
    }
    
    public async Task Music()
    {
        _programService.Stop();
        _programService.Start(new MusicProgram());
        await UpdateLight(true);
    }

    private async Task UpdateLight(bool updateAll = false)
    {
        var color = _settingsService.LightProgramSettings.Color;
        var light = new Light
        {
            HexString = $"#{color.R:X2}{color.G:X2}{color.B:X2}",
            RainbowOn = _programService.CurrentProgram is RainbowProgram,
            MusicOn = _programService.CurrentProgram is MusicProgram
        };
        if (updateAll)
            await Clients.All.SendAsync("UpdateLight", light);
        else
            await Clients.Others.SendAsync("UpdateLight", light);
    }
}