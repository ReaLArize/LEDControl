using System.Drawing;

namespace LEDControl.Programs.Settings;

public class LightProgramSettings
{
    public Color Color { get; set; }
    public int UpdateInterval { get; set; } = 5000;
}