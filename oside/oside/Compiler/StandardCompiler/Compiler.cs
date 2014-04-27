using System;
using System.IO;

public sealed class StandardCompiler : ICompiler {

    public ICompilerOutput<Solution> Compile(Solution solution, StandardOutputCallback standardOutput) {
        /*This is only temporary code until we have an actual build config system*/
        string finalOutput = solution.BuildDirectory + "\\disk.iso";
        if (File.Exists(finalOutput)) { File.Delete(finalOutput); }
        string bootsect = "";
        StandardCompileOutput<Solution> output = new StandardCompileOutput<Solution>(finalOutput, solution);

        foreach (Project p in solution.Projects) {
            ICompilerOutput<Project> projOut = CompileProject(p, standardOutput);
            foreach (ICompileOutputEntry e in projOut.Errors) {
                output.addError(
                    e.File,
                    e.Line,
                    e.Column,
                    e.Message);
            }
            foreach (ICompileOutputEntry w in projOut.Warnings) {
                output.addWarning(
                    w.File,
                    w.Line,
                    w.Column,
                    w.Message);
            }

            if (projOut.Errors.Length != 0) { continue; }
            bootsect = projOut.OutputFile;
        }

        if (File.Exists(bootsect)) {
            generateISO(
                "OS Test",
                bootsect,
                solution.BuildDirectory + "\\disk",
                finalOutput,
                standardOutput,
                new string[0]);
        }

        return output;
    }
    public ICompilerOutput<Project> CompileProject(Project project, StandardOutputCallback standardOutput) {

        #region split up every file in the project into a code file 

        fileObj[] assemblyFiles = new fileObj[0];
        fileObj[] cCodeFiles = new fileObj[0];
        fileObj[] cHeaderFiles = new fileObj[0];

        project.Root.Enumerate(true, delegate(ProjectEntity e) {
            if (!(e is ProjectFile)) { return true; }

            //add the filename into the appropriate code file list
            ProjectFile file = (ProjectFile)e;
            string ext = file.Extension.ToLower();
            switch (ext) { 
                case ".c":
                    Helpers.AddObject(ref cCodeFiles, new fileObj(file));
                    break;
                case ".h":
                    Helpers.AddObject(ref cHeaderFiles, new fileObj(file));
                    break;
                case ".asm":
                    Helpers.AddObject(ref assemblyFiles, new fileObj(file));
                    break;
            }
            return true;
        });
        #endregion

        //define the final output file
        string finalOutput = getOuputFilename(project, "build.bin");
        StandardCompileOutput<Project> output = new StandardCompileOutput<Project>(finalOutput, project);
        string[] outputFiles = new string[0];
        if (File.Exists(finalOutput)) { File.Delete(finalOutput); }

        //define whether or not the assembly code should
        //be linked (no point in linking if theres nothing 
        //to link with)
        bool linkable = cCodeFiles.Length != 0 || cHeaderFiles.Length != 0;

        //compile the assembly
        if (assemblyFiles.Length != 0) {
            //make sure that the main entry point assembly file is at the top
            //of the list to be compiled first.
            int bootfileIndex = -1;
            for (int c = 0; c < assemblyFiles.Length; c++) {
                if (assemblyFiles[c].ProjectFile.Name.ToLower() == "boot.asm") {
                    bootfileIndex = c;
                    break;
                }
            }
            if (bootfileIndex != -1) {
                Helpers.SwapEntries(ref assemblyFiles, bootfileIndex, 0);
            }

            string asmOut = getOuputFilename(project, "asm.o");
            compileAssembly(linkable, asmOut, output, assemblyFiles, standardOutput);
            Helpers.AddObject(ref outputFiles, asmOut);

            //if there are no C code/header files, then there is no point
            //compiling and linking them with the ASM code.
            if (!linkable) {
                if (File.Exists(asmOut)) {
                    File.Move(asmOut, finalOutput);
                }
                return output;
            }
        }

        //compile all the C code
        string cOut = getOuputFilename(project, "c.o");
        compileC(cOut, output, cCodeFiles, cHeaderFiles, standardOutput);
        Helpers.AddObject(ref outputFiles, cOut);

        //if there was no success, do not continue with linking.
        if (output.Errors.Length != 0) { return output; }

        //link the c and assembly code together.
        link(
            outputFiles,
            finalOutput,
            output,
            standardOutput);
        return output;
    }

