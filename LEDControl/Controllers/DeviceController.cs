using System;
using System.Net;
using System.Threading.Tasks;
using LEDControl.Database;
using LEDControl.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LEDControl.Controllers;

[Route("device")]
[ApiController]
public class DeviceController : ControllerBase
{
    private readonly DataContext _dataContext;

    public DeviceController(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetDevices()
    {
        return Ok(await _dataContext.Devices.ToListAsync());
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
        
        device.Id = Guid.NewGuid();
        await _dataContext.Devices.AddAsync(device);
        await _dataContext.SaveChangesAsync();
        return Ok(device);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDevice(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest();
        
        var device = await _dataContext.Devices.FirstOrDefaultAsync(p => p.Id == id);
        if (device is null)
            return NotFound();
        _dataContext.Devices.Remove(device);
        await _dataContext.SaveChangesAsync();
        return NoContent();
    }
}