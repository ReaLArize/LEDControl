using System.Drawing;

namespace LEDControl.Dtos;

public class LightRequest
{
    public Color[] Colors { get; set; }
    public LightRequestMode Mode { get; set; }

    private int[] _lastEq;

    public LightRequest(int numLeds)
    {
        Colors = new Color[numLeds];
        Mode = LightRequestMode.Off;
        _lastEq = new int[16];
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

    public void SetEq(int i, int value)
    {
        var diff = _lastEq[i] - value;
        if (diff >= 1)
        {
            if (diff > 5)
                diff = 2;
            else
                diff = 1;
            value = _lastEq[i] - diff;
            if (value < 0)
                value = 0;
        }

        if (i % 2 == 0)
        {
            var matrixStart = i * 16;
            for (var j = 0; j < value; j++)
                Colors[matrixStart + j] = GetEqColor(j);
        }
        else
        {
            var matrixStart = i * 16 + 15;
            for (var j = 0; j < value; j++)
                Colors[matrixStart - j] = GetEqColor(j);
        }

        _lastEq[i] = value;
    }

    private Color GetEqColor(int value)
    {
        if (value < 3)
            return Color.FromArgb(0, 0, 2);
        if (value < 6)
            return Color.FromArgb(0, 1, 1);
        if(value < 10)
            return Color.FromArgb(0, 2, 0);
        if (value < 13)
            return Color.FromArgb(1, 1, 0);
        return Color.FromArgb(2, 0, 0);
    }
}

public enum LightRequestMode
{
    Off,
    Color
}