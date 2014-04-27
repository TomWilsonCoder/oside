using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

public static class Helpers {
    public static string RemoveWhitespaces(string str) {
        int offset = 0;
        while (offset < str.Length && str[offset] == ' ') { offset++; }
        if (offset == str.Length) { return ""; }
        str = str.Substring(offset);
        offset = 0;
        while (offset < str.Length && str[str.Length - 1 - offset] == ' ') { offset++; }
        str = str.Substring(0, str.Length - offset);
        return str;
    }

    public static int GetScopeEnd(char open, char close, string str, int position) {
        int openCount = 0;
        int closeCount = 0;
        while (position < str.Length) {
            SkipStrings(str, ref position);
            if (position == str.Length) { break; }

            if (str[position] == open) { openCount++; }
            if (str[position] == close) {
                closeCount++;
                if (openCount == closeCount) {
                    return position;
                }
            }
            position++;
        }
        return -1;
    }
    public static void SkipStrings(string str, ref int position) {
        if (str[position] == '"' || str[position] == '\'') {
            char endChar = str[position++];

            while (position < str.Length) {
                if (str[position] == endChar) { position++; break; }
                if (str[position] == '\\') { position++; }
                position++;
            }
        }
    }

    public static void SwapEntries<T>(ref T[] array, int indexX, int indexY) {
        T oldX = array[indexX];
        array[indexX] = array[indexY];
        array[indexY] = oldX;
    }

    public static Stream CreateTempFile(out string filename) { return CreateTempFile(40, out filename); }
    public static Stream CreateTempFile(int nameLength, out string filename) {
        return CreateTempFile(nameLength, ".tmp", out filename);
    }
    public static Stream CreateTempFile(string extension, out string filename) {
        return CreateTempFile(40, extension, out filename);
    }
    public static Stream CreateTempFile(int nameLength, string extension, out string filename) {
        if (nameLength == 0) { filename = null; return null; }
        Random rnd = new Random();

        //define the directory where we create the random file
        string dir = Path.GetTempPath();

        //define the character set to use in the random string
        //(characters would just be randomly selected)
        string charset = "abcdefghijklmnopqrstuvwxyz";
        charset += charset.ToUpper();
        charset += "0123456789";

        //keep generating a random string until that the name
        //is not taken by the system (which is VERY rare)
        while (true) {
            string build = "";
            for (int c = 0; c < nameLength; c++) {
                build += charset[rnd.Next(0, charset.Length)];
            }

            //already taken?
            filename = dir + "\\" + build + extension;
            if (!File.Exists(filename)) {
                return File.Create(filename);
            }
        }
    }

    public static void AddObject<T>(ref T[] array, params T[] values) {
        int originLength = array.Length;
        int vLen = values.Length;
        Array.Resize(ref array, array.Length + vLen);
        for (int c = 0; c < vLen; c++) {
            array[originLength + c] = values[c];
        }
    }

    public static bool ValidFilename(string name) { 
        //there must be a name
        if (name.Replace(" ", "").Length == 0) { return false; }

        //define the list of valid characters supported in the name
        string valid = "abcdefghijklmnopqrstuvwxyz";
        valid += valid.ToUpper();
        valid += "._- ";
        valid += "0123456789";

        //check for invalid characters
        for (int c = 0; c < name.Length; c++) {
            char ch = name[c];
            if (valid.IndexOf(name[c]) == -1) {
                return false;
            }
        }

        //it's valid.
        return true;
    }

    public static string[] StringArrayFromBytes(byte[] data, bool use255) { 
        //convert the data to a stream so we can
        //read from it easier.
        MemoryStream str = new MemoryStream();
        str.Write(data, 0, data.Length);
        str.Position = 0;

        //read the number of strings there are in the array
        int length = (use255 ? str.ReadByte() : DecodeInt16(str));
        string[] buffer = new string[length];

        //read all the strings from the stream
        for (int c = 0; c < length; c++) {
            buffer[c] = (use255 ?
                ReadString255(str) :
                ReadString(str));
        }

        //clean up
        str.Close();
        return buffer;
    }
    public static byte[] StringArrayToBytes(string[] strings, bool use255) { 
        //define the memory stream to write to
        MemoryStream buffer = new MemoryStream();

        //write the length of the array
        if (use255) {
            buffer.WriteByte((byte)strings.Length);
        }
        else {
            EncodeInt16((short)strings.Length, buffer);
        }

        //write each string
        for (int c = 0; c < strings.Length; c++) {
            if (use255) {
                WriteString255(strings[c], buffer);
            }
            else {
                WriteString(strings[c], buffer);
            }
        }
        
        //return the byte array equivilent of the buffer
        byte[] ret = new byte[buffer.Length];
        buffer.Position = 0;
        buffer.Read(ret, 0, ret.Length);
        buffer.Close();
        return ret;
    }

