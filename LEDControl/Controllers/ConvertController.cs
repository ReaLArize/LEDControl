using System;
using System.Threading.Tasks;
using LEDControl.Database;
using LEDControl.Database.Models;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> AddVideo(ConvertVideo video)
    {
        if (video is null || string.IsNullOrEmpty(video.Link))
            return BadRequest();

        var ytVideo = YouTube.Default.GetVideo(video.Link);
        video.Id = Guid.NewGuid();
        video.Title = ytVideo.Title;
        video.Created = DateTime.Now;
        video.ConvertStatus = ConvertStatus.Waiting;

        await _dataContext.Videos.AddAsync(video);
        await _dataContext.SaveChangesAsync();
        return Ok(video);
    }
}