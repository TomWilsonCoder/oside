using System;
using System.IO;
using System.Drawing;

public static class Icons {
    private static icon[] p_Icons = new icon[0];
    private static object p_SyncLock = new object();

    public static void Load(string directory, bool recursive) {
        Load("", directory, recursive);
    }
    public static void Load(string prefix, string directory, bool recursive) {
        DirectoryInfo dir = new DirectoryInfo(directory);
        
        //if it's recursive, invoke this function for every
        //sub-directory
        if (recursive) {
            foreach (DirectoryInfo d in dir.GetDirectories()) {
                Load(prefix, d.FullName, true);
            }
        }
        
        //get the files in the directory specified
        int numericalRangeStart = (int)'0';
        int numericalRangeEnd = (int)'9';

        foreach (FileInfo f in dir.GetFiles()) {
            if (f.Extension == "") { continue; }

            //get the name of the icon in question 
            //by removing the size suffix from the
            //file name. If there isnt one, we assume
            //the icon is invalid.
            string name = f.Name.Replace(f.Extension, "");
            int numericOffset = 0;
            while (numericOffset < name.Length) {
                int chInt = (int)name[numericOffset];
                if (chInt >= numericalRangeStart &&
                    chInt <= numericalRangeEnd) {
                        break;
                }
                numericOffset++;
            }
            if (numericOffset == name.Length) { continue; }

            //load it
            LoadFile(f.FullName, prefix + name.Substring(0, numericOffset));
        }
    }
    public static void LoadFile(string file, string name) { 
        //load the bitmap
        Bitmap load = null;
        try { load = new Bitmap(file); }
        catch { return; }

        //bitmap widthxheight must be the same
        if (load.Width != load.Height) {
            load.Dispose();
            return;
        }
        
        //get the current icon instance associated with the name
        //so we can add a new bitmap size to it.
        icon ico = getIcoInst(name);
        if (ico != null) { 
            lock (p_SyncLock) {
                //make sure that the bitmap size is not in use.
                for (int c = 0; c < ico.bitmaps.Length; c++) {
                    if (ico.bitmaps[c].Size.Width == load.Size.Width) {
                        load.Dispose();
                        return;
                    }
                }

                //add it
                Array.Resize(ref ico.bitmaps, ico.bitmaps.Length + 1);
                ico.bitmaps[ico.bitmaps.Length - 1] = load;
            }
            return;
        }

        //add a new icon definition since there isnt one
        //which does by the specified name
        ico = new icon {
            name = name,
            bitmaps = new Bitmap[] { load }
        };
        lock (p_SyncLock) {
            Array.Resize(ref p_Icons, p_Icons.Length + 1);
            p_Icons[p_Icons.Length - 1] = ico;
        }
    }

    public static Icon GetIcon(string name, int size) { 
        //get the bitmap and convert it to an icon
        Bitmap bmp = GetBitmap(name, size);
        if (bmp == null) { return null; }
        return Icon.FromHandle(bmp.GetHicon());
    }

    public static Bitmap GetBitmap(string name, int size) { 
        //get all the bitmaps which are associated with
        //the name.
        Bitmap[] bitmaps = GetBitmaps(name);
        if (bitmaps == null) { return null; }

        //find a bitmap with the size in question
        for (int c = 0; c < bitmaps.Length; c++) {
            if (bitmaps[c].Width == size) {
                return bitmaps[c];
            }
        }

        //sort the bitmaps so we can quickly get the
        //bitmap with the size that is closer to the
        //size requested.
        sortBitmapBySize(ref bitmaps);
        Array.Reverse(bitmaps);

        for (int c = 0; c < bitmaps.Length; c++) {
            int bSize = bitmaps[c].Width;

            //the size is more than this bitmap size?
            if (size > bSize || c == bitmaps.Length - 1) { 
                //was there a bitmap in the last iteration
                //that was larger?
                //if so, select that for resizing, not this one
                Bitmap bitmapBase = bitmaps[c];
                if (c != 0 && bitmaps[c - 1].Width > size) {
                    bitmapBase = bitmaps[c - 1];
                }

                //resize
                Bitmap resizeBitmap = new Bitmap(size, size);
                Graphics resizeBuffer = Graphics.FromImage(resizeBitmap);
                resizeBuffer.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                resizeBuffer.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                resizeBuffer.DrawImage(
                    bitmapBase,
                    new Rectangle(
                        0, 0, size, size));
                resizeBuffer.Dispose();

                //submit the new size to the actual bitmap entry
                //so we don't have to do this again.
                lock (p_SyncLock) {
                    icon ico = getIcoInst(name);
                    Array.Resize(ref ico.bitmaps, ico.bitmaps.Length);
                    ico.bitmaps[ico.bitmaps.Length - 1] = resizeBitmap;
                }

                return resizeBitmap;
            }
        }

        //this should never happen
        return null;
    }
    public static Bitmap[] GetBitmaps(string name) {
        icon ico = getIcoInst(name);
        if (ico == null) { return null; }
        return ico.bitmaps;
    }


    private static icon getIcoInst(string name) {
        lock (p_SyncLock) {
            name = name.ToLower();
            for (int c = 0; c < p_Icons.Length; c++) {
                if (p_Icons[c].name.ToLower() == name) {
                    return p_Icons[c];
                }
            }
            return null;
        }
    }
    private static void sortBitmapBySize(ref Bitmap[] array) {
        if (array.Length <= 1) { return; }

        int pivotIndex = array.Length / 2;
        Bitmap pivot = array[pivotIndex];
        int pivotSize = pivot.Width;
        Bitmap[] left = new Bitmap[0];
        Bitmap[] right = new Bitmap[0];

        for (int c = 0; c < array.Length; c++) {
            if (c == pivotIndex) { continue; }
            int size = array[c].Width;
            if (size < pivotSize) {
                Array.Resize(ref left, left.Length + 1);
                left[left.Length - 1] = array[c];
            }
            else {
                Array.Resize(ref right, right.Length + 1);
                right[right.Length - 1] = array[c];
            }
        }

        sortBitmapBySize(ref left);
        sortBitmapBySize(ref right);

        array[left.Length] = pivot;
        for (int c = 0; c < left.Length; c++) {
            array[c] = left[c];
        }
        for (int c = 0; c < right.Length; c++) {
            array[left.Length + c + 1] = right[c];
        }
    }
    private class icon {
        public string name;
        public Bitmap[] bitmaps;
    }
}