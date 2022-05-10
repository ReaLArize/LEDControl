using System.Drawing;

namespace LEDControl.Dtos;

public class LightRequest
{
    public Color[] Colors { get; set; }

    public LightRequestMode Mode { get; set; }

    public LightRequest(int numLeds)
    {
        Colors = new Color[numLeds];
        Mode = LightRequestMode.Off;
    }

    public void Off()
    {
        Mode = LightRequestMode.Off;
        Colors = new Color[Colors.Length];
    }

    public void FullColor(Color color)
    {
        Mode = LightRequestMode.Color;
        
        for (var i = 0; i < Colors.Length; i++)
            Colors[i] = color;
    }

    public byte[] ToByteArray()
    {
        if (Mode == LightRequestMode.Off)
            return new byte[] { 0 };
        
        var res = new byte[Colors.Length * 3 + 1];
        res[0] = 1;
        var colorCount = 0;
        for (var i = 1; i < res.Length; i++)
        {
            res[i] = Colors[colorCount].R;
            i++;
            res[i] = Colors[colorCount].G;
            i++;
            res[i] = Colors[colorCount].B;
            colorCount++;
        }
        return res;
    }
}

public enum LightRequestMode
{
    Off,
    Color
}