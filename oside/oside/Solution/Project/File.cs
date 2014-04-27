using System;
using System.IO;
using System.Text;

public class ProjectFile : ProjectEntity {
    internal ProjectFile(string name, Project Project) : base(name, false, Project) { }

    public string NameExcludingExtension {
        get { 
            //is there an extension?
            string extension = Extension;
            if (extension == "") { return Name; }

            //remove the extension by removing it
            //from the end of the name string
            int index = Name.Length - extension.Length;
            return Name.Substring(0, index);
        }
    }
    public string Extension {
        get {
            string[] split = Name.Split('.');

            //does this file contain an extension?
            if (split.Length == 1) { return ""; }

            //just return the last "." entry in the name
            return "." + split[split.Length - 1];
        }
    }

    public ProjectFileType FileType {
        get {
            if (Extension == "") { return ProjectFileType.Unknown; }
            try {
                return (ProjectFileType)Enum.Parse(
                        typeof(ProjectFileType),
                        Extension.Substring(1),
                        true
                );
            }
            catch { }
            return ProjectFileType.Unknown;
        }
    }

    public Stream OpenStream(FileMode mode) {
        if (mode == FileMode.Open) { mode = FileMode.OpenOrCreate; }
        return new FileStream(PhysicalLocation, mode);
    }

    public byte[] ReadAllBytes() {
        //read all the data from the file
        Stream str = OpenStream(FileMode.Open);
        byte[] buffer = new byte[str.Length];
        str.Read(buffer, 0, buffer.Length);

        //clean up
        str.Close();
        return buffer;
    }
    public string ReadAllText() {
        byte[] data = ReadAllBytes();
        string buffer = Encoding.ASCII.GetString(data);
        return buffer;
    }
    public string[] ReadAllLines() {
        string[] lines = ReadAllText().Split('\n');
        
        //remove carriage return characters ('\r')
        for (int c = 0; c < lines.Length; c++) {
            lines[c] = lines[c].Replace("\r", "");
        }
        return lines;
    }

    public void WriteAllBytes(byte[] data) {
        Stream str = OpenStream(FileMode.Create);
        str.Write(data, 0, data.Length);
        str.Close();
    }
    public void WriteAllText(string contents) {
        WriteAllBytes(Encoding.ASCII.GetBytes(contents));
    }
    public void WriteAllLines(string[] lines) {
        WriteAllText(Helpers.Flatten(lines, "\r\n"));
    }

    public override void Delete(bool triggerSave) {
        //delete the physical file on disk
        if (File.Exists(PhysicalLocation)) {
            File.Delete(PhysicalLocation);
        }

        //delete from the directory
        ProjectDirectory parent = Parent;
        Helpers.RemoveObject(ref parent.p_ChildFiles, InstanceID);
        Project.DeleteEntity(this, false);

        if (triggerSave) {
            Project.Save();
        }
    }
}