using System;
using System.Net;
using System.Threading.Tasks;
using LEDControl.Database.Models;
using LEDControl.Services;
using Microsoft.AspNetCore.Mvc;

namespace LEDControl.Controllers;

[Route("device")]
[ApiController]
public class DeviceController : ControllerBase
{
    private readonly DeviceService _deviceService;

    public DeviceController(DeviceService deviceService)
    {
        _deviceService = deviceService;
    }
    
    [HttpGet]
    public IActionResult GetDevices()
    {
        return Ok(_deviceService.Devices);
    }

    [HttpPost]
    public async Task<IActionResult> AddDevice(Device device)
    {
        if (device is null)
            return BadRequest();
        if (!IPAddress.TryParse(device.Hostname, out var _))
            return BadRequest();
        if (device.Port < IPEndPoint.MinPort && device.Port > IPEndPoint.MaxPort)
            return BadRequest();
        if (device.Name.Length < 3)
            return BadRequest();
        if (device.NumLeds < 3)
            return BadRequest();
        return Ok(await _deviceService.AddDevice(device));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDevice(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest();
        if (!await _deviceService.DeleteDevice(id))
            return NotFound();
        
        return NoContent();
    }
}