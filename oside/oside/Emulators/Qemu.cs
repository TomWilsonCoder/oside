using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

public sealed class QemuEmulator : IEmulator {
    private string p_DiskImage;
    private Process p_Process;
    private Process p_QemuProcess;
    private Socket p_Socket;
    private NetworkStream p_Stream;
    private bool p_Suspended;
    private object p_SyncLock = new object();

    public QemuEmulator(string diskImage) {
        p_DiskImage = diskImage;
    }

    delegate bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam);

    #region State
    public void Start(int memory, StandardOutputCallback standardOutput) {
        //already running?
        if (Running) { return; }

        #region Invoke Qemu
        string qemuArguments = "-qmp tcp:127.0.0.1:4444,server,nowait " +
                               "-cdrom \"" + new FileInfo(p_DiskImage).FullName + "\" " +
                               "-m " + memory;

        //create a processlink process so that if this process is killed
        //it automatically kills the emulator as well.
        standardOutput("Launching process linker...");
        if (!File.Exists("processlink.exe")) {
            standardOutput("  Error! ProcessLink.exe is not found!");
            return;
        }
        p_Process = new Process() {
            StartInfo = new ProcessStartInfo { 
                FileName = "processLink.exe",
                Arguments = "-id " + Process.GetCurrentProcess().Id + " " +
                            "-f \"" + new FileInfo("emulators/qemu/qemu.exe").FullName + "\" " + 
                            "-a " + qemuArguments,
                UseShellExecute = false,
                CreateNoWindow = true,     
                RedirectStandardOutput = true
            }
        };

        //get the qemu process id from the spawn process
        int qemuPID = -1;
        p_Process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) {
            if (e.Data == null || qemuPID != -1) { return; }            
            qemuPID = Convert.ToInt32(e.Data);
        };

        //launch the process
        standardOutput("Starting Qemu...");
        p_Process.Start();
        standardOutput("   Process linker PID=" + p_Process.Id);
        p_Process.BeginOutputReadLine();

        //wait until the spawner has launched Qemu
        while (qemuPID == -1) ;
        p_QemuProcess = Process.GetProcessById(qemuPID);
        standardOutput("   Started, PID=" + qemuPID);

        #endregion

        //connect to the Qemu Monitor
        p_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        int connectStart = Environment.TickCount;
        standardOutput("Connecting to Qemu's QMP service...");
        while (Environment.TickCount < connectStart + 1000) {
            try { 
                p_Socket.Connect("localhost", 4444);
                p_Stream = new NetworkStream(p_Socket, true);
                break;
            }
            catch { }
        }

        //timed out?
        if (!p_Socket.Connected) {
            standardOutput("Error! Unable to connect to Qemu's QMP service");
            Stop();
            return;
        }
        
        //get the initial response from the server
        standardOutput("Performing handshake with QMP Server");
        JSONObject handshake = JSONObject.Decode("", monitorRead())[0];
        handshake = (JSONObject)handshake["QMP"];

        //get the version of the qemu emulator        
        JSONObject version = (JSONObject)handshake["version"];
        version = (JSONObject)version["qemu"];
        int versionMajor = Convert.ToInt32(version["major"]);
        int versionMinor = Convert.ToInt32(version["minor"]);

        //enter command mode
        monitorWrite("{ \"execute\": \"qmp_capabilities\" }");
        JSONObject result = JSONObject.Decode(monitorRead())[0];
        result = (JSONObject)result["return"];
        if (result.ChildValues[0].ChildCount > 0) {
            standardOutput("Unable to enter into QMP Command mode!");
            Stop();
            return;
        }

        //create an event listener
        standardOutput("Starting event listener...");
        Thread eventListener = new Thread(new ThreadStart(delegate {
            while (Running) {
                //send a blank signal to the server so we don't interfere
                //with current messages.
                Monitor.Enter(p_SyncLock);
                monitorExecute("", null);
                monitorReadObject(false);
                Monitor.Exit(p_SyncLock);

                Thread.Sleep(100);
            }
        }));
        eventListener.Start();
        standardOutput("  Listening...");

        //success
        standardOutput("Qemu Emulator version " + versionMajor + "." + versionMinor);
        standardOutput("Qemu Emulator ready...");

        return;
        EmulationProcessor ps = GetProcessors()[0];
        while (true) {
            int time = Environment.TickCount;
            GetRegisters(ps);
            Console.WriteLine((Environment.TickCount - time) + "ms");
        }

        new DebugRegisters(this).ShowDialog();
    }
    public bool Running {
        get {
            if (p_QemuProcess == null) { return false; }
            try {
                if (Process.GetProcessById(p_QemuProcess.Id) == null) {
                    return false;
                }
                return true;
            }
            catch { return false; }
        }
    }
    public void Stop() {
        if (p_Process == null) { return; }
        try { p_Socket.Close(); }
        catch { }
        try { p_QemuProcess.Kill(); }
        catch { }
        p_Socket = null;
        p_Stream = null;
        p_QemuProcess = null;
        p_Process = null;

        //trigger the event
        if (Shutdown != null) {
            Shutdown(this, null);
        }
    }
    public void Restart() {
        Monitor.Enter(p_SyncLock);
        monitorExecute("system_reset", null);
        monitorReadObject(false);
        Monitor.Exit(p_SyncLock);
    }

    public bool Suspended { get { return p_Suspended; } }
    public void Suspend() {
        Monitor.Enter(p_SyncLock);
        monitorExecuteCmd("stop");
        monitorReadObject(false);
        p_Suspended = true;
        Monitor.Exit(p_SyncLock);
    }
    public void Resume() {
        Monitor.Enter(p_SyncLock);
        monitorExecuteCmd("cont");
        monitorReadObject(false);
        p_Suspended = false;
        Monitor.Exit(p_SyncLock);
    }
    #endregion

    public EmulationProcessor[] GetProcessors() {
        Monitor.Enter(p_SyncLock);

        //get the JSON array which contains all the processors
        //running in the emulation.
        monitorExecute("query-cpus", null);
        JSONObject processors = monitorReadObject(true)[0];
        processors = (JSONObject)processors["return"];
        JSONObject[] children = processors.ChildValues[0].ChildValues;
        
        //read the JSON object into an EmulationCPU object array
        EmulationProcessor[] buffer = new EmulationProcessor[children.Length];
        for (int c = 0; c < buffer.Length; c++) {
            JSONObject obj = children[c];
            buffer[c] = new EmulationProcessor(
                Convert.ToInt32(obj["cpu"]),
                Convert.ToBoolean(obj["current"]),
                Convert.ToInt64(obj["pc"]),
                Convert.ToBoolean(obj["halted"]));


        }

        Monitor.Exit(p_SyncLock);
        return buffer;
    }
    public void UpdateProcessor(ref EmulationProcessor processor) {
        //get the current state of all processors
        EmulationProcessor[] processors = GetProcessors();

        //look for the processor to update in the 
        //list of processors we just got.
        for (int c = 0; c < processors.Length; c++) {
            if (processors[c].Index == processors[c].Index) {
                processor = processors[c];
                break;
            }
        }
    }

    public EmulationRegisterCollection GetRegisters(EmulationProcessor processor) {
        Monitor.Enter(p_SyncLock);

        //get all the registers assigned to the processor
        monitorExecute("human-monitor-command", new string[][] { 
            new string[] { "command-line", "info registers" },
            new string[] { "cpu-index", processor.Index.ToString() }
        });
        JSONObject registers = monitorReadObject(true)[0];

        //get the string which contains the raw register data returned 
        //from Qemu
        string raw = registers["return"].ToString();
        raw = raw.Replace("\\r", "").Replace("\\n", " ");

        //we assume that each register is seperated by a space.
        string[] split = raw.Split(' ');

        //iterate over the register strings
        EmulationRegister[] buffer = new EmulationRegister[0];
        for (int c = 0; c < split.Length; c++) {
            string entry = split[c];

            //don't know what the [...] bit means yet.
            if (entry.StartsWith("[")) { continue; }

            //get the register name and value string
            string[] eSplit = entry.Split('=');
            if (eSplit.Length != 2) { continue; }
            string name = eSplit[0];
            string value = eSplit[1];
            if (name.Replace(" ", "").Length == 0) { continue; }
            if (value.Length == 0) { value = "0"; }

            //add the register to the return buffer
            //(we convert the value string from hex to a 64bit int)
            try {
                Helpers.AddObject(ref buffer,
                    new EmulationRegister(
                        name,
                        Convert.ToInt64(value, 16)));
            }
            catch { }
        }

        Monitor.Exit(p_SyncLock);
        return new EmulationRegisterCollection(buffer);
    }

    #region Helpers

    private JSONObject[] monitorReadObject(bool expectedReturn) {
        JSONObject[] obj = JSONObject.Decode(monitorRead());

        //is it an event?
        if (obj.Length != 0 && obj[0]["event"] != null) {
            handleEvent(obj[0]);

            //re-call this function to get the data 
            //the caller expects.
            if (!expectedReturn) { return null; }
            return monitorReadObject(true);
        }
        return obj;
    }

    private void monitorExecuteCmd(string command) {
        monitorExecute("human-monitor-command", new string[][] { 
            new string[] { "command-line", command }
        });
    }
    private void monitorExecute(string command, string[][] arguments) { 
        //flatten the arguments
        string argumentFlat = "";
        if (arguments != null) {
            for(int c = 0; c < arguments.Length; c++){
                string[] s = arguments[c];

                //write the name
                argumentFlat += "\"" + s[0] + "\":";

                //is the value a string or an integer?
                long tmp;
                bool isInt = long.TryParse(s[1], out tmp);

                //write the value
                argumentFlat +=
                    (isInt ? "" : "\"") +
                    s[1] +
                    (isInt ? "" : "\"");


                if (c != arguments.Length - 1) { argumentFlat += ","; }

            }
            if (arguments.Length != 0) {
                argumentFlat = ",\"arguments\":{" + argumentFlat + "}";
            }
        }

        //send command
        monitorWrite("{\"execute\":\"" + command + "\"" + argumentFlat + "}");
    }
    private void monitorWrite(string message) {
        p_Stream.Write(
            System.Text.Encoding.ASCII.GetBytes(message),
            0,
            message.Length);
    }
    private string monitorRead() {
        string strBuffer = "";

        //wait for data to become available
        while (!p_Stream.DataAvailable) ;

        while (true ) {
            int available = p_Socket.Available;

            //if there is no data available, give it some time
            //to send more data
            if (available == 0) {
                int destTime = Environment.TickCount + 20;
                while (Environment.TickCount < destTime) {
                    if ((available = p_Socket.Available) != 0) { break; }
                }
                if (available == 0) { break; }
            }

            //read into a buffer
            byte[] buffer = new byte[available];
            p_Stream.Read(buffer, 0, available);

            //write the buffer to the return string
            strBuffer += System.Text.Encoding.ASCII.GetString(buffer);
        }

        return strBuffer;
    }
    #endregion

    #region Events
    private void handleEvent(JSONObject obj) { 
        //get the event name
        string name = obj["event"].ToString();

        //get the data
        JSONObject[] data = (JSONObject[])obj["data"];

        //trigger the event associated with the event name
        switch (name.ToLower()) {
            case "reset": if (Reset == null) { break; } Reset(this, null); break;
            case "stop": if (Paused == null) { break; } Paused(this, null); break;
            case "resume": if (Resumed == null) { break; } Resumed(this, null); break;
        }
    }

    public event EmulatorEventHandler<object> Reset;
    public event EmulatorEventHandler<object> Paused;
    public event EmulatorEventHandler<object> Resumed;
    public event EmulatorEventHandler<object> Shutdown;
    #endregion

}