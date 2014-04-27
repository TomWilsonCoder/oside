using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

public class MainIDE : Form {
    private Panel p_WorkingArea;

    private string p_BaseTitle = "OS Development Studio";
    private string p_Title;

    private ToolStrip p_Menu;
    private ToolStripItem p_MenuStripRunButton;
    private ToolStripItem p_DropDownRunButton;

    private StatusStrip p_StatusStrip;
    private ToolStripStatusLabel[] p_StatusLabels;
    private OutputWindow p_OutputWindow;
    private TabControl p_Tabs;
    private SolutionBrowserControl p_SolutionExplorer;

    private SplitContainer p_MainBodyContainer;
    private SplitContainer p_WorkingBodyContainer;

    private IEmulator p_Emulator;

    public MainIDE(Splash splash) {
        BackColor = Color.FromArgb(255, 230, 230, 230);
        ForeColor = Color.Black;

        Icon = Icons.GetIcon("icons.logo", 32);
        Text = "OS Development Studio";

        //trigger initialization of the text editor core
        TextEditor.Initialize();

        //once the form has loaded, send a signal to the 
        //splash screen to close.
        Load += delegate(object sender, EventArgs e) {
            splash.Ready();
            Focus();
        };

        //load the size and location of the window the last time the application
        //was running.
        if (RuntimeState.ObjectExists("mainIDE.windowRect")) {
            RuntimeState.RECTANGLE rect = (RuntimeState.RECTANGLE)RuntimeState.GetObject(
                "mainIDE.windowRect", 
                typeof(RuntimeState.RECTANGLE));
            //StartPosition = FormStartPosition.Manual;
            Location = new Point(rect.X, rect.Y);
            Size = new Size(rect.Width, rect.Height);

            //restore the window state
            if (RuntimeState.ObjectExists("mainIDE.windowState")) {
                WindowState = (FormWindowState)(byte)RuntimeState.GetObject(
                    "mainIDE.windowState",
                    typeof(byte));
            }
        }
        else {
            //set the initial size of the window to 75% of the current screen
            Size screenSize = Screen.FromPoint(Cursor.Position).WorkingArea.Size;
            Size = new Size(
                    (int)(screenSize.Width * 0.75),
                    (int)(screenSize.Height * 0.75));
            StartPosition = FormStartPosition.CenterScreen;
        }


        #region Create the initial panels
        p_StatusStrip = new StatusStrip { BackColor = BackColor };
        p_WorkingArea = new Panel { Dock = DockStyle.Fill };
        ToolStrip menu = new ToolStrip {
            GripStyle = ToolStripGripStyle.Hidden,
            Renderer = new toolStripRenderer(),
            BackColor = BackColor,
            ForeColor = ForeColor
        };
        p_Menu = menu;
        Controls.Add(menu);
        Controls.Add(p_StatusStrip);
        Controls.Add(p_WorkingArea);
        p_WorkingArea.BringToFront();
        #endregion 

        #region Menu
        /*Build the main menu items*/
        menu.Items.Add(new ToolStripMenuItem("File", null, getFileMenuItems()));
        menu.Items.Add(new ToolStripMenuItem("Edit", null, getEditMenuItems()));
        menu.Items.Add(new ToolStripMenuItem("Build", null, getBuildMenuItems()));
        menu.Items.Add(new ToolStripMenuItem("Tools", null, getToolsMenuItems()));
        menu.Items.Add(new ToolStripMenuItem("Help", null, getHelpMenuItems()));

        /*Build shortcuts*/
        menu.Items.Add(new ToolStripSeparator());
        p_MenuStripRunButton = menu.Items.Add(null, Icons.GetBitmap("tools.run", 16), menu_build_run);
        menu.Items.Add(null, Icons.GetBitmap("tools.stop", 16), menu_build_stop);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(null, Icons.GetBitmap("tools.build", 16), menu_build_build);
        #endregion
        
        //initialize the components
        initializeComponentSkeleton();
        initializeSolutionBrowser();
        initializeFileEditor();
        initializeOutputWindow();
        initializeStatusStrip();

        //create a UI update timer
        Timer updTimer = new Timer() { 
            Interval = 30,
            Enabled = true
        };
        updTimer.Tick += uiUpdate;


        //clean up
        p_WorkingArea.BringToFront();
        p_SaveStateEnabled = true;
    }

    private void initializeComponentSkeleton() { 
        //create the main body container which will hold
        //the left and right sides of the IDE
        p_MainBodyContainer = new SplitContainer() {
            FixedPanel = FixedPanel.Panel2,
            Dock = DockStyle.Fill,
            SplitterWidth = 3
        };
        p_WorkingArea.Controls.Add(p_MainBodyContainer);

        //create the working body container which will hold
        //the top and bottom sides of the IDE (text editor, error list etc..)
        p_WorkingBodyContainer = new SplitContainer() { 
            FixedPanel = FixedPanel.Panel2,
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 3
        };
        p_MainBodyContainer.Panel1.Controls.Add(p_WorkingBodyContainer);

        /*Load the previous view state*/
        if (RuntimeState.ObjectExists("mainIDE.viewState")) {
            CurrentViewState = (IDEViewState)(byte)
                RuntimeState.GetObject("mainIDE.viewState", typeof(byte));
        }

        /*Load previous state (note: do this once the form has loaded so
         the size of the window is correctly validated)*/
        Load += delegate(object sender, EventArgs e) {
            if (RuntimeState.ObjectExists("mainIDE.componentRightWidth")) {
                SetRightSideWidth((int)RuntimeState.GetObject("mainIDE.componentRightWidth", typeof(int)));
            }
            else {
                SetRightSideWidth(200);
            }
            if (RuntimeState.ObjectExists("mainIDE.componentBottomHeight")) {
                SetBodyBottomHeight((int)RuntimeState.GetObject("mainIDE.componentBottomHeight", typeof(int)));
            }
            else {
                SetBodyBottomHeight(100);
            }

            /*When the components have been resized, save it so that the application
             would resume that size when it's reloaded.*/
            p_MainBodyContainer.SplitterMoved += delegate(object s, SplitterEventArgs a) {
                RuntimeState.SetObject("mainIDE.componentRightWidth", (int)p_MainBodyContainer.Panel2.Width);
            };
            p_WorkingBodyContainer.SplitterMoved += delegate(object s, SplitterEventArgs a) {
                RuntimeState.SetObject("mainIDE.componentBottomHeight", (int)p_WorkingBodyContainer.Panel2.Height);
            };
        };
    }

