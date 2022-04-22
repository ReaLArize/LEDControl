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
    private readonly ConvertService _convertService;
    public TestController(ProgramService programService, SettingsService settingsService, ConvertService convertService)
    {
        _convertService = convertService;
        _programService = programService;
        _settingsService = settingsService;
    }
    
    [HttpGet("test")]
    public IActionResult Test()
    {
        _programService.Start(new LightProgram(new LightProgramSettings()));
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
        _settingsService.RaiseLightSettingsEvent(new LightProgramSettings()
        {
            Color = Color.Aqua
        });
        return Ok("OK");
    }
}