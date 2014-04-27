using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

public class MainIDE : Form {
    private Panel p_WorkingArea;
    private int p_MainBodySplitDistance;
    private int p_BodySplitDistance;

    private TextEditor p_TextEditor;
    private SolutionBrowserControl p_SolutionExplorer;

    public MainIDE(Splash splash) {
        BackColor = Color.FromArgb(255, 200, 200, 200);
        Icon = Icons.GetIcon("icons.logo", 32);
        Text = "OS Development Studio";
       
        //once the form has loaded, send a signal to the 
        //splash screen to close.
        Load += delegate(object sender, EventArgs e) {
            splash.Ready();
            Focus();
        };

        //set the minimum size of the window to 75% of the current screen
        Size screenSize = Screen.FromPoint(Cursor.Position).WorkingArea.Size;
        MinimumSize = new Size(
                (int)(screenSize.Width * 0.75),
                (int)(screenSize.Height * 0.75));

        //create the initial panels
        StatusStrip status = new StatusStrip();
        p_WorkingArea = new Panel { Dock = DockStyle.Fill };
        ToolStrip menu = new ToolStrip {
            GripStyle = ToolStripGripStyle.Hidden,
            Renderer = new ToolStripProfessionalRenderer() { 
                RoundedEdges = false
            }
        };

        //add the controls
        Controls.Add(menu);
        Controls.Add(status);
        Controls.Add(p_WorkingArea);
        p_WorkingArea.BringToFront();

        /*Build the main menu items*/
        ToolStripItem fileMenu = menu.Items.Add("File");
        fileMenu.Click += delegate(object sender, EventArgs e) {
            buildSplitContainer(
                null,
                new Control() { BackColor = Color.Red },
                p_SolutionExplorer);
        };
        ToolStripItem editMenu = menu.Items.Add("Edit");
        ToolStripItem buildMenu = menu.Items.Add("Build");
        ToolStripItem helpMenu = menu.Items.Add("Help");
        menu.Items.Add(new ToolStripSeparator());
        ToolStripItem run = menu.Items.Add(Icons.GetBitmap("tools.run", 16));
        run.ToolTipText = "Run";

        /*Test code*/
        Solution solution = new Solution("./testsolution/solution.ossln");

        //initialize the components
        p_SolutionExplorer = new SolutionBrowserControl();
        p_TextEditor = new TextEditor();
        p_SolutionExplorer.AddSolution(solution);

        //create the components to seperate the different working area
        //components.
        buildSplitContainer(null, null, p_SolutionExplorer);
    }

    private void buildSplitContainer(Control workingArea, Control workingAreaBottom, Control rightSideContainer) {
        //working area cannot be null.
        if (workingArea == null) { workingArea = new Control(); }

        //clean out all controls for the current split container
        p_WorkingArea.Visible = false;
        p_WorkingArea.Controls.Clear();

        //do we add a split container for the right side?
        Panel bodyContainer = p_WorkingArea;
        if (rightSideContainer != null) {
            //initialize the container which would have the right side
            SplitContainer container = new SplitContainer() { 
                FixedPanel = FixedPanel.Panel2,
                Dock = DockStyle.Fill,
                SplitterDistance = p_MainBodySplitDistance,
                Panel2MinSize = 0
            };

            container.SplitterMoved += delegate(object sender, SplitterEventArgs e) {
                p_MainBodySplitDistance = container.Panel1.Width;
            };
            p_WorkingArea.Controls.Add(container);

            //add the right side container
            rightSideContainer.Dock = DockStyle.Fill;
            container.Panel2.Controls.Add(rightSideContainer);
            bodyContainer = container.Panel1;
        }

        //do we add the split container for the bottom side?
        if (workingAreaBottom != null) { 
            //initialize the container which would have the bottom side
            SplitContainer container = new SplitContainer() {
                FixedPanel = FixedPanel.Panel2,
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = p_BodySplitDistance
            };
            container.SplitterMoved += delegate(object sender, SplitterEventArgs e) {
                p_BodySplitDistance = e.SplitY;
            };
            bodyContainer.Controls.Add(container);

            //add the bottom side container
            workingAreaBottom.Dock = DockStyle.Fill;
            container.Panel2.Controls.Add(workingAreaBottom);
            bodyContainer = container.Panel1;
        }

        //add the main working area
        workingArea.Dock = DockStyle.Fill;
        bodyContainer.Controls.Add(workingArea);
        p_WorkingArea.Visible=true;//.ResumeLayout();
    }


    private class toolStripRenderer : ToolStripProfessionalRenderer {
        public toolStripRenderer() {
            RoundedEdges = false;
        }
    }
}