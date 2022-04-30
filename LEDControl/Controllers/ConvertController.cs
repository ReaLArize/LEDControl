using System;
using System.Threading.Tasks;
using LEDControl.Database;
using LEDControl.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoLibrary;

namespace LEDControl.Controllers;

[Route("/convert/")]
[ApiController]
public class ConvertController : ControllerBase
{
    private readonly DataContext _dataContext;

    public ConvertController(DataContext dataContext)
    {
        _dataContext = dataContext;
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
            ConversionPreset = newVideo.ConversionPreset
        };

        await _dataContext.Videos.AddAsync(video);
        await _dataContext.SaveChangesAsync();
        return Ok(video);
    }

    [HttpGet]
    public async Task<IActionResult> GetVideos()
    {
        return Ok(await _dataContext.Videos.ToListAsync());
    }
}