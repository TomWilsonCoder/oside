using System;
using System.IO;

public abstract class ProjectEntity {
    private Project p_Project;
    private string p_Name;
    private bool p_IsDirectory;
    private long p_InstanceID;
    internal long p_ParentID;

    internal ProjectEntity(string name, bool isDirectory, Project Project) {
        p_Name = name;
        p_IsDirectory = isDirectory;
        p_Project = Project;
    }

    internal virtual void Save(Stream stream) {
        //write the instance ID's
        Helpers.EncodeInt64(p_InstanceID, stream);
        Helpers.EncodeInt64(p_ParentID, stream);

        //write the attributes
        byte[] write = new byte[p_Name.Length + 2];
        write[0] = (byte)p_Name.Length;
        write[1] = (byte)(p_IsDirectory ? 255 : 0);

        //write the name
        for (int c = 0; c < p_Name.Length; c++) {
            write[c + 2] = (byte)p_Name[c];
        }

        //write it
        stream.Write(write, 0, write.Length);
    }
    internal virtual void Load(Stream stream) { 
        //read the instance id
        p_InstanceID = Helpers.DecodeInt64(stream);
        p_ParentID = Helpers.DecodeInt64(stream);

        //read the header for the entity
        byte[] header = new byte[2];
        stream.Read(header, 0, 2);

        //read the directory flag
        p_IsDirectory = header[1] == 255;

        //read the name
        byte nameLength = header[0];
        byte[] nameRead = new byte[nameLength];
        p_Name = "";
        stream.Read(nameRead, 0, nameRead.Length);
        for (byte c = 0; c < nameLength; c++) {
            p_Name += (char)nameRead[c];
        }
    }

    internal virtual long AssignNewID() {
        p_InstanceID = Project.p_NextEntityID++;
        return p_InstanceID;
    }

    public long InstanceID { get { return p_InstanceID; } }

    public string Name { get { return p_Name; } }
    public string FullName {
        get {
            //if this instance is a root entity, don't return anything
            if (this is ProjectDirectory && InstanceID == 0) {
                return "";
            }

            //keep seeking back up through the parents
            //and add it's name to the list
            string[] buffer = { Name };
            ProjectDirectory current = Parent;
            while (current != null) {
                //ignore root directory!
                if (current.IsRoot) { break; }

                Array.Resize(ref buffer, buffer.Length + 1);
                buffer[buffer.Length - 1] = current.Name;
                current = current.Parent;
            }

            //flatten the list into a single string
            //(we do this in reverse since this instance is the first record)
            Array.Reverse(buffer);
            return Helpers.Flatten(buffer, "\\");
        }
    }

    public string PhysicalLocation {
        get { 
            //get the physical location of the directory
            //which contains the project file
            string buffer = new FileInfo(Project.Filename).Directory.FullName;

            //add the full name of the entity to the buffer
            //(including a "src" directory)
            buffer += "\\src\\" + FullName;
            return buffer;
        }
    }

    public Project Project { get { return p_Project; } }
    public ProjectDirectory Parent {
        get {
            return Project.ResolveDirectory(p_ParentID);
        }
    }

    public void Delete() { Delete(true); }
    public abstract void Delete(bool triggerSave);

    public override string ToString() {
        return FullName;
    }
}