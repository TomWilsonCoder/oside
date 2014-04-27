using System;
using System.IO;
using System.Text;

public class Solution {
    private Project[] p_Projects = new Project[0];
    private string p_SolutionName;
    private string p_Filename;
    private object p_SyncLock = new object();
    private string p_BuildDirectory = "";

    public Solution(string filename) {
        //perform recovery (if needed)
        Helpers.Restore(filename);

        FileStream stream = new FileStream(filename, FileMode.Open);
        p_Filename = filename;

        //read the solution name
        p_SolutionName = Helpers.ReadString255(stream);

        //read the build directory
        p_BuildDirectory = readPath(stream);

        //read all the projects
        while (stream.Position < stream.Length) {
            string path = readPath(stream);

            //convert the path from relative (if so) to absolute
            path = Helpers.GetAbsolutePath(
                        new FileInfo(p_Filename).Directory.FullName,
                        path
                   );

            Project project = new Project(path);
            project.p_Solution = this;
            Array.Resize(ref p_Projects, p_Projects.Length + 1);
            p_Projects[p_Projects.Length - 1] = project;
        }
        stream.Close();
    }

    public void Save() {
        string dir = new FileInfo(p_Filename).Directory.FullName + "\\";
        
        lock (p_SyncLock) {
            //backup first in case of interruption
            string backup = Helpers.Backup(p_Filename);

            FileStream fs = new FileStream(p_Filename, FileMode.Create);

            //write the solution name
            Helpers.WriteString255(p_SolutionName, fs);

            //write the build directory
            writePath(p_BuildDirectory, fs);

            //write all the project filenames
            for (int c = 0; c < p_Projects.Length; c++) {
                //get the relative filename
                string relative = p_Projects[c].Filename;
                relative = relative.Replace(dir, "");

                writePath(relative, fs);
            }

            //clean up
            fs.Flush();
            fs.Close();
            File.Delete(backup);

        }
    }

    public Project AddProject(string path) {
        Project buffer = new Project(path);
        buffer.p_Solution = this;
        lock (p_SyncLock) {
            Array.Resize(ref p_Projects, p_Projects.Length + 1);
            p_Projects[p_Projects.Length - 1] = buffer;
        }
        Save();
        return buffer;
    }
    public bool RemoveProject(Project project) {
        lock (p_SyncLock) {
            bool result = Helpers.RemoveObject(ref p_Projects, project);
            if (!result) { return false; }
            Save();
            project.p_Solution = null;
            return result;
        }
    }
    public Project GetProject(string name) {
        name = name.ToLower();
        lock (p_SyncLock) {
            for (int c = 0; c < p_Projects.Length; c++) {
                if (p_Projects[c].ProjectName.ToLower() == name) {
                    return p_Projects[c];
                }
            }
            return null;
        }
    }
    public Project CreateProject(string name) {
        //valid project name?
        if (!Helpers.ValidFilename(name)) {
            throw new Exception("Invalid project name \"" + name + "\"");
        }

        //create the project directory
        DirectoryInfo dir = new FileInfo(p_Filename).Directory;
        string projDirectory = dir.FullName + "\\" + name;
        Directory.CreateDirectory(projDirectory);

        //create the project file
        string file = projDirectory + "\\" + name + ".osproj";
        Project proj = Project.CreateProject(file, name);
        proj.Dispose();

        //add it to the solution
        return AddProject(file);
    }

    public string SolutionName { 
        get { return p_SolutionName; }
        set {
            p_SolutionName = value;
            Save();
        }
    }
    public Project[] Projects { get { return p_Projects; } }
    public string BuildDirectory { 
        get {
            lock (p_SyncLock) {
                //get the absolute path of the build directory which is currently
                //relative to the solution.
                return Helpers.GetAbsolutePath(
                        new FileInfo(p_Filename).Directory.FullName,
                        p_BuildDirectory
                );
            }
        }
        set { 
            //set the build directory to be relative to the solution
            //directory
            lock (p_SyncLock) {
                value = new DirectoryInfo(value).FullName;
                value = value.Replace(
                        new FileInfo(p_Filename).Directory.FullName + "\\",
                        "");
                p_BuildDirectory = value;
                Save();
            }
        }
    }

    public string Filename { get { return new FileInfo(p_Filename).FullName; } }

    public static Solution CreateSolution(string filename, string name) {
        return CreateSolution(
            filename,
            name,
            new DirectoryInfo(
                new FileInfo(filename).Directory.FullName +
                "\\build").FullName);
    }
    public static Solution CreateSolution(string filename, string name, string buildDirectory) {
        //valid name?
        if (!Helpers.ValidFilename(name)) {
            throw new Exception("Invalid solution name \"" + name + "\"");
        }
        
        //create the build directory
        if (!Directory.Exists(buildDirectory)) {
            Directory.CreateDirectory(buildDirectory);
        }
        
        //get the relative path of the build directory
        buildDirectory = new DirectoryInfo(buildDirectory).FullName;
        buildDirectory = buildDirectory.Replace(
            new FileInfo(filename).Directory.FullName + "\\",
            "");
       
        //create the output stream
        FileStream fs = new FileStream(filename, FileMode.Create);

        //write the solution name and build directory
        Helpers.WriteString255(name, fs);
        writePath(buildDirectory, fs);

        fs.Close();
        return new Solution(filename);        
    }

    private static void writePath(string path, Stream stream) {
        Helpers.EncodeInt16((short)path.Length, stream);
        stream.Write(
            Encoding.ASCII.GetBytes(path),
            0,
            path.Length
        );
    }
    private static string readPath(Stream stream) {
        short length = Helpers.DecodeInt16(stream);
        byte[] buffer = new byte[length];
        stream.Read(buffer, 0, length);
        return Encoding.ASCII.GetString(buffer);
    }
}