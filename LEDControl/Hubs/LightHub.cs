using System.Drawing;
using System.Threading.Tasks;
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

    public async Task ChangeLight(string hexString)
    {
        _settingsService.LightProgramSettings.Color = (Color)(_colorConverter.ConvertFromString(hexString) ?? Color.Black);
        _settingsService.RaiseSettingsChangedEvent();
        if(_programService.CurrentProgram is not LightProgram)
            _programService.Start(new LightProgram());
    }

    public async Task Off()
    {
        _settingsService.LightProgramSettings.Color = Color.Black;
        _settingsService.RaiseSettingsChangedEvent();
        if(_programService.CurrentProgram is not LightProgram)
            _programService.Start(new LightProgram());
        
    }
}