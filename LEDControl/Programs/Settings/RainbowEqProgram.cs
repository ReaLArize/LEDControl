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
using LEDControl.Services;
using MathNet.Numerics.IntegralTransforms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PulseAudioWrapper;

namespace LEDControl.Programs.Settings;

public unsafe class RainbowEqProgram : IProgram
{
    private DeviceService _deviceService;
    private Task _workTask;
    private readonly CancellationTokenSource _tokenSource = new();
    private pa_simple* _apiRead;
    private UdpClient _udpClient;
    private ILogger<RainbowEqProgram> _logger;

    private const nuint ChunkSize = 2048;
    private const int EqSize = 16;
    private const int LedCount = 295;
    private readonly byte[] _buffer = new byte[ChunkSize];

    private int _oldEqCount;
    private readonly double[][] _oldEq = new double[75][];
    
    private int _rainbowCount;


    public void Init(IServiceProvider serviceProvider)
    {
        for (var i = 0; i < _oldEq.Length; i++)
            _oldEq[i] = new double[EqSize];
        
        _udpClient = new UdpClient();
        _deviceService = serviceProvider.GetRequiredService<DeviceService>();
        _logger = serviceProvider.GetRequiredService<ILogger<RainbowEqProgram>>();
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
                var eqTask = Task.Run(() => ProcessEq(data));
                var rainbowTask = Task.Run(() => ProcessRainbow());

                Task.WaitAll(eqTask, rainbowTask);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error Music Processing");   
            }

            Thread.Sleep(10);
            
            _oldEqCount++;
            _rainbowCount++;
            if (_oldEqCount >= _oldEq.Length)
                _oldEqCount = 0;
            if (_rainbowCount >= 256)
                _rainbowCount = 0;
        }
    }

    private void ProcessRainbow()
    {
        var colors = new Color[LedCount];
        for (var i = 0; i < colors.Length; i++)
        {
            var tempColor = GetWheelColor(((i * 256 / colors.Length) + _rainbowCount) % 256);
            colors[i] = tempColor;
        }
        
        foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Light))
        {
            device.LightRequest.Mode = LightRequestMode.Color;
            device.LightRequest.Colors = colors;
            var data = device.LightRequest.ToByteArray();
            _udpClient.Send(data, data.Length, device.Hostname, device.Port);
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

    public void Run()
    {
        _workTask = Task.Run(() => ReadMusic(_tokenSource.Token));
    }

    public void Stop()
    {
        _tokenSource.Cancel();
        if(!_workTask.IsCompleted)
            _workTask.Wait();

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