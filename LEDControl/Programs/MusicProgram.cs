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
using LEDControl.Services;
using MathNet.Numerics.IntegralTransforms;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using PulseAudioWrapper;

namespace LEDControl.Programs;

public unsafe class MusicProgram : IProgram
{
    private const nuint ChunkSize = 2048;
    private const int OldCount = 100;
    
    private DeviceService _deviceService;
    private Task _workTask;
    private readonly CancellationTokenSource _tokenSource = new();
    private pa_simple* _apiRead;
    private IHubContext<MusicHub> _hubContext;
    private UdpClient _udpClient;
    
    private readonly byte[] _buffer = new byte[ChunkSize];
    private int _processCount;
    private readonly double[][] _oldFfft = new double[OldCount][];

    public void Init(IServiceProvider serviceProvider)
    {
        for (var i = 0; i < _oldFfft.Length; i++)
            _oldFfft[i] = new double[ChunkSize / 4];
        _udpClient = new UdpClient();
        _deviceService = serviceProvider.GetRequiredService<DeviceService>();
        _hubContext = serviceProvider.GetRequiredService<IHubContext<MusicHub>>();
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
            Process(fftData.Take(fftData.Length / 2).Select( x => x.Magnitude).ToArray());
            Thread.Sleep(10);

            _processCount++;
            if (_processCount == OldCount)
                _processCount = 0;
        }
    }
    
    private void Process(double[] fftData)
    {
        if (fftData.Average() > 0.05)
        {
            var averages = new double[fftData.Length];
            var differences = new double[fftData.Length];
            for (var i = 0; i < averages.Length; i++)
            {
                double temp = 0;
                for (var j = 0; j < _oldFfft.Length; j++)
                    temp += _oldFfft[j][i];
                averages[i] = temp / _oldFfft.Length;
            }
            for (int i = 0; i < averages.Length; i++)
                differences[i] = fftData[i] / averages[i];

            var colors = new Color[295];
            var max = differences.Max();
            for (int i = 0; i < colors.Length; i++)
            {
                var proc = differences[i] / max;
                if (proc < 0)
                    proc = 0;
                else if (proc > byte.MaxValue)
                    proc = byte.MaxValue;
                colors[i] = Color.FromArgb(0, 0, (int)proc * byte.MaxValue);
            }

            foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Light))
            {
                device.LightRequest.Mode = LightRequestMode.Color;
                device.LightRequest.Colors = colors;
                var data = device.LightRequest.ToByteArray();
                _udpClient.Send(data, data.Length, device.Hostname, device.Port);
            }
            

            _hubContext.Clients.All.SendAsync("UpdateChart", colors.Select(p => (int)p.B).ToList()).Wait();
        }
        else
            _hubContext.Clients.All.SendAsync("UpdateChart", new double[fftData.Length]);
        _oldFfft[_processCount] = fftData;
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
}