    private void initializeSolutionBrowser() { 
        //create the solution browser control
        p_SolutionExplorer = new SolutionBrowserControl() { Dock = DockStyle.Fill };

        //create the dockable window to hold the solution explorer
        DockableControl dock = new DockableControl() { 
            Dock = DockStyle.Fill,
            Title = "Solution Explorer"
        };
        dock.WorkingArea.Controls.Add(p_SolutionExplorer);

        //add the control to the control skeleton
        p_MainBodyContainer.Panel2.Controls.Add(dock);

        #region Dock events
        /*When the dock control is docked, adjust the skeleton to make it visible*/
        dock.DockClicked += delegate(object sender, EventArgs e) {
            p_MainBodyContainer.Panel2Collapsed = false;
        };
        /*When the dock control is undocked, adjust the skeleton to make it invisible*/
        dock.UndockClicked += delegate(object sender, EventArgs e) {
            p_MainBodyContainer.Panel2Collapsed = true;
        };
        /*When the dock is closed, just make it invisible but DON'T dispose it*/
        dock.CloseClicked += delegate(object sender, EventArgs e) {
            p_MainBodyContainer.Panel2Collapsed = true;
        };
        #endregion

        #region Solution events
        p_SolutionExplorer.FileOpened += delegate(SolutionBrowserControl sender, ProjectFile file) {
            AddTab(file.PhysicalLocation);
        };
        p_SolutionExplorer.FileDelete += delegate(SolutionBrowserControl sender, ProjectFile file) {
            RemoveTab(file.PhysicalLocation);
        };
        p_SolutionExplorer.SolutionChanged += delegate(SolutionBrowserControl sender, Solution solution) {
            saveState();

            p_Title = solution.SolutionName + " - " + p_BaseTitle;
            Text = p_Title;
        };
        #endregion

        //load the solution that was loaded in a previous
        //instance of the IDE
        if (RuntimeState.ObjectExists("mainIDE.openedSolution")) {
            string solutionFilename = RuntimeState.GetObjectString("mainIDE.openedSolution");
            if (!File.Exists(solutionFilename)) { return; }
            p_SolutionExplorer.Solution = new Solution(solutionFilename);
        }

        //restore the state of the solution control
        p_SolutionExplorer.LoadTreeExpandState("mainIDE.solutionBrowserState");
    }
    private void initializeFileEditor() { 
        //intitialize the tabs
        p_Tabs = new TabControl { 
            Dock = DockStyle.Fill,
            Visible = false
        };

        //initialize the right click menu for the tabs
        ContextMenuStrip rightClickMenu = new ContextMenuStrip();
        rightClickMenu.Opening += delegate(object sender, System.ComponentModel.CancelEventArgs e) {
            rightClickMenu.Items.Clear();
            if (p_Tabs.SelectedTab == null) { return; }
            rightClickMenu.Items.Add("Close", null, menu_file_closeFile);
            rightClickMenu.Items.Add("Close all but this", null, menu_file_closeAllOtherFiles);
            rightClickMenu.Items.Add(new ToolStripSeparator());
            rightClickMenu.Items.Add("Save", Icons.GetBitmap("menu.save", 16), menu_file_save);
        };
        p_Tabs.ContextMenuStrip = rightClickMenu;

        //add the tab control
        p_WorkingBodyContainer.Panel1.Controls.Add(p_Tabs);

        //load all the files that were loaded the last time the program was running
        if (RuntimeState.ObjectExists("mainIDE.openedFiles") && p_SolutionExplorer.Solution != null) {
            byte[] rawData = RuntimeState.GetObjectData("mainIDE.openedFiles");
            string[] files = Helpers.StringArrayFromBytes(rawData, false);
            foreach (string file in files) { 
                //does the file still exist?
                if (!File.Exists(file)) { continue; }

                //add the file
                AddTab(file);
            }

            //select the selected file in the previous instance
            if (RuntimeState.ObjectExists("mainIDE.openedFile")) {
                string fileSelected = RuntimeState.GetObjectString("mainIDE.openedFile");
                fileSelected = fileSelected.ToLower();
                foreach (TabPage t in p_Tabs.TabPages) {
                    if (((string)t.Tag).ToLower() == fileSelected) {
                        p_Tabs.SelectedTab = t;
                        break;
                    }
                }
            }
        }
    }
    private void initializeOutputWindow() { 
        //create the dockable window for the output window
        DockableControl dock = new DockableControl() { 
            Dock = DockStyle.Fill,
            Title = "Output"
        };

        //initialize the output window
        p_OutputWindow = new OutputWindow(p_WorkingBodyContainer.Panel2.Width);
        dock.WorkingArea.Controls.Add(p_OutputWindow);

        #region Output window events
        p_OutputWindow.FileGoto += delegate(ProjectFile file, int line, int column) {
            if (file == null) { return; }

            //get/create the tab for the file
            TabPage page = GetTab(file.PhysicalLocation);
            if (page == null) { page = AddTab(file.PhysicalLocation); }

            //select the line and column
            TextEditor editor = (TextEditor)page.Controls[0];
            editor.SelectWordAtColumn(line - 1, column);
        };
        #endregion

        #region Dock events
        dock.DockClicked += delegate(object sender, EventArgs e) {
            p_WorkingBodyContainer.Panel2Collapsed = false;
        };
        dock.UndockClicked += delegate(object sender, EventArgs e) {
            p_WorkingBodyContainer.Panel2Collapsed = true;
        };
        dock.CloseClicked += delegate(object sender, EventArgs e) {
            p_WorkingBodyContainer.Panel2Collapsed = true;
        };
        #endregion

        //add the dock to the bottom side of the body
        p_WorkingBodyContainer.Panel2.Controls.Add(dock);
        return;
    }

