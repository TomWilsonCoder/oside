public interface IEmulator {
    #region State
    void Start(int memory, StandardOutputCallback standardOutput);
    void Stop();
    void Restart();
    void Suspend();
    void Resume();
    
    bool Running { get; }
    bool Suspended { get; }
    #endregion

    EmulationProcessor[] GetProcessors();
    void UpdateProcessor(ref EmulationProcessor processor);

    EmulationRegisterCollection GetRegisters(EmulationProcessor processor);


    #region Events
    event EmulatorEventHandler<object> Reset;

    event EmulatorEventHandler<object> Shutdown;

    event EmulatorEventHandler<object> Paused;
    event EmulatorEventHandler<object> Resumed;
    #endregion
}

public delegate void EmulatorEventHandler<a>(IEmulator sender, a args);
