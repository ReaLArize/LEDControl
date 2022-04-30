using System;
using System.Linq;
using System.Threading.Tasks;
using LEDControl.Database;
using LEDControl.Database.Models;
using LEDControl.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VideoLibrary;

namespace LEDControl.Controllers;

[Route("/convert/")]
[ApiController]
public class ConvertController : ControllerBase
{
    private readonly DataContext _dataContext;
    private readonly IHubContext<ConvertHub> _hubContext;

    public ConvertController(DataContext dataContext, IHubContext<ConvertHub> hubContext)
    {
        _dataContext = dataContext;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> AddVideo(ConvertVideo newVideo)
    {
        if (newVideo is null || string.IsNullOrEmpty(newVideo.Link))
            return BadRequest();

        var ytVideo = YouTube.Default.GetVideo(newVideo.Link);
        if (ytVideo is null)
            return BadRequest();

        var video = new ConvertVideo
        {
            Id = Guid.NewGuid(),
            Title = ytVideo.Title,
            Created = DateTime.Now,
            ConvertStatus = ConvertStatus.Waiting,
            ConversionPreset = newVideo.ConversionPreset,
            Link = newVideo.Link
        };

        await _dataContext.Videos.AddAsync(video);
        await _dataContext.SaveChangesAsync();
        await _hubContext.Clients.All.SendAsync("NewVideo", video);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetVideos()
    {
        return Ok(await _dataContext.Videos.OrderByDescending(p => p.Created).ToListAsync());
    }
}