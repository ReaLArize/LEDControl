using System.Drawing;
using LEDControl.Programs.Settings;

namespace LEDControl.Services;

public class SettingsService
{
    private readonly object _lock;
    private RainbowProgramSettings _rainbowProgramSettings;
    private LightProgramSettings _lightProgramSettings;

    public delegate void SettingsChangedEventHandler(object sender);
    public event SettingsChangedEventHandler SettingsChangedEvent;
    
    public RainbowProgramSettings RainbowProgramSettings
    {
        get
        {
            lock (_lock)
                return _rainbowProgramSettings;
        }
        set
        {
            lock (_lock)
            {
                _rainbowProgramSettings = value;
                SettingsChangedEvent?.Invoke(this);
            }
        }
    }
    public LightProgramSettings LightProgramSettings
    {
        get
        {
            lock (_lock)
                return _lightProgramSettings;
        }
        set
        {
            lock (_lock)
            {
                _lightProgramSettings = value;
                SettingsChangedEvent?.Invoke(this);
            }
        }
    }
    
    public SettingsService()
    {
        _lock = new object();
        LightProgramSettings = new LightProgramSettings
        {
            Color = Color.Black
        };
        RainbowProgramSettings = new RainbowProgramSettings();
    }

    public void RaiseSettingsChangedEvent()
    {
        lock(_lock)
            SettingsChangedEvent?.Invoke(this);
    }
    
    
}