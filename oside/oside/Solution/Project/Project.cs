using System;
using System.IO;

public class Project : IDisposable {
    private ProjectEntity[] p_Directories;
    private ProjectEntity[] p_Files;
    private header p_Header;
    private object p_SyncLock = new object();
    private string p_Filename;
    private string p_ProjectName;
    private string p_BuildDirectory;

    internal Solution p_Solution;

    internal long p_NextEntityID = 0;
    private ProjectType p_ProjectType;

    public Project(string filename) {
        p_Filename = filename;

        //perform restore (if needed)
        Helpers.Restore(filename);

        //open the file
        FileStream fileStream = new FileStream(filename, FileMode.Open);

        //read the header
        p_Header = new header();
        p_Header.load(fileStream, this);
        
        //read the project name
        p_ProjectName = Helpers.ReadString255(fileStream);

        //read the build directory
        p_BuildDirectory = Helpers.ReadString(fileStream);

        //read the directories
        int directoryLength = Helpers.DecodeInt32(fileStream);
        p_Directories = new ProjectEntity[directoryLength];
        for (int c = 0; c < directoryLength; c++) {
            p_Directories[c] = new ProjectDirectory(null, this);
            p_Directories[c].Load(fileStream);
        }

        //read the files
        int fileLength = Helpers.DecodeInt32(fileStream);
        p_Files = new ProjectEntity[fileLength];
        for (int c = 0; c < fileLength; c++) {
            p_Files[c] = new ProjectFile(null, this);
            p_Files[c].Load(fileStream);
        }

        //clean up
        fileStream.Close();
    }
    
    public void Save() {
        lock (p_SyncLock) {
            //backup the file in case this process is interrupted
            string backup = Helpers.Backup(p_Filename);

            //create the file stream
            FileStream fs = new FileStream(p_Filename, FileMode.Create);

            //write the header
            p_Header.save(fs, this);

            //write the project name
            Helpers.WriteString255(p_ProjectName, fs);

            //write the build directory
            Helpers.WriteString(p_BuildDirectory, fs);

            //write the directories
            Helpers.EncodeInt32(p_Directories.Length, fs);
            for (int c = 0; c < p_Directories.Length; c++) {
                p_Directories[c].Save(fs);
            }

            //write the files
            Helpers.EncodeInt32(p_Files.Length, fs);
            for (int c = 0; c < p_Files.Length; c++) {
                p_Files[c].Save(fs);
            }

            //clean up
            fs.Flush();
            fs.Close();
            File.Delete(backup);
        }
    }

    internal void RegisterEntity(ProjectEntity entity, bool directory) {
        if (directory) {
            Array.Resize(ref p_Directories, p_Directories.Length + 1);
            p_Directories[p_Directories.Length - 1] = entity;
        }
        else {
            Array.Resize(ref p_Files, p_Files.Length + 1);
            p_Files[p_Files.Length - 1] = entity;
        }
    }
    internal void DeleteEntity(ProjectEntity entity, bool directory) {
        if (directory) {
            Helpers.RemoveObject(ref p_Directories, entity);
        }
        else {
            Helpers.RemoveObject(ref p_Files, entity);
        }
    }

    public ProjectDirectory Root { get { return (ProjectDirectory)p_Directories[0]; } }
    public string Filename { get { return p_Filename; } }
    public string ProjectName {
        get { return p_ProjectName; }
        set {
            p_ProjectName = value;
            Save();
        }
    }

    public Solution Solution { get { return p_Solution; } }

    public ProjectType ProjectType {
        get { return p_ProjectType; }
        set {
            p_ProjectType = value;
            Save();
        }
    }

    public string BuildDirectory {
        get {
            lock (p_SyncLock) {
                return Helpers.GetAbsolutePath(
                    new FileInfo(Filename).Directory.FullName,
                    p_BuildDirectory);
            }
        }
        set {
            lock (p_SyncLock) {
                //set the build position relative to the project file
                value = new DirectoryInfo(value).FullName;
                value = value.Replace(
                        new FileInfo(Filename).Directory.FullName + "\\",
                        "");
                p_BuildDirectory = value;
                Save();
            }
        }
    }

