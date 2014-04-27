
public struct EmulationRegister {
    private string p_Name;
    private long p_Value;
    public EmulationRegister(string name, long value) {
        p_Name = name;
        p_Value = value;
    }

    public string Name { get { return p_Name; } }
    public long Value { get { return p_Value; } }

    public override string ToString() {
        return Name + " = " + Value;
    }
}