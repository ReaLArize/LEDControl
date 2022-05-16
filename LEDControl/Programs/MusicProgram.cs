using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using LEDControl.Hubs;
using LEDControl.Services;
using MathNet.Numerics.IntegralTransforms;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using PulseAudioWrapper;

namespace LEDControl.Programs;

public unsafe class MusicProgram : IProgram
{
    private DeviceService _deviceService;
    private Task _workTask;
    private readonly CancellationTokenSource _tokenSource = new();
    private pa_simple* _apiRead;
    private IHubContext<MusicHub> _hubContext;

    public void Init(IServiceProvider serviceProvider)
    {
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
        pa_buffer_attr attr = new pa_buffer_attr()
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

        nuint chunkSize = 2048;
        var buffer = new byte[chunkSize];
        while (!token.IsCancellationRequested)
        {
            PulseSimpleApi.pa_simple_flush(_apiRead, &error);
            if (PulseSimpleApi.pa_simple_read(_apiRead, buffer, chunkSize, &error) < 0)
                Console.WriteLine("error");

            var rawData = Convert16BitToFloat(buffer);
            var fftData = rawData.Select(p => new Complex(p, 0)).ToArray();
            
            Fourier.Forward(fftData, FourierOptions.Default);
            Process(fftData.Take(fftData.Length / 2).Select( x => x.Magnitude ).ToArray());
            Thread.Sleep(10);
        }
    }
    
    public float[] Convert16BitToFloat(byte[] input)
    {
        int inputSamples = input.Length / 2; // 16 bit input, so 2 bytes per sample
        float[] output = new float[inputSamples];
        int outputIndex = 0;
        for(int n = 0; n < inputSamples; n++)
        {
            short sample = BitConverter.ToInt16(input,n*2);
            output[outputIndex++] = sample / 32768f;
        }
        return output;
    }

    private void Process(double[] fftData)
    {
        if (fftData.All(p => !double.IsNaN(p) && !double.IsInfinity(p)))
            _hubContext.Clients.All.SendAsync("UpdateChart", fftData).Wait();
        else
            _hubContext.Clients.All.SendAsync("UpdateChart", new double[fftData.Length]);
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
}