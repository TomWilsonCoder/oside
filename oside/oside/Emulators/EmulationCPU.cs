public struct EmulationCPU {
    private int p_Index;
    private long p_ProgramCounter;
    private bool p_IsHalted;
    private bool p_IsCurrent;

    internal EmulationCPU(int index, bool isCurrent, long programCounter, bool isHalted) {
        p_Index = index;
        p_ProgramCounter = programCounter;
        p_IsCurrent = isCurrent;
        p_IsHalted = isHalted;
    }

    public int Index { get { return p_Index; } }
    public long ProgramCounter { get { return p_ProgramCounter; } }
    public bool Halted { get { return p_IsHalted; } }
    public bool Current { get { return p_IsCurrent; } }
}