using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LEDControl.Database;
using LEDControl.Database.Models;
using LEDControl.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VideoLibrary;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace LEDControl.Services;

public class ConvertService : BackgroundService
{
    private readonly ILogger<ConvertService> _logger;
    private readonly IHubContext<ConvertHub> _hubContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly HttpClient _client;
    private readonly YouTube _youtube;
    private DirectoryInfo _tempPath;
    private DirectoryInfo _finalPath;

    public ConvertService(ILogger<ConvertService> logger, IHubContext<ConvertHub> hubContext, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _hubContext = hubContext;
        _serviceScopeFactory = serviceScopeFactory;
        _youtube = YouTube.Default;
        _client = new HttpClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _tempPath = Directory.CreateDirectory("./temp");
        _finalPath = Directory.CreateDirectory("./convertfiles");
        
        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
        FFmpeg.SetExecutablesPath(Directory.GetCurrentDirectory());

        while (!stoppingToken.IsCancellationRequested)
        {
            var continueProc = false;
            try
            {
                continueProc = await DoWork(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled Exception: {Message}", ex.Message);
            }
            if (!continueProc)
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task<bool> DoWork(CancellationToken token)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        var videos = await dataContext.Videos.Where(p =>
            p.ConvertStatus != ConvertStatus.Done && p.ConvertStatus != ConvertStatus.Failed).ToListAsync();

        if (!videos.Any())
            return false;

        foreach (var video in videos)
        {
            if (token.IsCancellationRequested)
                return false;

            try
            {
                video.ConvertStatus = ConvertStatus.Processing;
                await dataContext.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("Update", video);

                var ytVideo = (await _youtube.GetAllVideosAsync(video.Link)).MaxBy(o => o.AudioBitrate);

                if (ytVideo is null)
                {
                    video.Hint = "Error while loading video data";
                    video.ConvertStatus = ConvertStatus.Failed;
                    await dataContext.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("Update", video);
                    continue;
                }

                var workFile = await DownloadVideo(ytVideo, video, dataContext);
                var finalFile = new FileInfo(Path.Combine(_finalPath.FullName,
                    Path.GetFileNameWithoutExtension(workFile.Name) + ".mp3"));

                var mediaInfo = await FFmpeg.GetMediaInfo(workFile.FullName);
                var audioStream = mediaInfo.AudioStreams.OrderByDescending(p => p.Bitrate).First();
                var conversion = FFmpeg.Conversions.New();
                conversion.AddStream(audioStream);
                conversion.SetAudioBitrate(video.Bitrate);
                conversion.SetOutput(finalFile.FullName);
                conversion.SetPreset(video.ConversionPreset);

                conversion.OnProgress += (_, args) =>
                {
                    video.ConvertProgress =
                        (int)(Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds, 2) * 100);
                    dataContext.SaveChanges();
                    _hubContext.Clients.All.SendAsync("UpdateConvert", video.Id, video.ConvertProgress).Wait();
                };

                await conversion.Start(token);
                await Task.Delay(500, token);

                video.ConvertStatus = ConvertStatus.Done;
                video.ConvertProgress = 100;

                await dataContext.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("Update", video);

                workFile.Refresh();
                if (workFile.Exists)
                    workFile.Delete();
            }
            catch (Exception ex)
            {
                _logger.LogError("Processing Video {Id}: {Message}", video.Id, ex.Message);
                video.ConvertStatus = ConvertStatus.Failed;
                video.Hint = ex.Message;
                await dataContext.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("Update", video);
            }
        }

        return true;
    }

    private async Task<FileInfo> DownloadVideo(YouTubeVideo ytVideo, ConvertVideo video, DataContext dataContext)
    {
        var lastPercentageSend = 0;
        var workFile = new FileInfo(Path.Combine(_tempPath.FullName, video.Id + ytVideo.FileExtension));
        await using (var output = File.Open(workFile.FullName, FileMode.Create))
        {
            long? totalByte;
            using (var request = new HttpRequestMessage(HttpMethod.Head, ytVideo.Uri))
                totalByte = (await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)).Content.Headers.ContentLength;
            if (!totalByte.HasValue)
                throw new InvalidOperationException();

            await using (var input = await _client.GetStreamAsync(ytVideo.Uri))
            {
                var buffer = new byte[16 * 1024];
                int read, totalRead = 0;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                    totalRead += read;
                    var percentage = (totalRead / (double)totalByte) * 100;

                    if ((int)percentage % 10 == 0 && (int)percentage != lastPercentageSend)
                    {
                        lastPercentageSend = (int)percentage;

                        video.DownloadProgress = (int)percentage;
                        await dataContext.SaveChangesAsync();
                        await _hubContext.Clients.All.SendAsync("UpdateDownload", video.Id, (int)percentage);
                    }
                            
                }
            }
        }

        video.DownloadProgress = 100;
        await dataContext.SaveChangesAsync();
        await _hubContext.Clients.All.SendAsync("UpdateDownload", video.Id, 100);
        return workFile;
    }
}