    public static bool PointCollide(Rectangle bound, Point compare) {
        return
            compare.X > bound.X &&
            compare.Y > bound.Y &&
            compare.X < bound.X + bound.Width &&
            compare.Y < bound.Y + bound.Height;           
    }

    public static unsafe void ChangeBitmapLight(ref Bitmap bitmap, int light) {
        light = (int)(light * 1.0f / 100 * 255);

        //convert the bitmap to a compatable bitmap which we can
        //use.
        Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height);
        Graphics.FromImage(newBitmap).DrawImage(bitmap, Point.Empty);
        bitmap = newBitmap;

        //lock the bitmap in memory
        BitmapData bitmapLock = bitmap.LockBits(
            new Rectangle(Point.Empty, newBitmap.Size),
            ImageLockMode.ReadWrite,
            PixelFormat.Format32bppArgb);
        byte* ptr = (byte*)bitmapLock.Scan0.ToPointer();

        //iterate over every pixel
        int pixelCount = bitmap.Width * bitmap.Height;
        for (int c = 0; c < pixelCount; c++) { 
            //read argb
            byte r = *ptr++;
            byte g = *ptr++;
            byte b = *ptr++; 
            ptr++;

            //apply light
            r = (byte)(r * 1.0f / 255 * light);
            g = (byte)(g * 1.0f / 255 * light);
            b = (byte)(b * 1.0f / 255 * light);

            //apply changes
            ptr -= 4;
            *ptr++ = r;
            *ptr++ = g;
            *ptr++ = b;
            ptr++;
        }

