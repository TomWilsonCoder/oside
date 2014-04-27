public interface IEmulator {
    void Start(int memory, StandardOutputCallback standardOutput);
    void Stop();
    void Suspend();
    void Resume();
    
    bool Running { get; }
    bool Suspended { get; }


    EmulationProcessor[] GetProcessors();
    void UpdateProcessor(ref EmulationProcessor processor);

    EmulationRegister[] GetRegisters(EmulationProcessor processor);
}