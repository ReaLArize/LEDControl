using LEDControl.Programs.Settings;

namespace LEDControl.Services;

public class SettingsService
{
    public RainbowProgramSettings RainbowProgramSettings;
    public LightProgramSettings LightProgramSettings;
    private readonly object _lock;
    public SettingsService()
    {
        _lock = new object();
        LightProgramSettings = new LightProgramSettings();
        RainbowProgramSettings = new RainbowProgramSettings();
    }
    
    #region Rainbow

    
    public delegate void RainbowSettingsEventHandler(object sender, RainbowProgramSettings settings);
    public event RainbowSettingsEventHandler RainbowSettingsEvent;
    public void RaiseRainbowSettingsEvent(RainbowProgramSettings settings)
    {
        lock (_lock)
        {
            RainbowProgramSettings = settings;
            RainbowSettingsEvent?.Invoke(this, settings);
        }
            
    }
    
    #endregion
    
    #region Light

    
    public delegate void LightSettingsEventHandler(object sender, LightProgramSettings settings);
    public event LightSettingsEventHandler LightSettingsEvent;
    public void RaiseLightSettingsEvent(LightProgramSettings settings)
    {
        lock (_lock)
        {
            LightProgramSettings = settings;
            LightSettingsEvent?.Invoke(this, settings);
        }
    }
    
    #endregion
    
}