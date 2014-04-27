/*
    DO NOT TOUCH! 
*/

using System;
using System.Drawing;
using System.Windows.Forms;

public class DebugRegisters : Form {
    private IEmulator p_Emulator;
    private EmulationProcessor[] p_Processors;
    private int p_CurrentProcessorIndex;

    public DebugRegisters(IEmulator emulator) { 
        //initialize the form
        ShowIcon = false;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;

        //get the processors
        p_Emulator = emulator;
        p_Processors = emulator.GetProcessors();

        //
        int[] col;
        getTable(out col);
    }

    public EmulationProcessor CurrentProcessor {
        get {
            return p_Processors[p_CurrentProcessorIndex];
        }
    }

    private string[][] getTable(out int[] columnWidths) {
        const int columnWidth = 8; //[column1=name][column2=value] etc...
        const int registersPerRow = 4; //columnWidth/2
        columnWidths = new int[columnWidth];

        //get the registers
        EmulationRegisterCollection registers = p_Emulator.GetRegisters(CurrentProcessor);
        
        //define the return table
        string[][] buffer = new string[(int)Math.Ceiling(registers.Length * 1.0f / registersPerRow)][];


        return null;
    }
    private void draw(Graphics buffer) { 
        
    }
}