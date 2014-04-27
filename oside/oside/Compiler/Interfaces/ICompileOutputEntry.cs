
public interface ICompileOutputEntry {
    Solution Solution { get; }
    Project Project { get; }

    ProjectFile File { get; }
    int Line { get; }
    int Column { get; }

    string Message { get; }
}