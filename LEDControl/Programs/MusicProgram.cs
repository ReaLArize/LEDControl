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
using Microsoft.Extensions.Logging;
using PulseAudioWrapper;

namespace LEDControl.Programs;

public unsafe class MusicProgram : IProgram
{
    private DeviceService _deviceService;
    private Task _workTask;
    private readonly CancellationTokenSource _tokenSource = new();
    private pa_simple* _apiRead;
    private IHubContext<MusicHub> _hubContext;
    private UdpClient _udpClient;
    private ILogger<MusicProgram> _logger;

    private const nuint ChunkSize = 2048;
    private const int EqSize = 16;
    private readonly byte[] _buffer = new byte[ChunkSize];
    
    private int _oldFftCount;
    private readonly double[][] _oldFft = new double[100][];
    
    private int _oldEqCount;
    private readonly double[][] _oldEq = new double[75][];

    public void Init(IServiceProvider serviceProvider)
    {
        for (var i = 0; i < _oldFft.Length; i++)
            _oldFft[i] = new double[ChunkSize / 4];
        for (int i = 0; i < _oldEq.Length; i++)
            _oldEq[i] = new double[EqSize];
        
        _udpClient = new UdpClient();
        _deviceService = serviceProvider.GetRequiredService<DeviceService>();
        _hubContext = serviceProvider.GetRequiredService<IHubContext<MusicHub>>();
        _logger = serviceProvider.GetRequiredService<ILogger<MusicProgram>>();
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
                var lightTask = Task.Run(() => Process(data));
                var eqTask = Task.Run(() => ProcessEq(data));
                Task.WaitAll(lightTask, eqTask);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error Music Processing");   
            }

            Thread.Sleep(10);

            _oldFftCount++;
            _oldEqCount++;
            if (_oldFftCount == _oldFft.Length)
                _oldFftCount = 0;
            if (_oldEqCount == _oldEq.Length)
                _oldEqCount = 0;
        }
    }

    private void ProcessEq(double[] fftData)
    {
        if (fftData.Average() > 0.05)
        {
            foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Pictures))
                device.LightRequest.FullColor(Color.Black);
            
            var chunks = fftData.Chunk(fftData.Length / EqSize).ToArray();
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
    
    private void Process(double[] fftData)
    {
        if (fftData.Average() > 0.05)
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
                differences[i] = fftData[i] / (averages[i] * 2);

            var colors = new Color[295];
            for (int i = 0; i < colors.Length; i++)
            {
                var percentage = differences[i];
                if (percentage < 0)
                    percentage = 0;
                else if (percentage > 1 || differences[i] < 0.05)
                    percentage = 1;
                colors[i] = Color.FromArgb(0, 0, Convert.ToInt32(percentage * byte.MaxValue));
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
        {
            foreach (var device in _deviceService.Devices.Where(p => p.Mode == DeviceMode.Light))
            {
                device.LightRequest.Off();
                var data = device.LightRequest.ToByteArray();
                _udpClient.Send(data, data.Length, device.Hostname, device.Port);
            }
            _hubContext.Clients.All.SendAsync("UpdateChart", new double[fftData.Length]);
        }

        _oldFft[_oldFftCount] = fftData;
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