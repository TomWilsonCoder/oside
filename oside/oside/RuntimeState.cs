using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

public static class RuntimeState {
    private static object p_SyncLock = new object();
    private const string p_Filename = "./osidestate";

    /*
        The data in the stream is encoded as:
     *      1byte + length of string = name
     *      2bytes = length of data
     *      xbytes = data
    */

    public static void CreateObject(string name, object value) { 
        //check the type of the value
        if (value == null || !value.GetType().IsValueType) {
            throw new Exception("Value must not be null and a value type!");
        }

        //write the raw byte data of the object into 
        //data which is represented by the new name
        CreateObjectData(name, encodeObject(value));
    }
    public static void CreateObjectData(string name, byte[] data) {
        //name already exists?
        if (ObjectExists(name)) {
            throw new Exception("Object \"" + name + "\" already exists!");
        }
        
        //prepare the data stream for writing
        lock (p_SyncLock) {
            Stream stream = openStream();
            stream.Seek(0, SeekOrigin.End);

            //write the name and length of the data
            Helpers.WriteString255(name, stream);
            Helpers.EncodeInt16((short)data.Length, stream);

            //write the data
            stream.Write(data, 0, data.Length);
            stream.Flush();
            stream.Close();
        }
    }

    public static object GetObject(string name, Type baseType, object defaultValue) {
        //if a default type is specified, then we attempt
        //to get the value of the object by it's name.
        //if it fails e.g: name not found, the default value is returned.
        if (defaultValue == null) {
            return GetObject(name, baseType);
        }
        else {
            try {
                return GetObject(name, baseType);
            }
            catch {
                return defaultValue;
            }
        }
    }
    public static object GetObject(string name, Type baseType) { 
        //the type MUST be a struct
        if (!baseType.IsValueType) {
            throw new Exception("Invalid base type");
        }

        //allocate memory for the new type and copy the data from
        //the data stream into it.
        byte[] sourceData = GetObjectData(name);
        IntPtr objectPtr = Marshal.AllocHGlobal(sourceData.Length);
        Marshal.Copy(sourceData, 0, objectPtr, sourceData.Length);

        //return a managed type
        return Marshal.PtrToStructure(objectPtr, baseType);
    }
    public static byte[] GetObjectData(string name) { 
        //get the data pointer given to the name given
        short length = -1;
        long ptr = lookupPointer(name, out length);

        //does the length of the data match that of the length
        //of the type?
        if (ptr == -1) {
            throw new Exception("Name is invalid");
        }

        //read the data
        return read(ptr, length);
    }

    public static void SetObject(string name, object value) {
        //check the type of the value
        if (value == null || !value.GetType().IsValueType) {
            throw new Exception("Value must not be null and a value type!");
        }

        SetObjectData(name, encodeObject(value));
    }
    public static void SetObjectData(string name, byte[] data) {
        //get the data pointer given to the name
        short len;
        long ptr = lookupPointer(name, out len);
        if (len == -1) {
            //create it instead if it's not found.
            CreateObjectData(name, data);
            return;
        }

        //check length
        if (data.Length != len) {
            throw new Exception("Value does not match the range of data allocated.");
        }

        //write the data
        lock (p_SyncLock) {
            Stream stream = openStream();
            stream.Position = ptr;
            stream.Write(data, 0, len);
            stream.Flush();
            stream.Close();
        }
    }

    public static void DeleteObject(string name) { 
        //get the data pointer that the name is is allocating.
        short len;
        long ptr = lookupPointer(name, out len);
        if (len == -1) { return; }

        lock (p_SyncLock) { 
            //seek to the where the name is stored in the data stream
            Stream stream = openStream();
            stream.Position = ptr - 2 - name.Length;

            //overwrite the name in the stream with nullbytes
            //to basically render the block of memory useless.
            stream.Write(
                new byte[name.Length],
                0,
                name.Length);
            stream.Flush();
            stream.Close();
        }
    }

    public static bool ObjectExists(string name) {
        short len;
        return lookupPointer(name, out len) != -1;
    }

    public static void SetObjectString(string name, string data) {
        SetObjectData(
            name,
            Encoding.ASCII.GetBytes(data));
    }
    public static string GetObjectString(string name) {
        return Encoding.ASCII.GetString(
             GetObjectData(name));
    }

    public static void Clear() {
        lock (p_SyncLock) {
            //delete the file
            while (true) {
                try {
                    File.Delete(p_Filename);
                    break;
                }
                catch { }
            }
        }
    }

    private static byte[] encodeObject(object value) {
        //read the raw memory data from the value into a byte array
        byte[] buffer = new byte[Marshal.SizeOf(value)];
        IntPtr valuePtr = Marshal.AllocHGlobal(buffer.Length);
        Marshal.StructureToPtr(value, valuePtr, false);
        Marshal.Copy(valuePtr, buffer, 0, buffer.Length);
        return buffer;
    }
    private static long lookupPointer(string name, out short dataLength) { 
        //ready through the stream until we hit the name we want
        lock (p_SyncLock) {
            Stream stream = openStream();
            stream.Position = 0;
            while (stream.Position < stream.Length) {
                string nameRead = Helpers.ReadString255(stream);
                short length = Helpers.DecodeInt16(stream);

                if (nameRead == name) {
                    dataLength = length;
                    long position = stream.Position;
                    stream.Close();
                    return position;
                }

                stream.Position += length;
            }
            stream.Close();
        }
        dataLength = -1;
        return -1;
    }
 
    private static byte[] read(long ptr, short length) {
        lock (p_SyncLock) {
            Stream stream = openStream();
            stream.Position = ptr;
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);
            stream.Close();
            return buffer;
        }
    }

    public static FileStream openStream() {
        //get mutual exclusion of the state file
        while (true) {
            try {
                return new FileStream(
                    p_Filename,
                    FileMode.OpenOrCreate);
            }
            catch { }
        }
    }

    public struct COORD {
        public COORD(int x, int y) {
            X = x;
            Y = y;
        }
        public int X, Y;
    }
    public struct SIZE {
        public SIZE(int width, int height) {
            Width = width;
            Height = height;
        }
        public int Width, Height;
    }
    public struct RECTANGLE {
        public RECTANGLE(int x, int y, int width, int height) {
            Location = new COORD(x, y);
            Size = new SIZE(width, height);
        }

        public COORD Location;
        public SIZE Size;

        public int X { get { return Location.X; } }
        public int Y { get { return Location.Y; } }
        public int Width { get { return Size.Width; } }
        public int Height { get { return Size.Height; } }
    }
}