
public interface ICompiler {

    ICompilerOutput<Solution> Compile(Solution solution, StandardOutputCallback standardOutput);
    ICompilerOutput<Project> CompileProject(Project project, StandardOutputCallback standardOutput);

}