    public static Project CreateProject(string filename, string projectName) {
        return CreateProject(
            filename,
            projectName,
            new FileInfo(filename).Directory.FullName + "\\build");
    }
    public static Project CreateProject(string filename, string projectName, string buildDirectory) { 
        //create the build directory
        if (!Directory.Exists(buildDirectory)) {
            Directory.CreateDirectory(buildDirectory);
        }

        //make the build directory relative to project file
        buildDirectory = new DirectoryInfo(buildDirectory).FullName;
        buildDirectory = buildDirectory.Replace(
                new FileInfo(filename).Directory.FullName + "\\",
                "");

        //create the initial template for a Project
        File.WriteAllBytes(filename, new byte[256 + 2]);
        Project buffer = new Project(filename);

        //create the root directory
        buffer.p_NextEntityID = 1;
        buffer.p_ProjectName = projectName;
        buffer.p_BuildDirectory = buildDirectory;
        buffer.p_Directories = new ProjectDirectory[] { 
            new ProjectDirectory("{root}", buffer)
        };

        //cause a save so the Project can do all
        //the creating for us
        buffer.Save();
        return buffer;
    }

    /*Resolve functions*/
    public ProjectDirectory ResolveDirectory(long id) {
        return (ProjectDirectory)ResolveEntity(id, true);
    }
    public ProjectFile ResolveFile(long id) {
        return (ProjectFile)ResolveEntity(id, false);
    }
    public ProjectEntity ResolveEntity(long id, bool isDirectory) {
        lock (p_SyncLock) {
            //deturmine what list to enumerate through depending
            //on the isDirectory flag.
            ProjectEntity[] select = isDirectory ? p_Directories : p_Files;

            //look for an entity with the instance id
            for (int c = 0; c < select.Length; c++) {
                if (select[c].InstanceID == id) {
                    return select[c];
                }
            }
            return null;
        }
    }

    public void Dispose() {
        p_Filename = null;
        p_Directories = null;
        p_Files = null;
        p_Header = null;
        p_ProjectName = null;
    }
    public override string ToString() {
        return p_ProjectName;
    }

    private class header {

        public void save(Stream stream, Project Project) {
            //randomly fill 256 bytes of data to be the 
            //initial header data to act as padding
            //then fill the data in with actual information.
            byte[] buffer = new byte[256];
            MemoryStream bufferStream = new MemoryStream();
            Random rnd = new Random();
            for (int c = 0; c < 256; c++) {
                buffer[c] = (byte)rnd.Next(0, 256);
            }


            byte[] data = getData(Project);
            buffer[0] = (byte)data.Length;
            bufferStream.Write(buffer, 0, 256);
            bufferStream.Position = 1;
            bufferStream.Write(data, 0, data.Length);
            bufferStream.Position = 0;
            bufferStream.Read(buffer, 0, buffer.Length);

            stream.Write(buffer, 0, 256);
            
        }
        public void load(Stream stream, Project Project) { 
            //load the header data
            byte headerLength = (byte)stream.ReadByte();
            byte[] header = defaultData;
            stream.Read(header, 0, headerLength);
            MemoryStream headerStream = new MemoryStream();
            headerStream.Write(header, 0, headerLength);
            headerStream.Position = 0;

            //read the next entity id
            Project.p_NextEntityID = Helpers.DecodeInt64(headerStream);
            Project.p_ProjectType = (ProjectType)Helpers.DecodeInt16(headerStream);

            //skip over the rest of the header which is just padding
            stream.Position = 256;
        }

        private byte[] getData(Project Project) {
            MemoryStream ms = new MemoryStream();

            Helpers.EncodeInt64(Project.p_NextEntityID, ms);
            Helpers.EncodeInt16((short)Project.p_ProjectType, ms);

            //convert the stream to bytes and return it
            ms.Position = 0;
            byte[] buffer = new byte[ms.Length];
            ms.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        /*Used for compatability to account for missing data in the header.*/
        private byte[] defaultData {
            get {
                MemoryStream ms = new MemoryStream();

                //write the default data
                Helpers.EncodeInt64(0, ms);
                Helpers.EncodeInt16(0, ms);

                byte[] buffer = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }
    }
}