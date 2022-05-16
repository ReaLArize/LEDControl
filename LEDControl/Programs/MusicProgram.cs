using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LEDControl.Hubs;
using LEDControl.Services;
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
            channels = 2,
            format = pa_sample_format.PA_SAMPLE_FLOAT32LE,
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

        nuint chunkSize = 1024;
        var buffer = new byte[chunkSize];
        while (!token.IsCancellationRequested)
        {
            PulseSimpleApi.pa_simple_flush(_apiRead, &error);
            if (PulseSimpleApi.pa_simple_read(_apiRead, buffer, chunkSize, &error) < 0)
                Console.WriteLine("error");

            var rawData = Array.ConvertAll(buffer, Convert.ToDouble);
            var fftData = FftSharp.Transform.FFTpower(rawData);
            Process(fftData.Skip(1).ToArray());
            Thread.Sleep(10);
        }
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