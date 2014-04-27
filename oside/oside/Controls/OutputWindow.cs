using System;
using System.Drawing;
using System.Windows.Forms;

public class OutputWindow : Control {
    private TabControl p_Tabs;
    private TabPage p_ErrorsTab;
    private TabPage p_WarningsTab;
    private TabPage p_OutputTab;

    private ListView p_ErrorList;
    private ListView p_WarningsList;
    private RichTextBox p_Output;

    private string p_OutputString = "";
    private string p_OutputCurrentString = "";

    public OutputWindow(int width) {
        Dock = DockStyle.Fill;

        //initialize the tab control
        ImageList imageList = new ImageList() { 
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(16, 16)
        };
        imageList.Images.Add(Icons.GetBitmap("icons.error", 16));
        imageList.Images.Add(Icons.GetBitmap("icons.warning", 16));
        imageList.Images.Add(Icons.GetBitmap("icons.output", 16));

        p_Tabs = new TabControl() {
            Dock = DockStyle.Fill,
            ImageList = imageList
        };
        Controls.Add(p_Tabs);

        //add the error, warnings and output tab
        p_ErrorsTab = new TabPage("Errors (0)") { ImageIndex = 0 };
        p_WarningsTab = new TabPage("Warnings (0)") { ImageIndex = 1 };
        p_OutputTab = new TabPage("Output") { ImageIndex = 2 };

        //add the error and warning lists
        p_ErrorList = createErrorWarningList(p_ErrorsTab, Icons.GetBitmap("icons.error", 16), width);
        p_WarningsList = createErrorWarningList(p_WarningsTab, Icons.GetBitmap("icons.warning", 16), width);

        //create the output list
        p_Output = new RichTextBox() { 
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.Black,
            ForeColor = Color.FromArgb(255, 190, 190, 190),
            Font = new Font("Consolas", 8),
            BorderStyle = BorderStyle.None
        };
        p_OutputTab.Controls.Add(p_Output);

        //create a timer which automatically refreshes the output window
        //when the output string has changed (we have to do this because
        //the Write function is called asyncronously and can cause deadlocks
        //if we use the Invoke function on it.)
        Timer outputRefresh = new Timer() { 
            Enabled = true,
            Interval = 10
        };
        outputRefresh.Tick += delegate(object sender, EventArgs e) {
            if (p_OutputCurrentString != p_OutputString) {
                p_Output.Text = p_OutputString;
                p_OutputCurrentString = p_OutputString;
                p_Output.SelectionStart = p_OutputString.Length;
                p_Output.ScrollToCaret();
            }
        };


        //add the tab pages
        p_Tabs.TabPages.AddRange(new TabPage[] { 
            p_ErrorsTab, 
            p_WarningsTab,
            p_OutputTab
        });
    }

    public void SwitchToOutput() {
        p_Tabs.SelectedTab = p_OutputTab;
    }
    public void SwitchToErrors() {
        p_Tabs.SelectedTab = p_ErrorsTab;
    }
    public void SwitchToWarnings() {
        p_Tabs.SelectedTab = p_WarningsTab;
    }

    public void Write(string line) {
        p_OutputString += line;
        Application.DoEvents();
    }
    public void WriteLine(string line) {
        line =
            DateTime.Now.ToString() + " > " +
            line;
        Write(line + "\n");
    }

    public void AddEntry(bool isWarning, ProjectFile projectFile, string description, int line, int column) { 
        //create the item for the list view
        ListViewItem lsti = new ListViewItem() { 
            ImageIndex = 0,
            Tag = new object[] { 
                projectFile,
                line,
                column
            }
        };
       
        lsti.SubItems.Add(description);
        lsti.SubItems.Add(projectFile.Name);
        lsti.SubItems.Add(line.ToString());
        lsti.SubItems.Add(column.ToString());
        lsti.SubItems.Add(projectFile.Project.ProjectName);
        
        //add it to the appropriate list
        ListView lst = (isWarning ? p_WarningsList : p_ErrorList);
        lst.Items.Add(lsti);

        //update the text of the tab
        TabPage tab = (isWarning ? p_WarningsTab : p_ErrorsTab);
        tab.Text = tab.Text.Split(' ')[0];
        tab.Text += " (" + lst.Items.Count + ")";
    }

    public void ClearWarnings() { ClearList(true); }
    public void ClearErrors() { ClearList(false); }
    public void ClearList() {
        ClearList(true);
        ClearList(false);
    }
    public void ClearList(bool isWarning) {
        ListView lst = (isWarning ? p_WarningsList : p_ErrorList);
        lst.Items.Clear();
        TabPage tb = (isWarning ? p_WarningsTab : p_ErrorsTab);
        tb.Text = tb.Text.Split(' ')[0] + " (0)";
    } 
    
    private void onLstiClick(object sender, EventArgs e) {
        //get the selected item
        ListView.SelectedListViewItemCollection selected = ((ListView)sender).SelectedItems;
        if (selected.Count == 0) { return; }
        ListViewItem lsti = selected[0];

        //get the information about the entry which contains
        //where in the file the entry is occuring.
        object[] tag = (object[])lsti.Tag;

        //trigger the event
        if (FileGoto == null) { return; }
        FileGoto(
            (ProjectFile)tag[0],
            (int)tag[1],
            (int)tag[2]);
    }

    public delegate void FileGotoHandler(ProjectFile file, int line, int column);
    public event FileGotoHandler FileGoto;

    private ListView createErrorWarningList(TabPage page, Bitmap icon, int width) {
        ImageList imgList = new ImageList() { 
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(16, 16),  
        };
        imgList.Images.Add(icon);
        ListView buffer = new ListView() { 
            View = View.Details,
            Dock = DockStyle.Fill,
            SmallImageList = imgList,
            LargeImageList = imgList,
            FullRowSelect = true
        };
        buffer.DoubleClick += onLstiClick;

        width -= 154; //24 - 40 - 40 - 50
        buffer.Columns.Add("", 24);
        buffer.Columns.Add("Description", (int)(width * 0.7));
        buffer.Columns.Add("File", (int)(width * 0.2));
        buffer.Columns.Add("Line", 40);
        buffer.Columns.Add("Column", 40);
        buffer.Columns.Add("Project", 50);

        page.Controls.Add(buffer);
        return buffer;
    }
}