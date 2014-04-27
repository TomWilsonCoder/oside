using System;
using System.IO;
using System.Diagnostics;

public sealed class StandardCompileOutput<SourceType> : ICompilerOutput<SourceType> {
    private string p_OutputFile;
    private SourceType p_Source;

    internal StandardCompileOutput(string outputPath, SourceType source) {
        p_OutputFile = outputPath;
        p_Source = source;
    }

    internal ICompileOutputEntry[] p_Errors = new ICompileOutputEntry[0];
    internal ICompileOutputEntry[] p_Warnings = new ICompileOutputEntry[0];

    public SourceType Source { get { return p_Source; } }
    public string OutputFile { get { return p_OutputFile; } }
    public ICompileOutputEntry[] Errors { get { return p_Errors; } }
    public ICompileOutputEntry[] Warnings { get { return p_Warnings; } }

    public IEmulator CreateEmulator() {
        //errors?
        if (Errors.Length != 0) { return null; }

        //run qemu
        return new QemuEmulator(p_OutputFile);
    }

    internal void addError(ProjectFile file, int line, int column, string msg) {
        Solution solution = (file == null ? null : file.Project.Solution);
        Project project = (file == null ? null : file.Project);
        Array.Resize(ref p_Errors, p_Errors.Length + 1);
        p_Errors[p_Errors.Length - 1] = new StandardCompileEntry(
            solution,
            project,
            file,
            line,
            column,
            msg);
    }
    internal void addWarning(ProjectFile file, int line, int column, string msg) {
        Solution solution = (file == null ? null : file.Project.Solution);
        Project project = (file == null ? null : file.Project);
        Array.Resize(ref p_Warnings, p_Warnings.Length + 1);
        p_Warnings[p_Warnings.Length - 1] = new StandardCompileEntry(
            solution,
            project,
            file,
            line,
            column,
            msg);
    }
}