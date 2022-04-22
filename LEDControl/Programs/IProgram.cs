using System;

namespace LEDControl.Programs;

public interface IProgram
{
    void Init(IServiceProvider serviceProvider);
    
    void Run();

    void Stop();
}