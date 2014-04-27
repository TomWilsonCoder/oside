public interface IEmulator {
    void Start(int memory, StandardOutputCallback standardOutput);
    void Stop();
    void Suspend();
    void Resume();
    
    bool Running { get; }
    bool Suspended { get; }


    EmulationCPU[] GetProcessors();
    void UpdateProcessor(ref EmulationCPU processor);
}