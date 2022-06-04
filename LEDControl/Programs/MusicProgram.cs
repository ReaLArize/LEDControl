using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using LEDControl.Database.Models;
using LEDControl.Dtos;
using LEDControl.Hubs;
using LEDControl.Programs.Settings;
using LEDControl.Services;
using MathNet.Numerics.IntegralTransforms;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PulseAudioWrapper;

namespace LEDControl.Programs;

public unsafe class MusicProgram : IProgram
{
    private SettingsService _settingsService;
    private DeviceService _deviceService;
    private Task _workTask;
    private readonly CancellationTokenSource _tokenSource = new();
    private pa_simple* _apiRead;
    private IHubContext<MusicHub> _hubContext;
    private UdpClient _udpClient;
    private ILogger<MusicProgram> _logger;
    private MusicProgramSettings _musicProgramSettings;
    private Random _random;

    private const nuint ChunkSize = 2048;
    private const int EqSize = 16;
    private const int LedCount = 295;
    private readonly byte[] _buffer = new byte[ChunkSize];
    
    private int _oldFftCount;
    private readonly double[][] _oldFft = new double[100][];
    
    private int _oldEqCount;
    private readonly double[][] _oldEq = new double[75][];
    
    private int _rainbowCount;
    private Color _currentColor = Color.Blue;


    public void Init(IServiceProvider serviceProvider)
    {
        for (var i = 0; i < _oldFft.Length; i++)
            _oldFft[i] = new double[ChunkSize / 4];
        for (var i = 0; i < _oldEq.Length; i++)
            _oldEq[i] = new double[EqSize];
        
        _udpClient = new UdpClient();
        _deviceService = serviceProvider.GetRequiredService<DeviceService>();
        _hubContext = serviceProvider.GetRequiredService<IHubContext<MusicHub>>();
        _logger = serviceProvider.GetRequiredService<ILogger<MusicProgram>>();
        _settingsService = serviceProvider.GetRequiredService<SettingsService>();
        _musicProgramSettings = _settingsService.MusicProgramSettings;
        _settingsService.SettingsChangedEvent += OnSettingsChanged;
        _random = new Random(DateTime.Now.Millisecond);
    }

    private void ReadMusic(CancellationToken token)
    {
        pa_sample_spec sampleSpec = new()
        {
            channels = 1,
            format = pa_sample_format.PA_SAMPLE_S16LE,
            rate = 44100
        };
        pa_buffer_attr attr = new ()
        {
            fragsize = 1600
        };
        int error;
        _apiRead = PulseSimpleApi.pa_simple_new(
            null,
            "Test",
            pa_stream_direction.PA_STREAM_RECORD,
            "alsa_output.usb-C-Media_Electronics_Inc._USB_Audio_Device-00.analog-stereo.monitor",
            "Test",
            &sampleSpec,
            null,
            &attr,
            &error
        );
        while (!token.IsCancellationRequested)
        {
            PulseSimpleApi.pa_simple_flush(_apiRead, &error);
            if (PulseSimpleApi.pa_simple_read(_apiRead, _buffer, ChunkSize, &error) < 0)
                Console.WriteLine("error");

            var rawData = Convert16BitToFloat(_buffer);
            var fftData = rawData.Select(p => new Complex(p, 0)).ToArray();
            
            Fourier.Forward(fftData, FourierOptions.Matlab);
            var data = fftData.Take(fftData.Length / 2).Select(x => x.Magnitude).ToArray();
            try
            {
                var lightTask = _musicProgramSettings.MusicMode switch
                {
                    MusicMode.Blue => Task.Run(() => ProcessStatic(data, Color.Blue)),
                    MusicMode.Green => Task.Run(() => ProcessStatic(data, Color.Green)),
                    MusicMode.Red => Task.Run(() => ProcessStatic(data, Color.Red)),
                    MusicMode.Rainbow => Task.Run(() => ProcessRainbow(data)),
                    MusicMode.Changing => Task.Run(() => ProcessChanging(data)),
                    _ => Task.Run(() => ProcessRainbow(data))
                };

                var eqTask = Task.Run(() => ProcessEq(data));
                Task.WaitAll(lightTask, eqTask);
                _oldFft[_oldFftCount] = data;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error Music Processing");   
            }

            Thread.Sleep(10);

            _oldFftCount++;
            _oldEqCount++;
            _rainbowCount++;
            if (_oldFftCount >= _oldFft.Length)
                _oldFftCount = 0;
            if (_oldEqCount >= _oldEq.Length)
                _oldEqCount = 0;
            if (_rainbowCount >= 256)
                _rainbowCount = 0;
        }
    }
    private void ProcessEq(double[] fftData)
    {
        if (fftData.Average() > 0.005)
        {
            foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Pictures))
                device.LightRequest.FullColor(Color.Black);
            
