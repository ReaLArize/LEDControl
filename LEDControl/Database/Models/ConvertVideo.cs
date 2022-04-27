using System;
using System.ComponentModel.DataAnnotations;
using Xabe.FFmpeg;

namespace LEDControl.Database.Models;

public class ConvertVideo
{
    [Key]
    public Guid Id { get; set; }
    
    [MaxLength(250)]
    public string Link { get; set; }

    [MaxLength(250)]
    public string Title { get; set; }
    public int DownloadProgress { get; set; }
    public int ConvertProgress { get; set; }
    public ConvertStatus ConvertStatus { get; set; }
    public DateTime Created { get; set; }

    public ConversionPreset ConversionPreset { get; set; }
    public int Bitrate { get; set; } = 320_000;

    [MaxLength(250)]
    public string Hint { get; set; }
    
}