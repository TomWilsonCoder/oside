using System.Diagnostics;

public interface ICompilerOutput<SourceType> {
    string OutputFile { get; }

    ICompileOutputEntry[] Errors { get; }
    ICompileOutputEntry[] Warnings { get; }

    SourceType Source { get; }

    IEmulator CreateEmulator();
}