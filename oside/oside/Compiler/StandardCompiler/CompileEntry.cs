public sealed class StandardCompileEntry : ICompileOutputEntry {

    private Solution p_Solution;
    private Project p_Project;
    private ProjectFile p_File;
    private int p_Line, p_Column;
    private string p_Message;

    internal StandardCompileEntry(Solution solution, Project project, ProjectFile file, int line, int col, string message) {
        p_Solution = solution;
        p_Project = project;
        p_File = file;
        p_Line = line;
        p_Column = col;
        p_Message = message;
    }

    public Solution Solution { get { return p_Solution; } }
    public Project Project { get { return p_Project; } }

    public ProjectFile File { get { return p_File; } }
    public int Line { get { return p_Line; } }
    public int Column { get { return p_Column; } }
    public string Message { get { return p_Message; } }
}