using System;
using LEDControl.Programs;
using Microsoft.Extensions.Logging;

namespace LEDControl.Services;

public class ProgramService
{
    private readonly ILogger<ProgramService> _logger;
    private readonly object _executionLock;
    private readonly IServiceProvider _serviceProvider;
    public IProgram CurrentProgram;
    
    public ProgramService(IServiceProvider serviceProvider, ILogger<ProgramService> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _executionLock = new object();
    }

    public void Start<T>(T program) where T : IProgram
    {
        lock (_executionLock)
        {
            if (CurrentProgram != null)
            {
                _logger.LogInformation("Stopping current program {Name}", CurrentProgram.GetType().Name);
                CurrentProgram.Stop();
                CurrentProgram = null;
            }
            else
            {
                _logger.LogInformation("Starting program {Name}", program.GetType().Name);
                CurrentProgram = program;
                CurrentProgram.Init(_serviceProvider);
                CurrentProgram.Run();
            }
                
        }
    }

    public void Stop()
    {
        lock (_executionLock)
        {
            if (CurrentProgram == null) return;
            _logger.LogInformation("Stopping current program {Name}", CurrentProgram.GetType().Name);
            CurrentProgram.Stop();
            CurrentProgram = null;
        }
    }
}