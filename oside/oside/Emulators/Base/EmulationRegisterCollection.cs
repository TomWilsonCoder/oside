public sealed class EmulationRegisterCollection {
    private EmulationRegister[] p_Registers;
    private int p_Length;

    internal EmulationRegisterCollection(EmulationRegister[] registers) {
        p_Registers = registers;

        //set a local copy of the length 
        //so that in future we don't have
        //redundancy hopping across object properties.
        p_Length = registers.Length;
    }

    public int Length { get { return p_Length; } }

    public EmulationRegister? this[string name] {
        get { 
            //look for the register by the name
            name = name.ToLower();
            for (int c = 0; c < p_Length; c++) {
                if (p_Registers[c].Name.ToLower() == name) {
                    return p_Registers[c];
                }
            }

            //not found
            return null;
        }
    }
    public EmulationRegister? this[int index] {
        get { 
            //check for overflow
            if (index >= p_Length) {
                return null;
            }
            return p_Registers[index];
        }
    }

}