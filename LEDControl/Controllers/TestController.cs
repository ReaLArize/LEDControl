using System.Drawing;
using LEDControl.Programs;
using LEDControl.Programs.Settings;
using LEDControl.Services;
using Microsoft.AspNetCore.Mvc;

namespace LEDControl.Controllers;

[ApiController]
public class TestController : ControllerBase
{
    private readonly ProgramService _programService;
    private readonly SettingsService _settingsService;
    public TestController(ProgramService programService, SettingsService settingsService)
    {
        _programService = programService;
        _settingsService = settingsService;
    }
    
    [HttpGet("test")]
    public IActionResult Test()
    {
        _programService.Start(new LightProgram());
        return Ok("OK");
    }
    
    [HttpGet("test2")]
    public IActionResult Test2()
    {
        _programService.Stop();
        return Ok("OK");
    }
    
    [HttpGet("test3")]
    public IActionResult Test3()
    {
        _settingsService.LightProgramSettings.Color = Color.Aqua;
        _settingsService.RaiseSettingsChangedEvent();
        return Ok("OK");
    }
}