        //clean up
        bitmap.UnlockBits(bitmapLock);
    }

    public static Process SpawnProcess(string filename, string arguments) {
        return SpawnProcess(
            filename,
            new FileInfo(filename).Directory.FullName,
            arguments);
    }
    public static Process SpawnProcess(string filename, string currentDirectory, string arguments) {
        Process ps = new Process();
        ps.StartInfo = new ProcessStartInfo { 
            FileName = filename,
            Arguments = arguments,
            WorkingDirectory = currentDirectory
        };
        ps.Start();
        return ps;
    }

    public static Process SpawnProcess(string filename, string arguments, bool wait,
                                    StandardOutputCallback standardOut) { 
        return SpawnProcess(
            filename,
            arguments,
            wait,
            standardOut,
            standardOut);
    }
    public static Process SpawnProcess(string filename, string arguments, bool wait,
                                    StandardOutputCallback standardOut,
                                    StandardOutputCallback errorOut) { 
        return SpawnProcess(
            filename,
            new FileInfo(filename).Directory.FullName,
            arguments,
            wait,
            standardOut,
            errorOut);
    }
    public static Process SpawnProcess(string filename, string currentDirectory, string arguments, bool wait,
                                    StandardOutputCallback standardOut) { 
        return SpawnProcess(
            filename,
            currentDirectory,
            arguments,
            wait,
            standardOut,
            standardOut);
    }
    public static Process SpawnProcess(string filename, string currentDirectory, string arguments, bool wait,
                                    StandardOutputCallback standardOut,
                                    StandardOutputCallback errorOut) { 

        //standardOut(filename + " " + arguments);

        //create the process with redirecting output enabled
        Process process = new Process() { 
            StartInfo = new ProcessStartInfo() {
                FileName = filename,
                WorkingDirectory = currentDirectory,
                Arguments = arguments,

                RedirectStandardError = true,
                RedirectStandardOutput = true,

                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) {
            if (e.Data == null) { return; }
            standardOut(e.Data);
        };
        process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
            if (e.Data == null) { return; }
            errorOut(e.Data);
        };

        //start the process
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        //wait for the process to finish
        if (wait) { process.WaitForExit(); }
        return process;
    }


    public static string Backup(string filename) {
        string backup = GetBackupFilename(filename);
        File.Copy(filename, backup, true);
        return backup;
    }
    public static void Restore(string filename) {
        string backup = GetBackupFilename(filename);
        if (!File.Exists(backup)) { return; }
        File.Copy(backup, filename, true);
        File.Delete(backup);
    }
    public static string GetBackupFilename(string filename) {
        FileInfo f = new FileInfo(filename);
        
        //just add the "_backup" suffix to the file name
        string name = f.Name.Replace(f.Extension, "");
        name += "_backup";
        return f.Directory.FullName + "\\" + name + f.Extension;
    }

    public static string GetAbsolutePath(string absolute, string relative) { 
        //split up the absolute and relative paths
        absolute = absolute.Replace("/", "\\");
        relative = relative.Replace("/", "\\");
        string[] absoluteSplit = absolute.Split('\\');
        string[] relativeSplit = relative.Split('\\');
        Helpers.RemoveBlankStrings(ref absoluteSplit);
        Helpers.RemoveBlankStrings(ref relativeSplit);

        //is the relative path an absolute path?
        if (relative.Length > 2 && relative[1] == ':') { return relative; }

        //enumerate over the relative entries
        for (int c = 0; c < relativeSplit.Length; c++) {
            string sub = relativeSplit[c];
            
            //count how many times this subpath wants to go back
            //up a directory.
            int times = -1; //-1 because the first "." is from the relative path
            bool hasPeriod = false;
            if (sub[0] == '.') {
                for (int x = 0; x < sub.Length; x++) {
                    if (sub[x] != '.') {
                        //if there is characters after the the "." characters, it's invalid!
                        return null;
                    }
                    times++;
                    hasPeriod = true;
                }
            }
            if (times < 0) { times = 0; }

            //go backwards in the absolute path by the specified number
            //of times.
            Array.Resize(ref absoluteSplit, absoluteSplit.Length - times);

            //add the relative sub-path to the end of the absolute position
            //but only if there was no request to go up a directory.
            if (times > 0 || hasPeriod) { continue; }
            Array.Resize(ref absoluteSplit, absoluteSplit.Length + 1);
            absoluteSplit[absoluteSplit.Length - 1] = sub;
        }

        return Helpers.Flatten(absoluteSplit, "\\");
    }

    public static void RemoveBlankStrings(ref string[] array) {
        string[] buffer = new string[0];
        for (int c = 0; c < array.Length; c++) {
            if (array[c].Replace(" ", "").Length == 0) { continue; }
            Array.Resize(ref buffer, buffer.Length + 1);
            buffer[buffer.Length - 1] = array[c];
        }
        array = buffer;
    }
    public static string Flatten(string[] array, string seperatator) {
        string buffer = "";
        for (int c = 0; c < array.Length; c++) {
            buffer += array[c] + (c == array.Length - 1 ? "" : seperatator);
        }
        return buffer;
    }

    public static bool RemoveObject<T>(ref T[] array, T value) { 
        //define the new array which is one entry less
        //than the input array
        T[] buffer = new T[array.Length - 1];

        //fill in the buffer but ignore the value to remove
        int ai = 0;
        int arrayLength = array.Length;
        bool found = false;
        for (int c = 0; c < arrayLength; c++) {
            if (array[c].Equals(value)) { found = true; continue; }
            if (ai >= buffer.Length) { break; }
            buffer[ai] = array[c];
            ai++;
        }

        //clean up
        if (!found) { return false; }
        array = buffer;
        return true;
    }

    public static void WriteString255(string str, Stream stream) {
        byte[] buffer = new byte[str.Length + 1];
        buffer[0] = (byte)str.Length;
        for (int c = 0; c < str.Length; c++) {
            buffer[c + 1] = (byte)str[c];
        }
        stream.Write(buffer, 0, buffer.Length);
    }
    public static string ReadString255(Stream str) { 
        //read the length of the string
        byte length = (byte)str.ReadByte();

        //read the string into a byte buffer
        byte[] buffer = new byte[length];
        str.Read(buffer, 0, length);

        //convert the bytes to a string
        string returnBuffer = "";
        for (int c = 0; c < length; c++) {
            returnBuffer += (char)buffer[c];
        }
        return returnBuffer;
    }

    public static void WriteString(string str, Stream stream) {
        EncodeInt16((short)str.Length, stream);
        stream.Write(
            Encoding.ASCII.GetBytes(str),
            0,
            str.Length);
    }
    public static string ReadString(Stream stream) {
        short length = DecodeInt16(stream);
        byte[] bytes = new byte[length];
        stream.Read(bytes, 0, length);
        return Encoding.ASCII.GetString(bytes);
    }

    public static void EncodeInt16(short value, Stream stream) {
        byte[] buffer = EncodeInt16(value);
        stream.Write(buffer, 0, 2);
    }
    public static short DecodeInt16(Stream stream) {
        byte[] buffer = new byte[2];
        stream.Read(buffer, 0, 2);
        return DecodeInt16(buffer);
    }
    public static byte[] EncodeInt16(short value) {
        byte[] buffer = new byte[2];

        buffer[0] = (byte)(value >> 8);
        value -= (short)((short)buffer[0] << 8);
        buffer[1] = (byte)value;

        return buffer;
    }
    public static short DecodeInt16(byte[] data) {
        return
            (short)((data[0] << 8) +
                    (data[1]));
    }

    public static void EncodeInt32(int value, Stream str) {
        byte[] buffer = EncodeInt32(value);
        str.Write(buffer, 0, 4);
    }
    public static int DecodeInt32(Stream str) {
        byte[] read = new byte[4];
        str.Read(read, 0, 4);
        return DecodeInt32(read);
    }
    public static byte[] EncodeInt32(int value) {
        byte[] buffer = new byte[4];

        buffer[0] = (byte)(value >> 24);
        value -= (int)((int)buffer[0] << 24);
        buffer[1] = (byte)(value >> 16);
        value -= (int)((int)buffer[1] << 16);
        buffer[2] = (byte)(value >> 8);
        value -= (int)((int)buffer[2] << 8);
        buffer[3] = (byte)(value);

        return buffer;
    }
    public static int DecodeInt32(byte[] data) {
        return (int)(((int)data[0] << 24) +
                     ((int)data[1] << 16) +
                     ((int)data[2] << 8) +
                     ((int)data[3]));
    }

    public static void EncodeInt64(long value, Stream str) {
        byte[] buffer = EncodeInt64(value);
        str.Write(buffer, 0, 8);
    }
    public static long DecodeInt64(Stream str) {
        byte[] buffer = new byte[8];
        str.Read(buffer, 0, 8);
        return DecodeInt64(buffer);
    }
    public static byte[] EncodeInt64(long value) {
        byte[] buffer = new byte[8];

        buffer[0] = (byte)(value >> 56);
        value -= (long)((long)buffer[0] << 56);
        buffer[1] = (byte)(value >> 48);
        value -= (long)((long)buffer[1] << 48);
        buffer[2] = (byte)(value >> 40);
        value -= (long)((long)buffer[2] << 40);
        buffer[3] = (byte)(value >> 32);
        value -= (long)((long)buffer[3] << 32);

        buffer[4] = (byte)(value >> 24);
        value -= (long)((long)buffer[4] << 24);
        buffer[5] = (byte)(value >> 16);
        value -= (long)((long)buffer[1] << 16);
        buffer[6] = (byte)(value >> 8);
        value -= (long)((long)buffer[2] << 8);
        buffer[7] = (byte)(value);

        return buffer;
    }
    public static long DecodeInt64(byte[] data) {
        return (long)(((long)data[0] << 56) +
                     ((long)data[1] << 48) +
                     ((long)data[2] << 40) +
                     ((long)data[3] << 32) +

                     ((long)data[4] << 24) +
                     ((long)data[5] << 16) +
                     ((long)data[6] << 8) +
                     ((long)data[7]));
    }
}