            var chunks = new double[16][];
            chunks[0] = fftData[0..3];
            chunks[1] = fftData[4..7];
            chunks[2] = fftData[8..12];
            chunks[3] = fftData[13..28];
            chunks[4] = fftData[29..44];
            chunks[5] = fftData[45..62];
            chunks[6] = fftData[63..78];
            chunks[7] = fftData[79..94];
            chunks[8] = fftData[95..110];
            chunks[9] = fftData[111..126];
            chunks[10] = fftData[127..142];
            chunks[11] = fftData[143..158];
            chunks[12] = fftData[159..174];
            chunks[13] = fftData[175..190];
            chunks[14] = fftData[191..206];
            chunks[15] = fftData[207..255];
            
            for (var i = 0; i < chunks.Length; i++)
            {
                double maxOld = 0;
                for (var j = 0; j < _oldEq.Length; j++)
                {
                    if (_oldEq[j][i] > maxOld)
                        maxOld = _oldEq[j][i];
                }

                var avg = chunks[i].Average();
                var perc = avg / maxOld;
                if (perc > 1)
                    perc = 1;
                else if (perc < 0 || avg < 0.05)
                    perc = 0;
                

                foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Pictures))
                    device.LightRequest.SetEq(i, Convert.ToInt32(perc * 16));
                _oldEq[_oldEqCount][i] = chunks[i].Average() * 1.5;
            }
            
            foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Pictures))
            {
                var data = device.LightRequest.ToByteArray();
                _udpClient.Send(data, data.Length, device.Hostname, device.Port);
            }
        }
        else
        {
            foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Pictures))
            {
                device.LightRequest.Off();
                var data = device.LightRequest.ToByteArray();
                _udpClient.Send(data, data.Length, device.Hostname, device.Port);
            }
        }
    }
    private void ProcessStatic(double[] fftData, Color color)
    {
        if (fftData.Average() > 0.05)
        {
            var differences = CalculatePercentages(fftData);
            var colors = new Color[LedCount];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.FromArgb(Convert.ToInt32(color.R * differences[i]),
                    Convert.ToInt32(color.G * differences[i]), 
                    Convert.ToInt32(color.B * differences[i]));
            }

            foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Light))
            {
                device.LightRequest.Mode = LightRequestMode.Color;
                device.LightRequest.Colors = colors;
                var data = device.LightRequest.ToByteArray();
                _udpClient.Send(data, data.Length, device.Hostname, device.Port);
            }
            

            _hubContext.Clients.All.SendAsync("UpdateChart", differences).Wait();
        }
        else
        {
            foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Light))
            {
                device.LightRequest.Off();
                var data = device.LightRequest.ToByteArray();
                _udpClient.Send(data, data.Length, device.Hostname, device.Port);
            }
            _hubContext.Clients.All.SendAsync("UpdateChart", new double[fftData.Length]);
        }
    }
    private void ProcessRainbow(double[] fftData)
    {
        if (fftData.Average() > 0.05)
        {
            var differences = CalculatePercentages(fftData);
            var colors = new Color[LedCount];
            for (var i = 0; i < colors.Length; i++)
            {
                var tempColor = GetWheelColor(((i * 256 / colors.Length) + _rainbowCount) % 256);
                tempColor = Color.FromArgb(Convert.ToInt32(tempColor.R * differences[i]),
                    Convert.ToInt32(tempColor.G * differences[i]), 
                    Convert.ToInt32(tempColor.B * differences[i]));
                
                colors[i] = tempColor;
            }

            foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Light))
            {
                device.LightRequest.Mode = LightRequestMode.Color;
                device.LightRequest.Colors = colors;
                var data = device.LightRequest.ToByteArray();
                _udpClient.Send(data, data.Length, device.Hostname, device.Port);
            }
            
            
            _hubContext.Clients.All.SendAsync("UpdateChart", differences).Wait();
        }        
        else
        {
            foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Light))
            {
                device.LightRequest.Off();
                var data = device.LightRequest.ToByteArray();
                _udpClient.Send(data, data.Length, device.Hostname, device.Port);
            }
            _hubContext.Clients.All.SendAsync("UpdateChart", new double[fftData.Length]);
        }
        
        
        
        foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Light))
        {
            device.LightRequest.Mode = LightRequestMode.Color;
            for (var ii = 0; ii < device.NumLeds; ii++)
            {
                device.LightRequest.Colors[ii] = GetWheelColor(((ii * 256 / device.NumLeds) + _rainbowCount) % 256);
            }
        }
    }
    private void ProcessChanging(double[] fftData)
    {
        if (_oldEqCount == 0 && _random.Next(30) > 1)
        {
            _currentColor = _random.Next(10) switch
            {
                0 => Color.Blue,
                1 => Color.Red,
                2 => Color.Green,
                3 => Color.Yellow,
                4 => Color.Orange,
                5 => Color.Purple,
                6 => Color.DeepPink,
                7 => Color.Aqua,
                8 => Color.Navy,
                _ => Color.SpringGreen
            };
        }
        
        ProcessStatic(fftData, _currentColor);
    }
    private double[] CalculatePercentages(double[] fftData)
    {
        switch (_musicProgramSettings.CalculateMode)
        {
            case CalculateMode.Average:
            {
                var averages = new double[fftData.Length];
                var differences = new double[fftData.Length];
                for (var i = 0; i < averages.Length; i++)
                {
                    double temp = 0;
                    for (var j = 0; j < _oldFft.Length; j++)
                        temp += _oldFft[j][i];
                    averages[i] = temp / _oldFft.Length;
                }
            
                for (int i = 0; i < averages.Length; i++)
                {
                    differences[i] = fftData[i] / (averages[i] * 2);
                    if (differences[i] < 0)
                        differences[i] = 0;
                    else if (differences[i] > 1 || differences[i] < 0.05)
                        differences[i] = 1;
                }

                return differences;
            }
            case CalculateMode.Max:
            {
                var maxValues = new double[fftData.Length];
                var percentages = new double[fftData.Length];
                for (var i = 0; i < maxValues.Length; i++)
                {
                    double max = 0;
                    for (var j = 0; j < _oldFft.Length; j++)
                    {
                        if (_oldFft[j][i] > max)
                            max = _oldFft[j][i];
                    }
                    maxValues[i] = max;
                }
            
                for (var i = 0; i < maxValues.Length; i++)
                {
                    percentages[i] = fftData[i] / (maxValues[i] * 1.2);
                    if (percentages[i] < 0)
                        percentages[i] = 0;
                    else if (percentages[i] > 1 || percentages[i] < 0.05)
                        percentages[i] = 1;
                }

                return percentages;
            }
            default:
                return new double[fftData.Length];
        }
    }

    public void Run()
    {
        _workTask = Task.Run(() => ReadMusic(_tokenSource.Token));
    }

    public void Stop()
    {
        _tokenSource.Cancel();
        if(!_workTask.IsCompleted)
            _workTask.Wait();
        
        _settingsService.SettingsChangedEvent -= OnSettingsChanged;

        foreach (var device in _deviceService.Devices)
        {
            device.LightRequest.Off();
            var data = device.LightRequest.ToByteArray();
            _udpClient.Send(data, data.Length, device.Hostname, device.Port);
        }
        
        PulseSimpleApi.pa_simple_free(_apiRead);
        Task.Delay(50).Wait();
    }
    
    private IEnumerable<float> Convert16BitToFloat(byte[] input)
    {
        var inputSamples = input.Length / 2; // 16 bit input, so 2 bytes per sample
        var output = new float[inputSamples];
        var outputIndex = 0;
        for(var n = 0; n < inputSamples; n++)
        {
            var sample = BitConverter.ToInt16(input,n*2);
            output[outputIndex++] = sample / 32768f;
        }
        return output;
    }
    
    
    private void OnSettingsChanged(object sender)
    {
        _musicProgramSettings = _settingsService.MusicProgramSettings;
    }

    private Color GetWheelColor(int wheelpos)
    {
        if (wheelpos < 85)
        {
            return Color.FromArgb(0, wheelpos * 3, 255 - wheelpos * 3);
        }

        if (wheelpos < 170)
        {
            wheelpos -= 85;
            return Color.FromArgb(wheelpos * 3, 255 - wheelpos * 3, 0);
        }

        wheelpos -= 170;
        return Color.FromArgb(255 - wheelpos * 3, 0, wheelpos * 3);
    }
}