    private void initializeStatusStrip() {

        //create the ready and text editor status's
        ToolStripStatusLabel mainStatus = new ToolStripStatusLabel("Ready");
        ToolStripStatusLabel textEditorLinesCount = new ToolStripStatusLabel("Lines 0");
        ToolStripStatusLabel textEditorLineNumber = new ToolStripStatusLabel("Line 0");
        ToolStripStatusLabel textEditorColumnNumber = new ToolStripStatusLabel("Column 0");

        //define the seperator to add some spacing between the labels.
        string seperator = new string(' ', 10);

        //add them
        p_StatusStrip.Items.Add(mainStatus);

        //makes the labels added after this to be aligned on the right side
        p_StatusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });

        p_StatusStrip.Items.Add(textEditorLinesCount);
        p_StatusStrip.Items.Add(seperator);

        p_StatusStrip.Items.Add(textEditorLineNumber);
        p_StatusStrip.Items.Add(seperator);

        p_StatusStrip.Items.Add(textEditorColumnNumber);
        p_StatusStrip.Items.Add(seperator);

        

        //define the global status labels to be the ones
        //we just added. We add them in order of appearance from
        //left to right.
        p_StatusLabels = new ToolStripStatusLabel[] { 
            mainStatus,
            textEditorLinesCount,
            textEditorLineNumber,
            textEditorColumnNumber
        };
    }
   
    private Panel bodyBottomPanel { get { return p_WorkingBodyContainer.Panel2; } }
    private Panel bodyTopPanel { get { return p_WorkingBodyContainer.Panel1; } }
    private Panel rightPanel { get { return p_MainBodyContainer.Panel2; } }

    public Solution CurrentSolution { 
        get { return p_SolutionExplorer.Solution; }
        set {
            p_SolutionExplorer.Solution = value;
        }
    }

    #region Menu
    private ToolStripItem[] getFileMenuItems() {
        return new ToolStripItem[] { 
            #region New
            new ToolStripMenuItem("New...", null, new ToolStripItem[] {
                new ToolStripMenuItem("Solution", Icons.GetBitmap("solutionexplorer.solution", 16), menu_file_new_solution),
                new ToolStripMenuItem("Project", Icons.GetBitmap("solutionexplorer.project",16), menu_file_new_project),
                new ToolStripMenuItem("File", Icons.GetBitmap("filetype.unknown",16), menu_file_new_file, Keys.Control | Keys.N),
            }),
            #endregion
            
            #region Import
            new ToolStripSeparator(),

            new ToolStripMenuItem("Import", null, new ToolStripItem[] { 
                new ToolStripMenuItem("New project from directory", Icons.GetBitmap("solutionexplorer.folder",16), menu_file_import_projectFromDirectory)
            }),

            new ToolStripSeparator(),
            #endregion

            /*Open*/
            new ToolStripMenuItem("Open solution", null, menu_file_openSolution),
            
            /*Print*/
            new ToolStripSeparator(),
            new ToolStripMenuItem("Print", Icons.GetBitmap("menu.print", 16), menu_file_print) { 
                ShortcutKeys = Keys.Control | Keys.P
            },
            new ToolStripMenuItem("Print Preview", Icons.GetBitmap("menu.printPreview", 16), menu_file_printPreview),

            /*Save*/
            new ToolStripSeparator(),
            new ToolStripMenuItem("Save", Icons.GetBitmap("menu.save", 16), menu_file_save) { 
                ShortcutKeys = Keys.Control | Keys.S
            },
            new ToolStripMenuItem("Save all", Icons.GetBitmap("menu.saveAll", 16), menu_file_saveAll),

            /*Close*/
            new ToolStripSeparator(),
            new ToolStripMenuItem("Close file", null, menu_file_closeFile),
            new ToolStripMenuItem("Close all other files", null, menu_file_closeAllOtherFiles),
            new ToolStripMenuItem("Close solution", null, menu_file_closeSolution),

            /*Exit*/
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, delegate(object sender, EventArgs e) {
                Close();
            })
        };
    }
    private ToolStripItem[] getEditMenuItems() {
        return new ToolStripItem[] { 
            new ToolStripMenuItem("Undo", Icons.GetBitmap("menu.undo", 16), menu_edit_undo) { ShortcutKeys = Keys.Control | Keys.Z },
            new ToolStripMenuItem("Redo", Icons.GetBitmap("menu.redo", 16), menu_edit_redo),

            new ToolStripSeparator(),

            new ToolStripMenuItem("Cut", Icons.GetBitmap("menu.cut", 16), menu_edit_cut) { ShortcutKeys = Keys.Control | Keys.X },
            new ToolStripMenuItem("Copy", Icons.GetBitmap("menu.copy", 16), menu_edit_copy) { ShortcutKeys = Keys.Control | Keys.C },
            new ToolStripMenuItem("Paste", Icons.GetBitmap("menu.paste", 16), menu_edit_paste) { ShortcutKeys = Keys.Control | Keys.V },

        };
    }
    private ToolStripItem[] getBuildMenuItems() {
        return new ToolStripItem[] {
            p_DropDownRunButton = new ToolStripMenuItem("Run", Icons.GetBitmap("tools.run", 16), menu_build_run, Keys.F5),
            new ToolStripMenuItem("Stop", Icons.GetBitmap("tools.stop", 16), menu_build_stop),
            
            new ToolStripSeparator(),
            new ToolStripMenuItem("Build", Icons.GetBitmap("tools.build", 16), menu_build_build, Keys.F6)
        };
    }
    private ToolStripItem[] getToolsMenuItems() {
        return new ToolStripItem[] { 
        
            /*Launch command prompt*/
            new ToolStripMenuItem("Launch Command Prompt", Icons.GetBitmap("tools.cmd", 16), delegate(object sender, EventArgs e) {
               Helpers.SpawnProcess("cmd", "");
            }),
            new ToolStripMenuItem("Launch Command Prompt from...", Icons.GetBitmap("tools.cmd", 16), new ToolStripItem[]{
                new ToolStripMenuItem("System directory", null, delegate(object sender, EventArgs e){
                    Helpers.SpawnProcess("cmd", Environment.SystemDirectory, "");
                }),
                new ToolStripMenuItem("IDE directory", null, delegate(object sender, EventArgs e) {
                    Helpers.SpawnProcess("cmd", "./", "");
                }),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Compilers directory", null, delegate(object sender, EventArgs e) {
                    Helpers.SpawnProcess("cmd", "./compilers", "");
                }),
                new ToolStripMenuItem("Emulators directory", null, delegate(object sender, EventArgs e) { 
                    Helpers.SpawnProcess("cmd", "./emulators", ""); 
                }),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Project build directory", null, delegate(object sender, EventArgs e) { 
                    if(CurrentSolution == null) {
                        MessageBox.Show(
                            "You must have a solution open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    if(CurrentSolution.Projects.Length == 0) {
                        MessageBox.Show(
                            "The solution must be at least one project in the solution",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    Project project = p_SolutionExplorer.GetSelectedProject();
                    if(project==null) { project = CurrentSolution.Projects[0]; }
                    Helpers.SpawnProcess("cmd", project.BuildDirectory, "");
                }),
                new ToolStripMenuItem("Solution build directory", null, delegate(object sender, EventArgs e) {
                    if(CurrentSolution == null) {
                        MessageBox.Show(
                            "You must have a solution open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    Helpers.SpawnProcess("cmd", CurrentSolution.BuildDirectory, "");
                })
            }),


            /*Clear state information*/
            new ToolStripSeparator(),
            new ToolStripMenuItem("Clear state information", null, delegate(object sender, EventArgs e) {
                DialogResult res = MessageBox.Show(
                    "Warning! Clearing the state would clear all session information including layout of which a restart is required.\n\nSave all your current work before continuing.\nAre you sure you want to continue?",
                    "Warning",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);
                if(res!= DialogResult.Yes){return;}

                RuntimeState.Clear();
                Process.Start("oside.exe");
                Process.GetCurrentProcess().Kill();
            })
        };
    }
    private ToolStripItem[] getHelpMenuItems() {
        return new ToolStripItem[] { 
            new ToolStripMenuItem("Change log", null, delegate(object sender, EventArgs e) {
                if(!File.Exists("changelog.txt")) {
                    MessageBox.Show("Changelog.txt does not exist",
                                    "Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                    return;
                }
                AddTab("changelog.txt", true);
            }),
            new ToolStripMenuItem("About", null, delegate(object sender, EventArgs e) {
                About.Show();
            })
        };
    }

    #region File functions
    private void menu_file_new_solution(object sender, EventArgs e) { 
        //prompt for the name of the solution
        string solutionName = Input.Show("Enter the name of the new solution", "Name", "NewSolution");
        if (solutionName == null) { return; }

        //check that the solution is valid
        if (!Helpers.ValidFilename(solutionName)) {
            MessageBox.Show("The name of the solution \"" + solutionName + "\" is invalid",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            menu_file_new_solution(sender, e);
            return;
        }

        //get the location where the solution would be located.
        string solutionLocation =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" +
            "OS Development Studio\\Projects\\" + solutionName;

        //make sure the solution name does not already exist
        if (Directory.Exists(solutionLocation)) {
            MessageBox.Show("The solution \"" + solutionName + "\" already exists",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            menu_file_new_solution(sender, e);
            return;
        }

        //create the solution
        Directory.CreateDirectory(solutionLocation);
        Solution solution = Solution.CreateSolution(
            solutionLocation + "\\solution.ossln",
            solutionName);

        //set the new solution to the current solution open
        CurrentSolution = solution;
    }
    private void menu_file_new_project(object sender, EventArgs e) { 
        //there must a solution open
        if (CurrentSolution == null) {
            MessageBox.Show("You must have a solution open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //prompt the user for a project name
        string projectName = Input.Show("Enter the name of the new project", "Name", "NewProject");
        if (projectName == null) { return; }

        //valid project name?
        if (!Helpers.ValidFilename(projectName)) {
            MessageBox.Show("The name of the project \"" + projectName + "\" is invalid",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            menu_file_new_project(sender, e);
            return;
        }

        //does the project already exist?
        if (CurrentSolution.GetProject(projectName) != null) {
            MessageBox.Show("The project \"" + projectName + "\" already exists.");
            menu_file_new_project(sender, e);
            return;
        }

        //create the project
        Project project = CurrentSolution.CreateProject(projectName);
        p_SolutionExplorer.RefreshProjectList();
        p_SolutionExplorer.ExpandSolution();
    }
    private void menu_file_new_file(object sender, EventArgs e) { 
        //there must be a solution open
        if (CurrentSolution == null) {
            MessageBox.Show("You must have a solution open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //there must at least one project in the solution
        if (CurrentSolution.Projects.Length == 0) {
            MessageBox.Show("You must have at least one project in the solution \"" + CurrentSolution.SolutionName + "\"",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //get the filename of the new file to add
        string filename = AddProjectItem.Show();
        if (filename == null) { return; }

        //get the currently selected directory
        ProjectEntity selected = p_SolutionExplorer.GetSelectedProjectEntity();
        if (selected == null) { 
            //select the project's root folder instead
            Project proj = p_SolutionExplorer.GetSelectedProject();
            if (proj == null) { proj = CurrentSolution.Projects[0]; }
            selected = proj.Root;
        }
        if (selected is ProjectFile) { selected = selected.Parent; }
        ProjectDirectory directory = (ProjectDirectory)selected;

        //does the file already exists?
        if (directory.EntityExists(filename)) {
            MessageBox.Show("The name \"" + filename + "\" is already in use in directory \"" + directory.FullName + "\"",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            menu_file_new_file(sender, e);
            return;
        }

        //create the file
        ProjectFile file = directory.CreateFile(filename);
        p_SolutionExplorer.RefreshAll();
        p_SolutionExplorer.ExpandDirectory(directory);
    }
    
    
    private void menu_file_import_projectFromDirectory(object sender, EventArgs e) { 
        //there must a solution open
        if (CurrentSolution == null) {
            MessageBox.Show("You must have a solution open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //prompt the user for the directory to import from
        FolderBrowserDialog dirSelect = new FolderBrowserDialog();
        if (dirSelect.ShowDialog() != DialogResult.OK) { return; }
        DirectoryInfo physicalDirectory = new DirectoryInfo(dirSelect.SelectedPath);

        //prompt for the project name
        string projectName = Input.Show("Project name", "Name", physicalDirectory.Name);
        if (projectName == null) { return; }

        //valid project name?
        if (!Helpers.ValidFilename(projectName)) {
            MessageBox.Show("The name of the project \"" + projectName + "\" is invalid",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            menu_file_import_projectFromDirectory(sender, e);
            return;
        }

        //does the project already exist?
        if (CurrentSolution.GetProject(projectName) != null) {
            MessageBox.Show("The project \"" + projectName + "\" already exists.");
            menu_file_import_projectFromDirectory(sender, e);
            return;
        }

        //create the initial project 
        Project project = CurrentSolution.CreateProject(projectName);
        
        //import
        importProjectFromDirectory(project.Root, physicalDirectory);

        //refresh the project list
        p_SolutionExplorer.RefreshProjectList();
    }
    private void importProjectFromDirectory(ProjectDirectory vDir, DirectoryInfo pDir) { 
        //add the directories
        foreach (DirectoryInfo d in pDir.GetDirectories()) {
            importProjectFromDirectory(
                vDir.CreateDirectory(d.Name),
                d);
        }

        //add the files
        foreach (FileInfo f in pDir.GetFiles()) {
            ProjectFile vFile = vDir.CreateFile(f.Name);
            File.Copy(f.FullName, vFile.PhysicalLocation, true);
        }
    }

    private void menu_file_openSolution(object sender, EventArgs e) {
        //prompt for the filename to open
        OpenFileDialog open = new OpenFileDialog { 
            Filter = "Solution file|*.ossln"
        };
        if (open.ShowDialog() != DialogResult.OK) { return; }

        //attempt to open the solution
        try {
            CurrentSolution = new Solution(open.FileName);
        }
        catch { }
    }

    private void menu_file_print(object sender, EventArgs e) { 
        //is there a file open?
        TextEditor editor = getCurrentEditor();
        if (editor == null) {
            MessageBox.Show("No file is currently open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //print 
        editor.Print((string)p_Tabs.SelectedTab.Tag);
    }
    private void menu_file_printPreview(object sender, EventArgs e) {
        //is there a file open?
        TextEditor editor = getCurrentEditor();
        if (editor == null) {
            MessageBox.Show("No file is currently open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //show the print preview dialog
        editor.PrintPreview((string)p_Tabs.SelectedTab.Tag);
    }

    private void menu_file_save(object sender, EventArgs e) {
        //any files open?
        if (p_Tabs.TabPages.Count == 0) {
            MessageBox.Show("No file is currently open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //save the current tab
        SaveTab(p_Tabs.SelectedTab);
    }
    private void menu_file_saveAll(object sender, EventArgs e) { 
        //save all the tabs
        foreach (TabPage t in p_Tabs.TabPages) {
            SaveTab(t);
        }
    }

    private void menu_file_closeFile(object sender, EventArgs e) {
        //is there any files open?
        if (p_Tabs.TabPages.Count == 0) {
            MessageBox.Show("No file is currently open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //close the file that is open
        string openTab = (string)p_Tabs.SelectedTab.Tag;
        RemoveTab(openTab);
    }
    private void menu_file_closeAllOtherFiles(object sender, EventArgs e) {
        //is there any files open?
        if (p_Tabs.TabPages.Count == 0) {
            MessageBox.Show("No file is currently open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //get the current tab
        TabPage current = p_Tabs.SelectedTab;

        //remove all tabs but the current one
        TabControl.TabPageCollection tabs = p_Tabs.TabPages;
        for (int c = tabs.Count - 1; c != -1; c--) {
            if (tabs[c] == current) { continue; }
            RemoveTab((string)tabs[c].Tag);
        }
    }
    private void menu_file_closeSolution(object sender, EventArgs e) { 
        //is a solution open?
        if (p_SolutionExplorer.Solution == null) {
            MessageBox.Show("No solution is currently open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //close all files
        while (p_Tabs.TabPages.Count != 0) {
            RemoveTab((string)p_Tabs.TabPages[0].Tag);
        }

        //close the solution
        p_SolutionExplorer.Solution = null;
    }
    #endregion

    #region Build functions
    /*Run button also qualifies as the pause button.*/
    private void menu_build_run(object sender, EventArgs e) { 
        //emulator running?
        if (p_Emulator != null) {
            //resume/suspend
            if (p_Emulator.Suspended) { p_Emulator.Resume(); }
            else { p_Emulator.Suspend(); }

            //update the menu buttons for the run/suspend function
            p_MenuStripRunButton.Image = Icons.GetBitmap(
                "tools." + (p_Emulator.Suspended ? "run" : "pause"),
                16);
            p_DropDownRunButton.Text = (p_Emulator.Suspended ? "Resume" : "Pause");
            p_DropDownRunButton.Image = p_MenuStripRunButton.Image;
            return;
        }

        //build and run
        ICompilerOutput<Solution> output = tryBuild();
        if (output == null) { return; }
        readOnlyMode(true);
        p_Emulator = output.CreateEmulator();
        p_Emulator.Start(16, delegate(string line) { p_OutputWindow.WriteLine(line); });
        Text = p_Title + " (Running)";

        //update the menu buttons
        p_MenuStripRunButton.Image = Icons.GetBitmap("tools.pause",16);
        p_DropDownRunButton.Image = p_MenuStripRunButton.Image;
        p_DropDownRunButton.Text = "Pause";

        //create a timer to automatically set the state to "not running"
        //when the process is closed
        Timer timer = new Timer() { 
            Interval = 10,
            Enabled = true
        };
        timer.Tick += delegate(object s, EventArgs a) {
            if (!p_Emulator.Running) {
                p_MenuStripRunButton.Image = Icons.GetBitmap("tools.run", 16);
                p_DropDownRunButton.Image = p_MenuStripRunButton.Image;
                p_DropDownRunButton.Text = "Run";

                p_Emulator = null;
                ((Timer)s).Enabled = false;
                readOnlyMode(false);
                Text = p_Title;
            }
        };
    }
    private void menu_build_stop(object sender, EventArgs e) { 
        //emulator running?
        if (p_Emulator == null) {
            MessageBox.Show("Emulator is not currently running",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //kill the emulator
        p_Emulator.Stop();
    }
    private void menu_build_build(object sender, EventArgs e) {
        //emulator running?
        if (p_Emulator != null) {
            MessageBox.Show("Emulator cannot be running during a build process.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }
        tryBuild();
    }

    private bool[] p_PreviousReadOnlyState = new bool[0];
    private bool p_ReadOnlyMode = false;
    private void readOnlyMode(bool readOnly) {
        if (p_ReadOnlyMode == readOnly) { return; }
        p_ReadOnlyMode = readOnly;

        TabControl.TabPageCollection tabs = p_Tabs.TabPages;

        //read-only? set all the opened files to read-only
        if (readOnly) {
            p_PreviousReadOnlyState = new bool[tabs.Count];
            for (int c = 0; c < tabs.Count; c++) { 
                TextEditor editor = (TextEditor)tabs[c].Controls[0];
                p_PreviousReadOnlyState[c] = editor.ReadOnly;
                editor.ReadOnly = true;
            }
        }
        else {
            //restore the readonly state of the tabs
            for (int c = 0; c < tabs.Count; c++) {
                TextEditor editor = (TextEditor)tabs[c].Controls[0];
                editor.ReadOnly = p_PreviousReadOnlyState[c];
            }
        }
    }
    private ICompilerOutput<Solution> tryBuild() {
        try {
            return build();
        }
        catch(Exception ex) {
            readOnlyMode(false);
            p_OutputWindow.WriteLine("An error has occured during building!");
            p_OutputWindow.WriteLine("  Message: \"" + ex.Message + "\"");
            p_OutputWindow.WriteLine("  Stack trace: \n" + ex.StackTrace);
            return null;
        }
    }
    private ICompilerOutput<Solution> build() {
        //there must be a solution open
        //there must a solution open
        if (CurrentSolution == null) {
            MessageBox.Show("You must have a solution open",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return null;
        }

        //save all the tabs
        foreach (TabPage p in p_Tabs.TabPages) {
            SaveTab(p);
        }

        //enter read-only mode to prevent modification during compile
        readOnlyMode(true);

        //show the output window
        p_MainBodyContainer.Panel2Collapsed = false;
        p_OutputWindow.SwitchToOutput();
        p_OutputWindow.ClearList();

        //trigger build for the current solution
        ICompiler compiler = new StandardCompiler();
        ICompilerOutput<Solution> output = compiler.Compile(
            CurrentSolution,
            delegate(string line) {
                p_OutputWindow.WriteLine(line);
            });

        //add the errors/warnings that occured
        for (int c = 0; c < output.Errors.Length; c++) {
            p_OutputWindow.AddEntry(
                false,
                output.Errors[c].File,
                output.Errors[c].Message,
                output.Errors[c].Line,
                output.Errors[c].Column);
        }
        for (int c = 0; c < output.Warnings.Length; c++) {
            p_OutputWindow.AddEntry(
                true,
                output.Warnings[c].File,
                output.Warnings[c].Message,
                output.Warnings[c].Line,
                output.Warnings[c].Column);
        }

        //select the errors tab if the compile was unsuccessfull.
        if (output.Errors.Length != 0) {
            p_OutputWindow.SwitchToErrors();
            readOnlyMode(false);
            return null;
        }

        //clean up
        readOnlyMode(false);
        return output;
    }

    #endregion

    #region Edit functions
    private void menu_edit_undo(object sender, EventArgs e) {
        TextEditor editor = getCurrentEditor();
        if (editor == null) { return; }
        editor.Undo();
    }
    private void menu_edit_redo(object sender, EventArgs e) {
        TextEditor editor = getCurrentEditor();
        if (editor == null) { return; }
        editor.Redo();
    }

    private void menu_edit_cut(object sender, EventArgs e) {
        TextEditor editor = getCurrentEditor();
        if (editor == null) { return; }
        editor.Cut();
    }
    private void menu_edit_copy(object sender, EventArgs e) {
        TextEditor editor = getCurrentEditor();
        if (editor == null) { return; }
        editor.Copy();
    }
    private void menu_edit_paste(object sender, EventArgs e) {
        TextEditor editor = getCurrentEditor();
        if (editor == null) { return; }
        editor.Paste();
    }
    #endregion

    #endregion

    #region Tabs
    public TabPage GetTab(string filename) {
        filename = new FileInfo(filename).FullName;
        foreach (TabPage t in p_Tabs.TabPages) {
            if (((string)t.Tag).ToLower() == filename.ToLower()) {
                return t;
            }
        }
        return null;
    }

    public TabPage AddTab(string filename) {
        return AddTab(filename, false);
    }
    public TabPage AddTab(string filename, bool readOnly) {
        filename = new FileInfo(filename).FullName;

        //already opened?
        TabPage buffer = GetTab(filename);
        if (buffer != null) {
            p_Tabs.SelectedTab = buffer;
            return buffer; 
        }

        //create the text editor
        TextEditor editor = new TextEditor(filename) { Dock = DockStyle.Fill };
        if (readOnly) { editor.ToggleReadOnly(); }
        else {
            editor.Modified += delegate(object sender, EventArgs e) {
                TextEditor inst = (TextEditor)sender;
                TabPage p = (TabPage)inst.Parent;
                if (!p.Text.EndsWith("*")) {
                    p.Text += " *";
                }
            };
        }

        //create the tab
        buffer = new TabPage() { 
            Text = new FileInfo(filename).Name,
            Tag = filename,
            BorderStyle = BorderStyle.None
        };
        buffer.Controls.Add(editor);

        //are we in read-only mode?
        if (p_ReadOnlyMode) {
            Helpers.AddObject(ref p_PreviousReadOnlyState, readOnly);
            editor.ReadOnly = true;
        }

        //clean up
        p_Tabs.Visible = true;
        p_Tabs.TabPages.Add(buffer);
        p_Tabs.SelectedTab = buffer;
        saveState();
        return buffer;
    }

    public bool RemoveTab(string filename) {
        //don't allow the tab to be removed if we are 
        //in read-only mode
        if (p_ReadOnlyMode) {
            MessageBox.Show("Unable to remove tab, the application is in read-only mode.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return false;
        }

        TabPage tab = GetTab(filename);
        if (tab == null) { return false; }
        p_Tabs.TabPages.Remove(tab);
        if (p_Tabs.TabPages.Count == 0) {
            p_Tabs.Visible = false;
        }
        return true;
    }
    public void SaveTab(TabPage page) {
        //don't allow save if we are in read-only mode.
        if (p_ReadOnlyMode) {
            MessageBox.Show("Unable to save in read-only mode",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //get the text editor component
        TextEditor editor = (TextEditor)page.Controls[0];

        //set the modified date so that when we save the file,
        //it doesn't conflict with the file change monitor
        editor.ModifiedTime = DateTime.Now;

        //save the contents
        File.WriteAllText(
                (string)page.Tag,
                editor.Contents);

        //remove the "modified" character from the tab title
        if (!page.Text.EndsWith("*")) { return; }
        page.Text = page.Text.Substring(0, page.Text.Length - 2);
    }

    public void RemoveDeletedFileTabs() {
        TabControl.TabPageCollection tabs = p_Tabs.TabPages;
        for (int c = tabs.Count - 1; c != -1; c--) {
            TabPage page = tabs[c];
            if (!File.Exists((string)page.Tag)) {
                RemoveTab((string)page.Tag);
            }
        }
    }
    #endregion

    #region Events
    private bool p_SaveStateEnabled = false;
    private void saveState() {
        if (!p_SaveStateEnabled) { return; }
        RuntimeState.Clear();

        //write window rectangle/state
        saveCurrentWindowRect();
        RuntimeState.SetObject("mainIDE.windowState", (byte)WindowState);

        //write right and bottom component sizes
        RuntimeState.SetObject("mainIDE.componentRightWidth", (int)p_MainBodyContainer.Panel2.Width);
        RuntimeState.SetObject("mainIDE.componentBottomHeight", (int)p_WorkingBodyContainer.Panel2.Height);

        //write the current view state
        RuntimeState.SetObject("mainIDE.viewState", (byte)CurrentViewState);

        //write the currently opened solution
        if (p_SolutionExplorer.Solution != null) {
            RuntimeState.SetObjectString("mainIDE.openedSolution", p_SolutionExplorer.Solution.Filename);
        }

        //write the currently opened file
        if (p_Tabs.SelectedTab != null) {
            RuntimeState.SetObjectString("mainIDE.openedFile", (string)p_Tabs.SelectedTab.Tag);
        }

        //save the state of the solution explorer
        p_SolutionExplorer.SaveTreeExpandState("mainIDE.solutionBrowserState");

        //write all of the currently opened files
        string[] openedFiles = new string[p_Tabs.TabPages.Count];
        for (int c = 0; c < openedFiles.Length; c++) {
            openedFiles[c] = (string)p_Tabs.TabPages[c].Tag;
        }
        byte[] openedFilesBytes = Helpers.StringArrayToBytes(openedFiles, false);
        RuntimeState.SetObjectData("mainIDE.openedFiles", openedFilesBytes);
    }

    private void saveCurrentWindowRect() {
        //do not save if we're maximized/minimized!
        if (WindowState != FormWindowState.Normal) {
            return;
        }

        RuntimeState.SetObject(
            "mainIDE.windowRect",
            new RuntimeState.RECTANGLE(
                Location.X, Location.Y,
                Width, Height));
    }

    private FormWindowState p_OldWindowState;
    protected override void OnResize(EventArgs e) {
        base.OnResize(e);
        
        //has the window state changed?
        if (p_OldWindowState != WindowState) { 
            //save it
            RuntimeState.SetObject("mainIDE.windowState", (byte)WindowState);
            p_OldWindowState = WindowState;
        }
    }
    protected override void OnResizeEnd(EventArgs e) {
        base.OnResize(e);
        saveCurrentWindowRect();
    }
    protected override void OnMove(EventArgs e) {
        base.OnMove(e);
        saveCurrentWindowRect();
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        base.OnFormClosing(e);

        saveState();
        if (p_Emulator != null) {
            p_Emulator.Stop();
        }
    }
    #endregion

    public IDEViewState CurrentViewState {
        get {
            IDEViewState state = IDEViewState.NONE;

            /*Base view state*/
            if (!p_MainBodyContainer.Panel2Collapsed) {
                state |= IDEViewState.RIGHT_PANEL;
            }
            if (!p_WorkingBodyContainer.Panel2Collapsed) {
                state |= IDEViewState.BOTTOM_PANEL;
            }

            return state;
        }
        set { 
            //make right/bottom panel visible?
            p_MainBodyContainer.Panel2Collapsed =
                (value & IDEViewState.RIGHT_PANEL) != IDEViewState.RIGHT_PANEL;
            p_WorkingBodyContainer.Panel2Collapsed =
                (value & IDEViewState.BOTTOM_PANEL) != IDEViewState.BOTTOM_PANEL;

            //apply changes for the next instance so we "remember" the settings.
            RuntimeState.SetObject("mainIDE.viewState", (byte)value);
        }
    }

    private TextEditor getCurrentEditor() { 
        TabPage currentTab = p_Tabs.SelectedTab;

        //no file open?
        if (currentTab == null) { return null; }

        //get the text editor
        return (TextEditor)currentTab.Controls[0];
    }

    private void uiUpdate(object sender, EventArgs e) {
        updateTextEditorStatusLabels();
    }
    private void updateTextEditorStatusLabels() { 
        //grab the text editor status components
        ToolStripStatusLabel linesCount = p_StatusLabels[1];
        ToolStripStatusLabel lineNumber = p_StatusLabels[2];
        ToolStripStatusLabel columnNumber = p_StatusLabels[3];

        //is there a file open?
        TabPage currentTab = p_Tabs.SelectedTab;
        if (currentTab == null) { 
            //do not show the text editor status because
            //there isnt one open.
            linesCount.Visible = false;
            lineNumber.Visible = false;
            columnNumber.Visible = false;
            return;
        }

        //get the text editor
        TextEditor editor = (TextEditor)currentTab.Controls[0];
        
        //set the information
        linesCount.Text = "Lines " + editor.LineCount;
        lineNumber.Text = "Line " + editor.LineNumber;
        columnNumber.Text = "Column " + editor.ColumnNumber;

        //show the line number/column number
        linesCount.Visible = true;
        lineNumber.Visible = true;
        columnNumber.Visible = true;
    }

    public void SetRightSideWidth(int width) {
        p_MainBodyContainer.SplitterDistance = p_WorkingArea.Width - width;
    }
    public void SetBodyBottomHeight(int height) {
        p_WorkingBodyContainer.SplitterDistance = p_WorkingArea.Height - height;
    }

    private class toolStripRenderer : ToolStripProfessionalRenderer {
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
    }
    private class centerLabel : Label {
        public centerLabel() {
            ParentChanged += delegate(object sender, EventArgs e) {
                Parent.Resize += delegate(object sender2, EventArgs e2) {
                    update();
                };
            };
            Resize += delegate(object sender, EventArgs e) {
                update();
            };

            HandleCreated += delegate(object sender, EventArgs e) { update(); };
        }

        private void update() {
            Location = new Point(
                (Parent.Width / 2) - (Width / 2),
                (Parent.Height / 2) - (Height / 2));
        }
    }
}