using System;
using System.IO;
using System.Threading;

public class ProjectDirectory : ProjectEntity {
    private long[] p_ChildDirectories;
    internal long[] p_ChildFiles;
    private object p_SyncLock = new object();
    
    internal ProjectDirectory(string name, Project Project) : base(name, true, Project) {
        p_ChildDirectories = new long[0];
        p_ChildFiles = new long[0];
    }

    public bool IsRoot { get { return InstanceID == 0; } }

    internal override void Load(Stream stream) {
        //load the base entity
        base.Load(stream);

        Monitor.Enter(p_SyncLock);

        //read the sub-directory instance id's
        int sDirCount = Helpers.DecodeInt32(stream);
        p_ChildDirectories = new long[sDirCount];
        for (int c = 0; c < sDirCount; c++) {
            p_ChildDirectories[c] = Helpers.DecodeInt64(stream);
        }

        //read the sub-file instance id's
        int sFileCount = Helpers.DecodeInt32(stream);
        p_ChildFiles = new long[sFileCount];
        for (int c = 0; c < sFileCount; c++) {
            p_ChildFiles[c] = Helpers.DecodeInt64(stream);
        }

        Monitor.Exit(p_SyncLock);
    }
    internal override void Save(Stream stream) {
        //save base data
        base.Save(stream);

        Monitor.Enter(p_SyncLock);

        //write child directories
        Helpers.EncodeInt32((int)p_ChildDirectories.Length, stream);
        for (int c = 0; c < p_ChildDirectories.Length; c++) {
            Helpers.EncodeInt64(p_ChildDirectories[c], stream);
        }

        //write child files
        Helpers.EncodeInt32((int)p_ChildFiles.Length, stream);
        for (int c = 0; c < p_ChildFiles.Length; c++) {
            Helpers.EncodeInt64(p_ChildFiles[c], stream);
        }

        Monitor.Exit(p_SyncLock);
    }

    public delegate bool EnumerateEntityCallback(ProjectEntity entity);
    public bool Enumerate(bool recursive, EnumerateEntityCallback callback) { 
        //get the files and directories
        ProjectDirectory[] dirs = GetDirectories();
        ProjectFile[] files = GetFiles();

        //enumerate
        for (int c = 0; c < dirs.Length; c++) {
            if (!callback(dirs[c])) { return false; }

            //recursion?
            if (recursive) {
                if (!dirs[c].Enumerate(true, callback)) { return false; }
            }
        }
        for (int c = 0; c < files.Length; c++) {
            if (!callback(files[c])) { return false; }
        }

        return true;
    }

    public ProjectDirectory[] GetDirectories() {
        lock (p_SyncLock) {
            ProjectDirectory[] buffer = new ProjectDirectory[p_ChildDirectories.Length];
            for (int c = 0; c < buffer.Length; c++) {
                buffer[c] = Project.ResolveDirectory(p_ChildDirectories[c]);
            }
            return buffer;
        }
    }
    public ProjectFile[] GetFiles() {
        lock (p_SyncLock) {
            ProjectFile[] buffer = new ProjectFile[p_ChildFiles.Length];
            for (int c = 0; c < buffer.Length; c++) {
                buffer[c] = Project.ResolveFile(p_ChildFiles[c]);
            }
            return buffer;
        }
    }

    public ProjectDirectory GetDirectory(string name) {
        lock (p_SyncLock) {
            name = name.ToLower();
            for (int c = 0; c < p_ChildDirectories.Length; c++) {
                ProjectDirectory dir = Project.ResolveDirectory(p_ChildDirectories[c]);
                if (dir.Name.ToLower() == name) {
                    return dir;
                }
            }
            return null;
        }
    }
    public ProjectFile GetFile(string name) {
        lock (p_SyncLock) {
            name = name.ToLower();

            for (int c = 0; c < p_ChildFiles.Length; c++) {
                ProjectFile file = Project.ResolveFile(p_ChildFiles[c]);
                if (file.Name.ToLower() == name) {
                    return file;
                }
            }
            return null;
        }
    }

    public bool DirectoryExists(string name) {
        return GetDirectory(name) != null;
    }
    public bool FileExists(string name) {
        return GetFile(name) != null;
    }
    public bool EntityExists(string name) {
        return DirectoryExists(name) ||
               FileExists(name);
    }

    public ProjectDirectory CreateDirectory(string name) { 
        //valid name?
        if (!Helpers.ValidFilename(name)) {
            throw new Exception("Invalid directory name \"" + name + "\"");
        }

        //already exists?
        ProjectDirectory buffer = GetDirectory(name);
        if (FileExists(name)) { return null; }
        if (buffer != null) { return buffer; }

        Monitor.Enter(p_SyncLock);

        //create it
        buffer = new ProjectDirectory(name, Project);
        buffer.AssignNewID();
        buffer.p_ParentID = InstanceID;

        //create the physical directory to associate with this directory
        if (!Directory.Exists(buffer.PhysicalLocation)) {
            Directory.CreateDirectory(buffer.PhysicalLocation);
        }

        //add to the directory children
        Array.Resize(ref p_ChildDirectories, p_ChildDirectories.Length + 1);
        p_ChildDirectories[p_ChildDirectories.Length - 1] = buffer.InstanceID;
        Project.RegisterEntity(buffer, true);

        Monitor.Exit(p_SyncLock);
        Project.Save();
        return buffer;
    }
    public ProjectFile CreateFile(string name) {
        if (!Helpers.ValidFilename(name)) {
            throw new Exception("Invalid directory name \"" + name + "\"");
        }

        //does the file already exist?
        ProjectFile buffer = GetFile(name);
        if (DirectoryExists(name)) { return null; }
        if (buffer != null) { return buffer; }

        //make sure that the physical directory that contains
        //the physical file exists.
        string parentDirectory = new DirectoryInfo(PhysicalLocation).FullName;
        if (!Directory.Exists(parentDirectory)) {
            Directory.CreateDirectory(parentDirectory);
        }

        Monitor.Enter(p_SyncLock);

        //create it
        buffer = new ProjectFile(name, Project);
        buffer.AssignNewID();
        buffer.p_ParentID = InstanceID;

        //create the physical file on disk
        File.WriteAllText(buffer.PhysicalLocation, "");

        //add it to the file list
        Array.Resize(ref p_ChildFiles, p_ChildFiles.Length + 1);
        p_ChildFiles[p_ChildFiles.Length - 1] = buffer.InstanceID;
        Project.RegisterEntity(buffer, false);

        //clean up
        Monitor.Exit(p_SyncLock);
        Project.Save();
        return buffer;
    }

    public override void Delete(bool triggerSave) {
        //do not allow the root directory to be deleted
        if (InstanceID == 0) { return; }

        Monitor.Enter(p_SyncLock);

        //trigger delete on all sub directories
        ProjectDirectory[] dirs = GetDirectories();
        for (int c = 0; c < dirs.Length; c++) {
            dirs[c].Delete(false);
        }

        //delete all sub-files
        ProjectFile[] files = GetFiles();
        for (int c = 0; c < files.Length; c++) {
            files[c].Delete(false);
        }

        //delete this directory from the parent
        ProjectDirectory parent = Parent;
        Helpers.RemoveObject(ref parent.p_ChildDirectories, InstanceID);
        Project.DeleteEntity(this, true);

        //delete the physical directory
        if (Directory.Exists(PhysicalLocation)) {
            Directory.Delete(PhysicalLocation, true);
        }

        //clean up
        if (triggerSave) {
            Project.Save();
        }

        Monitor.Exit(p_SyncLock);
    }
}