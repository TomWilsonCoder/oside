using System;
using System.IO;
using System.Threading;
using System.Diagnostics;

static class Init {
    static void Main(string[] args) {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

        //process the arguments
        int hostProcessID = -1;
        string spawnProcessFilename = "";
        string spawnProcessArguments = "";
        for (int c = 0; c < args.Length; c++) {
            switch (args[c]) {
                case "-id": hostProcessID = Convert.ToInt32(args[++c]); break;
                case "-f": spawnProcessFilename = args[++c]; break;
                case "-a":
                    c++;
                    while (c < args.Length) {
                        bool isString = args[c].Contains(" ");
                        spawnProcessArguments +=
                            (isString ? "\"" : "") +
                            args[c] +
                            (isString ? "\" " : " ");
                        c++;
                    }
                    break;
            }
        }

        //check
        if (getProcess(hostProcessID) == null) {
            error("Process id " + hostProcessID + " is not running");
            return;
        }
        if (!File.Exists(spawnProcessFilename)) {
            error("Spawn filename \"" + spawnProcessFilename + "\" does not exist");
            return;
        }

        //start 
        Process spawn = null;
        try {
            spawn = new Process() {
                StartInfo = new ProcessStartInfo() {
                    FileName = spawnProcessFilename,
                    Arguments = spawnProcessArguments,

                    CreateNoWindow = true,
                    UseShellExecute = false,

                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            spawn.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                if (e.Data == null) { return; }
                Console.WriteLine(e.Data);
            };
            spawn.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                if (e.Data == null) { return; }
                Console.WriteLine(e.Data);
            };
            spawn.Start();
        }
        catch(Exception ex) {
            error("Unknown error \"" + ex.Message + "\"");
            return;
        }

        //kill the spawn process if the host process gets killed
        new Thread(new ThreadStart(delegate {
            while (true) {
                if (getProcess(hostProcessID) == null) {
                    try {
                        spawn.Kill();
                    }
                    catch { }
                    Process.GetCurrentProcess().Kill();                   
                }
                Thread.Sleep(1000);
            }
        })).Start();

        //wait for the spawn to exit
        Console.WriteLine(spawn.Id);
        spawn.BeginOutputReadLine();
        spawn.BeginErrorReadLine();
        spawn.WaitForExit();
        Process.GetCurrentProcess().Kill();
    }

    static Process getProcess(int id) {
        try {
            return Process.GetProcessById(id);
        }
        catch {
            return null;
        }
    }
    static void error(string msg) {
        Console.WriteLine("Error: " + msg);
    }
}