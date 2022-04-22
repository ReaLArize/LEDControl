namespace LEDControl.Database.Models;

public class Video
{
    public string Id { get; set; }
    public string Title { get; set; }
    public int DownloadProgress { get; set; }
    public int ConvertProgress { get; set; }
}