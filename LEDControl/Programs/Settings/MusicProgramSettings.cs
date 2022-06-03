namespace LEDControl.Programs.Settings;

public class MusicProgramSettings
{
    public MusicMode MusicMode { get; set; }
    public CalculateMode CalculateMode { get; set; }
}

public enum MusicMode
{
    Rainbow,
    Blue,
    Red,
    Green,
    Changing
}

public enum CalculateMode
{
    Average,
    Max
}