    #region Linker
    private void link(string[] files, string outputFile, StandardCompileOutput<Project> output, StandardOutputCallback stdOut) { 
        //perform the link
        outputEntry[] errors = new outputEntry[0];
        outputEntry[] warnings = new outputEntry[0];
        link(ref errors, ref warnings, files, outputFile, stdOut);
        
        //add the errors and warnings to the output object
        for (int c = 0; c < errors.Length; c++) {
            output.addError(null, 0, 0, errors[c].message);
        }
        for (int c = 0; c < warnings.Length; c++) {
            output.addWarning(null, 0, 0, warnings[c].message);
        }
    }
    private void link(ref outputEntry[] errors, ref outputEntry[] warnings, string[] files, string outputFile, StandardOutputCallback stdOut) { 
        //define the filename for the linker
        string linker = new FileInfo("compilers/linker/ld.exe").FullName;
        //linker = "C:\\cygwin\\bin\\ld.exe";
        
        //define the arguments to pass to the linker
        string args = "-m elf_i386 -o \"" + outputFile + "\" "; //-m elf_i386
        for (int c = 0; c < files.Length; c++) {
            args += "\"" + files[c] + "\" ";
        }

        //invoke the linker
        outputEntry[] e = errors;
        outputEntry[] w = warnings;
        Helpers.SpawnProcess(
            linker,
            null,
            args,
            true,
            delegate(string line) {
                stdOut(line);
                gnuStdOutHandler(ref e, ref w, "/linker/ld", line);
            });

        //clean up
        errors = e;
        warnings = w;
        
    }
    #endregion

    #region Assembly compiler
    private void compileAssembly(bool linkable, string outputFile, StandardCompileOutput<Project> output, fileObj[] files, StandardOutputCallback stdOut) {
        if (File.Exists(outputFile)) { File.Delete(outputFile); }

        //collapse all the files into one file to compile
        string collapsed = collapseFiles(files, ".asm");

        //compile
        outputEntry[] errors = new outputEntry[0];
        outputEntry[] warnings = new outputEntry[0];
        compileAssembly(linkable, ref errors, ref warnings, collapsed, outputFile, stdOut);
        processCompileResults(files, output, errors, warnings);

        //clean up
        File.Delete(collapsed);
    }
    private void compileAssembly(bool linkable, ref outputEntry[] errors, ref outputEntry[] warnings, string filename, string outputFile, StandardOutputCallback stdOut) { 
        //define the filename for the NASM compiler
        string nasmCompiler = new FileInfo("compilers/nasm/nasm.exe").FullName;

        //call nasm to compile the ASM code.
        outputEntry[] e = errors;
        outputEntry[] w = warnings;
        Helpers.SpawnProcess(
            nasmCompiler,
            new FileInfo(filename).Directory.FullName,
            (linkable ? "-f elf32 " : "") + 
            "\"" + filename + "\" " +
            "-o \"" + outputFile + "\"",
            true,
            delegate(string line) {
                stdOut(line);
                gnuStdOutHandler(
                    ref e,
                    ref w,
                    filename,
                    line);
            });

        //clean up
        errors = e;
        warnings = w;
    }
    #endregion

    #region C compiler
    private void compileC(string outputFile, StandardCompileOutput<Project> output, fileObj[] codeFiles, fileObj[] headerFiles, StandardOutputCallback stdOut) {
        if (File.Exists(outputFile)) { File.Delete(outputFile); }
        
        //collapse the code files and header files into one array but add the header files first.
        fileObj[] files = new fileObj[0];
        Helpers.AddObject(ref files, headerFiles);
        Helpers.AddObject(ref files, codeFiles);

        //flatten the files down to a single file
        string collapsed = collapseFiles(files, ".c");

        //perform the compile
        outputEntry[] errors = new outputEntry[0];
        outputEntry[] warnings = new outputEntry[0];
        compileC(ref errors, ref warnings, collapsed, outputFile, stdOut);
        processCompileResults(codeFiles, output, errors, warnings);

        //clean up
        File.Delete(collapsed);
    }
    private void compileC(ref outputEntry[] errors, ref outputEntry[] warnings, string filename, string outputFile, StandardOutputCallback stdOut) { 
        //get the filename for the GCC compiler
        string gcc = new FileInfo("compilers/gcc/bin/gcc.exe").FullName;

        //invoke the GCC compiler
        outputEntry[] e = errors;
        outputEntry[] w = warnings;
        Helpers.SpawnProcess(
            gcc,
            null,
            "-c \"" + filename + "\" -o \"" + outputFile + "\" " +
            "-Wall -Wextra -nostdlib -nostartfiles -nodefaultlibs -m32",
            true,
            delegate(string line) {
                stdOut(line);
                gnuStdOutHandler(ref e, ref w, filename, line);
            });

        //clean up
        errors = e;
        warnings = w;
    }
    #endregion

    private void gnuStdOutHandler(ref outputEntry[] errors, ref outputEntry[] warnings, string filename, string line) {
        //strip out the filename
        if (!line.StartsWith(filename)) { return; }
        line = line.Replace(filename, "");

        //get the line number and column number
        string[] split = line.Split(' ');
        string header = split[0];
        string[] headerSplit = header.Split(':');
        int lineNum = 0;
        int columnNum = 0;
        int.TryParse(headerSplit[1], out lineNum);
        if (header.Length >= 3) { int.TryParse(headerSplit[2], out columnNum); }

        //get the type of message
        string messageType = split[1].Replace(":", "").ToLower();

        //read the message
        string message = "";
        for (int c = 2; c < split.Length; c++) {
            message += split[c] + (c == split.Length - 1 ? "" : " ");
        }

        //add the error/warning entry
        if (messageType == "error" || messageType == "warning" || messageType == "fatal") {
            outputEntry e = new outputEntry { 
                line = lineNum,
                column = columnNum,
                message = message
            };

            if (messageType == "error" ||
                messageType == "fatal") {
                Helpers.AddObject(ref errors, e);
            }
            if (messageType == "warning") {
                Helpers.AddObject(ref warnings, e);
            }
        }
    }

    private void generateISO(string volumeTitle, string bootsector, string diskContents, string output, StandardOutputCallback stdOut, string[] hideFiles) {
        if (!Directory.Exists(diskContents)) {
            Directory.CreateDirectory(diskContents);
        }
             
        //define the path for the mkisofs (make iso filesystem) exe
        string mkisofs = new FileInfo("compilers/mkisofs/mkisofs.exe").FullName;

        //define the arguments to pass to the exe
        string[] args = new string[] {
            "-iso-level 3",
            "-v -allow-lowercase",
            "-volid \"" + volumeTitle + "\"",
            "-o \"" + output + "\"",
        };
        if (bootsector != null) {
            if (!File.Exists(bootsector)) { return; }
            File.Copy(bootsector, diskContents + "\\bootsect.bin", true);
            int sectorCount = (int)Math.Ceiling(new FileInfo(bootsector).Length * 1.0f / 512);
            Helpers.AddObject(ref args, "-b bootsect.bin");
            Helpers.AddObject(ref args, "-no-emul-boot -boot-load-size " + sectorCount);
            Helpers.AddObject(ref hideFiles, "bootsect.bin");
        }

        //write the arguments for the hidden files
        string hideArgsBuffer = "-hide ";
        for (int c = 0; c < hideFiles.Length; c++) {
            hideArgsBuffer += "" + hideFiles[c] + " ";
        }
        Helpers.AddObject(ref args, hideArgsBuffer);

        //add disk contents folder
        Helpers.AddObject(ref args, "\"" + diskContents + "\"");

        //invoke the exe
        Helpers.SpawnProcess(
            mkisofs,
            new FileInfo(mkisofs).Directory.FullName,
            Helpers.Flatten(args, " "),
            true,
            stdOut);

        //clean up
        if (bootsector != null) {
            File.Delete(diskContents + "\\bootsect.bin");
        }
    }
    private string getOuputFilename(Project project, string name) {
        return
            project.BuildDirectory + "\\" + name;
    }
    private string collapseFiles(fileObj[] files, string extension) { 
        //create the file to collapse all the files into
        string filename;
        Stream stream = Helpers.CreateTempFile(extension, out filename);

        //read and then write every file in the file array 
        //to the temporary file
        for (int c = 0; c < files.Length; c++) { 
            //read the file data
            FileStream readStream = new FileStream(files[c].Filename, FileMode.Open);
            byte[] fileData = new byte[readStream.Length];
            readStream.Read(fileData, 0, fileData.Length);

            //is there a newline character at the end of data?
            //if not, we need to write it. Otherwise we get
            //a mismatch when getting a file from an absolute line position
            //later on when we get the origination of errors/warnings.
            bool hasNewLineCharEnding = false;
            if (fileData.Length >= 2) {
                int len = fileData.Length;
                if (fileData[len - 2] == '\n' || fileData[len - 1] == '\n') {
                    hasNewLineCharEnding = true;
                }
            }

            //write the data
            stream.Write(fileData, 0, fileData.Length);
            if (!hasNewLineCharEnding) {
                stream.Write(new byte[2] { (byte)'\r', (byte)'\n' }, 0, 2);
            }
        }

        //clean up
        stream.Close();
        return filename;
    }
    private void processCompileResults(fileObj[] files, StandardCompileOutput<Project> output, outputEntry[] errors, outputEntry[] warnings) {
        for (int c = 0; c < errors.Length; c++) {
            ProjectFile file;
            int line;
            resolveFile(errors[c], files, out file, out line);
            output.addError(
                file,
                line,
                errors[c].column,
                errors[c].message);
        }
        for (int c = 0; c < warnings.Length; c++) {
            ProjectFile file;
            int line;
            resolveFile(warnings[c], files, out file, out line);
            output.addWarning(
                file,
                line,
                warnings[c].column,
                warnings[c].message);
        }
    }
    private void resolveFile(outputEntry outputEntry, fileObj[] files, out ProjectFile file, out int line) {
        long lineTotal = 0;
        file = null;
        line = -1;
        for (int c = 0; c < files.Length; c++) {
            fileObj entry = files[c];

            int relativeLine = (int)(outputEntry.line - lineTotal);
            if (relativeLine <= entry.LineLength) {
                line = relativeLine;
                file = entry.ProjectFile;
                return;
            }
            lineTotal += entry.LineLength;
        }
    }

    private struct fileObj {
        public fileObj(ProjectFile pFile) {
            Filename = pFile.PhysicalLocation;
            ProjectFile = pFile;
            while (true) {
                try {
                    LineLength = File.ReadAllLines(Filename).Length;
                    break;
                }
                catch { }
            }
        }

        public long LineLength;
        public string Filename;
        public ProjectFile ProjectFile;
    }
    private struct outputEntry {
        public long line;
        public int column;
        public string message;
    }
}