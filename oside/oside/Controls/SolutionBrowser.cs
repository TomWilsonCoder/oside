using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

public class SolutionBrowserControl : Control {
    private TreeView p_Tree;
    private Solution p_Solution;

    public SolutionBrowserControl() {
        #region Image list
        int iconSize = 16;
        ImageList imageList = new ImageList() {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(iconSize, iconSize)
        };

        imageList.Images.Add("folder", Icons.GetBitmap("solutionexplorer.folder", iconSize));
        imageList.Images.Add("folderOpen", Icons.GetBitmap("solutionexplorer.folderOpen", iconSize));
        imageList.Images.Add("asm", Icons.GetBitmap("filetype.asm", iconSize));
        imageList.Images.Add("c", Icons.GetBitmap("filetype.c", iconSize));
        imageList.Images.Add("cpp", Icons.GetBitmap("filetype.cpp", iconSize));
        imageList.Images.Add("h", Icons.GetBitmap("filetype.h", iconSize));
        imageList.Images.Add("vb", Icons.GetBitmap("filetype.vb", iconSize));
        imageList.Images.Add("cs", Icons.GetBitmap("filetype.cs", iconSize));
        imageList.Images.Add("unknown", Icons.GetBitmap("filetype.unknown", iconSize));
        
        imageList.Images.Add("solution", Icons.GetBitmap("solutionexplorer.solution", iconSize));
        imageList.Images.Add("proj", Icons.GetBitmap("solutionexplorer.project", iconSize));
        #endregion

        #region Tree view
        p_Tree = new TreeView() { 
            Dock = DockStyle.Fill,
            ImageList = imageList,
            LabelEdit = true,
    
            AllowDrop = true
        };
        Controls.Add(p_Tree);
       
        /*Drag drop*/
        p_Tree.ItemDrag += tree_itemDrag;
        p_Tree.DragEnter += tree_dragEnter;
        p_Tree.DragOver += tree_dragOver;
        p_Tree.DragDrop += tree_dragDrop;

        /*When the user hit's enter, make whatever is selected in
          the tree list expand.*/
        p_Tree.KeyDown += delegate(object s, KeyEventArgs e) {
            if (e.KeyCode != Keys.Enter) { return; }
            TreeNode selected = p_Tree.SelectedNode;
            if (selected == null) { return; }
            selected.Expand();

            //open the file?
            if (selected.Tag is ProjectFile && FileOpened != null) {
                FileOpened(this, (ProjectFile)selected.Tag);
            }
        };

        /*When the user double clicks on a file, open it*/
        p_Tree.DoubleClick += delegate(object s, EventArgs e) {
            Point cursorPosition = Cursor.Position;
            cursorPosition = p_Tree.PointToClient(cursorPosition);
            TreeNode node = p_Tree.GetNodeAt(cursorPosition);
            if (node == null || !(node.Tag is ProjectFile)) { return; }
            if (FileOpened != null) {
                FileOpened(this, (ProjectFile)node.Tag);
            }
        };

        /*Make sure that left/right click causes the appropriate node
          to be selected*/
        p_Tree.MouseDown += delegate(object s, MouseEventArgs e) {
            TreeNode selected = p_Tree.GetNodeAt(e.X, e.Y);
            if (selected == null) { return; }
            p_Tree.SelectedNode = selected;
        };

        /*When a folder is expanded, change the icon to an expanded
         folde*/
        p_Tree.AfterCollapse += delegate(object sender, TreeViewEventArgs e) {
            if (!(e.Node.Tag is ProjectDirectory)) { return; }
            e.Node.ImageKey = e.Node.SelectedImageKey = "folder";
        };
        p_Tree.AfterExpand += delegate(object sender, TreeViewEventArgs e) {
            if (!(e.Node.Tag is ProjectDirectory)) { return; }
            e.Node.ImageKey = e.Node.SelectedImageKey = "folderOpen";
        };
        #endregion

        #region Tree right click
        ContextMenuStrip rightClick = new ContextMenuStrip();
        rightClick.Opening += delegate(object s, System.ComponentModel.CancelEventArgs e) {
            presentRightClickMenu(rightClick);
            e.Cancel = rightClick.Items.Count == 0;
        };
        p_Tree.ContextMenuStrip = rightClick;
        #endregion
    }

    public Solution Solution {
        get { return p_Solution; }
        set {
            p_Solution = value;

            //clear out the tree
            p_Tree.Nodes.Clear();

            //null?
            if (value == null) {
                if (SolutionChanged != null) {
                    SolutionChanged(this, null);
                }
                return;
            }

            //create the solution node
            int projCount = value.Projects.Length;
            TreeNode node = p_Tree.Nodes.Add(
                "Solution '" + value.SolutionName + "' " +
                "(" + projCount + " project" + (projCount != 1 ? "s" : "") + ")");
            /*Yes, we copy the VS2008 Solution Explorer style title.*/

            //setup the node
            node.ImageKey = "solution";
            node.SelectedImageKey = "solution";
            node.Tag = value;
            IndexSolution(node);
            node.Expand();

            //fire the solution changed event
            if (SolutionChanged != null) {
                SolutionChanged(this, p_Solution);
            }
        }
    }

    public void IndexSolution(TreeNode node) {
        //clear out all current nodes
        node.Nodes.Clear();

        //index every project
        Solution solution = (Solution)node.Tag;
        Project[] projects = solution.Projects;
        for (int c = 0; c < projects.Length; c++) {
            TreeNode projectNode = node.Nodes.Add(projects[c].ProjectName);
            projectNode.ImageKey = "proj";
            projectNode.SelectedImageKey = "proj";
            projectNode.Tag = projects[c];
            IndexProject(projectNode);
            
        }
    }
    public void IndexProject(TreeNode node) { 
        //index the project's root directory
        Project proj = (Project)node.Tag;
        IndexDirectory(proj.Root, node);
    }
    public void IndexDirectory(ProjectDirectory dir, TreeNode node) {
        if (node.Tag == null) {
            node.Tag = dir;
        }
        node.Nodes.Clear();

        //get sub directory and files
        ProjectDirectory[] dirs = dir.GetDirectories();
        ProjectFile[] files = dir.GetFiles();

        //recursively invoke this function for every sub-directory
        for (int c = 0; c < dirs.Length; c++) { 
            //add the tree node for the sub directory
            TreeNode n = node.Nodes.Add(dirs[c].Name);
            n.ImageKey = "folder";
            n.SelectedImageKey = "folder";

            //index the child directory
            IndexDirectory(dirs[c], n);
        }

        for (int c = 0; c < files.Length; c++) { 
            //get the type for the image key to apply 
            //to this file.
            string type = files[c].FileType.ToString();

            //add the node
            TreeNode n = node.Nodes.Add(files[c].Name);
            n.ImageKey = type;
            n.SelectedImageKey = type;
            n.Tag = files[c];
        }
    }

    public Solution GetSelectedSolution() {
        //project selected?
        Project selectedProject = GetSelectedProject();
        if (selectedProject != null) { return selectedProject.Solution; }

        //is the selected node a solution?
        TreeNode selectedNode = p_Tree.SelectedNode;
        if (selectedNode == null) { return null; }
        if (!(selectedNode.Tag is Solution)) { return null; }
        return (Solution)selectedNode.Tag;
    }
    public Project GetSelectedProject() {
        //entity selected?
        ProjectEntity selectedEntity = GetSelectedProjectEntity();
        if (selectedEntity != null) { return selectedEntity.Project; }

        //is the selected node a project?
        TreeNode selectedNode = p_Tree.SelectedNode;
        if (selectedNode == null) { return null; }
        if (!(selectedNode.Tag is Project)) { return null; }

        return (Project)selectedNode.Tag;
    }
    public ProjectEntity GetSelectedProjectEntity() {
        TreeNode selected = p_Tree.SelectedNode;
        if (selected == null) { return null; }

        if (!(selected.Tag is ProjectEntity)) { return null; }
        return (ProjectEntity)selected.Tag;
    }

    public bool RefreshAll() {
        if (Solution == null) { return false; }
        object st = PushTreeState();
        Solution = Solution;
        PopTreeState(st);
        return true;
    }
    public bool RefreshProjectList() { 
        //is there a solution?
        if (Solution == null) { return false; }

        //trigger a re-index of the solution
        object st = PushTreeState();
        IndexSolution(findNodeByTag(Solution));
        PopTreeState(st);
        return true;
    }

    public void ExpandSolution() {
        if (Solution == null) { return; }
        TreeNode node = findNodeByTag(Solution);
        if (node == null) { return; }
        node.Expand();
    }
    public void ExpandProject(Project project) { ExpandDirectory(project.Root); }
    public void ExpandDirectory(ProjectDirectory directory) {
        TreeNode node = findNodeByTag(directory);
        if (node == null) { return; }
        node.Expand();
    }

    public object PushTreeState() { 
        //build up a list of all tree nodes
        //that are expanded. we do this
        object[] expanded = new object[0];
        enumNodes(delegate(TreeNode n) {
            if (!n.IsExpanded) { return true; }
            Array.Resize(ref expanded, expanded.Length + 1);
            expanded[expanded.Length - 1] = n.Tag;
            return true;
        });
        return expanded;
    }
    public void PopTreeState(object state) {
        object[] expand = (object[])state;

        //expand all tree nodes which contains
        //a tag in the expand list
        //and collapse those that don't match.
        enumNodes(delegate(TreeNode n) {
            bool e = false;
            object tag = n.Tag;
            for (int c = 0; c < expand.Length; c++) {
                if (expand[c] == tag) {
                    e = true;
                    break;
                }
            }

            //expand/collapse
            if (e) { n.Expand(); }
            else { n.Collapse(); }
            return true;
        });
    }

    #region Drag/drop
    private void tree_itemDrag(object sender, ItemDragEventArgs e) { 
        //only allow project entities to be dragged
        TreeNode node = (TreeNode)e.Item;
        if (!(node.Tag is ProjectEntity)) { return; }

        //start drag/drop
        p_Tree.DoDragDrop(node, DragDropEffects.Move);
    }
    private void tree_dragEnter(object sender, DragEventArgs e) {
        //file drop?
        object fileDropData = e.Data.GetData(DataFormats.FileDrop);

        //node?
        TreeNode node = (TreeNode)e.Data.GetData(typeof(TreeNode));

        //valid?
        if (fileDropData == null && node == null) {
            return;
        }

        //trigger dragdrop
        e.Effect = DragDropEffects.Move;
    }
    private void tree_dragOver(object sender, DragEventArgs e) {
        e.Effect = DragDropEffects.None;

        //get the screen position relative to the treeview
        //so we can get a treenode that the cursor is over.
        Point cursorPosition = Cursor.Position;
        cursorPosition = p_Tree.PointToClient(cursorPosition);

        //get the tree node at the current cursor position and select it
        TreeNode node = p_Tree.GetNodeAt(cursorPosition);
        if (node == null) {
            return; 
        }
        p_Tree.SelectedNode = node;
        
        //the node must be a project directory/project in order
        //to accept a file/directory to move.
        object nodeTag = node.Tag;
        if (nodeTag is Project || nodeTag is ProjectDirectory) {
            e.Effect = DragDropEffects.Move;
        }

    }
    private void tree_dragDrop(object sender, DragEventArgs e) { 
        //valid?
        if (e.Effect == DragDropEffects.None) { return; }

        //get the project directory of which we perform the move operation
        Point cursorPosition = new Point(e.X, e.Y);
        cursorPosition = p_Tree.PointToClient(cursorPosition);
        TreeNode node = p_Tree.GetNodeAt(cursorPosition);
        if (node == null) { node = p_Tree.SelectedNode; }
        if (node == null) { return; }
        ProjectDirectory directory = (node.Tag is Project ?
            ((Project)node.Tag).Root :
            (ProjectDirectory)node.Tag);

        //is it a file drop?
        if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
            string[] entries = (string[])e.Data.GetData(DataFormats.FileDrop);

            //copy every entry over to the project directory
            foreach (string s in entries) {
                copyFileEntry(s, directory);
            }

            //refresh the directory list in the tree
            object st = PushTreeState();
            IndexDirectory(directory, node);
            PopTreeState(st);
            node.Expand();
        }
    }

    private void copyFileEntry(string path, ProjectDirectory dir) { 
        //is the path a file?
        if (File.Exists(path)) { 
            //create the file entry in the directory
            FileInfo f = new FileInfo(path);
            ProjectFile file = dir.CreateFile(f.Name);

            //copy over the contents of the file
            //over to the one in the project.
            File.Copy(f.FullName, file.PhysicalLocation, true);
            return;
        }

        //must be a directory, otherwise the path is invalid
        if (!Directory.Exists(path)) { return; }

        //create the directory instance in the project
        dir = dir.CreateDirectory(new DirectoryInfo(path).Name);

        //enumerate over all the child directories in the directory
        foreach (DirectoryInfo d in new DirectoryInfo(path).GetDirectories()) { 
            //copy all of the sub-files in this directory 
            //by recursively invoking this function
            copyFileEntry(d.FullName, dir);
        }

        //copy all files in the directory
        foreach (FileInfo f in new DirectoryInfo(path).GetFiles())  {
            //create the file entry in the directory
            ProjectFile file = dir.CreateFile(f.Name);

            //copy over the contents of the file
            //over to the one in the project.
            File.Copy(f.FullName, file.PhysicalLocation, true);
        }
    }
    #endregion

    private delegate bool nodeEnumCallback(TreeNode node);
    private bool enumNodes(nodeEnumCallback c) {
        foreach (TreeNode n in p_Tree.Nodes) {
            c(n);
            if (!enumNodes(n, c)) { return false; }
        }
        return true;
    }
    private bool enumNodes(TreeNode node, nodeEnumCallback c) {
        c(node);
        foreach (TreeNode n in node.Nodes) {
            if (!c(n)) { return false; }
            if (!enumNodes(n, c)) { return false; }
        }
        return true;
    }

    private void triggerRemoveDirectory(ProjectDirectory dir) {
        if (FileDelete == null) { return; }

        dir.Enumerate(true, delegate(ProjectEntity e) {
            if (e is ProjectFile) {
                FileDelete(this, (ProjectFile)e);
            }
            return true;
        });
    }

    private TreeNode findNodeByTag(object tag) {
        foreach (TreeNode n in p_Tree.Nodes) {
            TreeNode res = findNodeByTag(n, tag);
            if (res != null) { return res; }
        }
        return null;
    }
    private TreeNode findNodeByTag(TreeNode search, object tag) {
        if (search.Tag == tag) { return search; }
        foreach (TreeNode n in search.Nodes) {
            if (n.Tag == tag) { return n; }
            TreeNode res = findNodeByTag(n, tag);
            if (res != null) { return res; }
        }
        return null;
    }

    private TreeNode getNodeByPath(string path) {
        path = path.ToLower();
        TreeNode found = null;
        enumNodes(delegate(TreeNode n) {
            if (getNodePath(n).ToLower() == path) {
                found = n;
                return false;
            }
            return true;
        });
        return found;
    }
    private string getNodePath(TreeNode node) {
        string[] buffer = new string[] { node.Text };
        TreeNode current = node.Parent;
        while (current != null) {
            Array.Resize(ref buffer, buffer.Length + 1);
            buffer[buffer.Length - 1] = current.Text;
            current = current.Parent;
        }
        Array.Reverse(buffer);
        return Helpers.Flatten(buffer, "/");
    }

    public void SaveTreeExpandState(string objectName) { 
        //get the list of nodes that are expanded
        string[] expanded = new string[0];
        enumNodes(delegate(TreeNode n) {
            if (!n.IsExpanded) { return true; }
            Array.Resize(ref expanded, expanded.Length + 1);
            expanded[expanded.Length - 1] = getNodePath(n);
            return true;
        });

        //write the list to the object
        byte[] data = Helpers.StringArrayToBytes(expanded, false);
        RuntimeState.SetObjectData(objectName, data);
    }
    public void LoadTreeExpandState(string objectName) {
        //object exist?
        if (!RuntimeState.ObjectExists(objectName)) {
            return;
        }

        p_Tree.CollapseAll();

        //get the list of expanded nodes that we should expand
        byte[] data = RuntimeState.GetObjectData(objectName);
        string[] list = Helpers.StringArrayFromBytes(data, false);

        //expand all nodes that match those in the list
        foreach (string s in list) {
            TreeNode node = getNodeByPath(s);
            if (node == null) { continue; }
            node.Expand();
        }
    }

    private void presentRightClickMenu(ContextMenuStrip menu) {
        //clear all items currently in the menu
        menu.Items.Clear();

        //is anything selected?
        if (p_Tree.SelectedNode == null) { return; }

        //position the menu where the mouse is
        menu.SetBounds(Cursor.Position.X, Cursor.Position.Y, menu.Width, menu.Height);
        menu.Hide(); menu.Show();

        //get the selected tag
        object tag = p_Tree.SelectedNode.Tag;

        /*Solution entry*/
        if (tag is Solution) {
            return;
        }


        /*ProjectEntity/Project items*/
        if (tag is Project || tag is ProjectDirectory) {
            ToolStripMenuItem add = (ToolStripMenuItem)menu.Items.Add("Add", null);

            add.DropDownItems.Add(getRightClickItem_Add_NewItem());
            add.DropDownItems.Add(getRightClickItem_Add_ExistingItem());
            add.DropDownItems.Add(getRightClickItem_Add_NewFolder());

            menu.Items.Add(new ToolStripSeparator());
        }

        #region Clipboard
        ToolStripItem cut = menu.Items.Add("Cut", Icons.GetBitmap("menu.cut", 16), null);
        ToolStripItem copy = menu.Items.Add("Copy", Icons.GetBitmap("menu.copy", 16), null);
        ToolStripItem paste = menu.Items.Add("Paste", Icons.GetBitmap("menu.paste", 16), null);
        menu.Items.Add(getRightClickItem_Delete());
        menu.Items.Add(new ToolStripSeparator());
        #endregion
    }

    public delegate void SolutionBrowserEventHandler<T>(SolutionBrowserControl solution, T args);
    public event SolutionBrowserEventHandler<ProjectFile> FileDelete;
    public event SolutionBrowserEventHandler<ProjectFile> FileOpened;

    public event SolutionBrowserEventHandler<Solution> SolutionChanged;

    #region Right click menu items
    private ToolStripMenuItem getRightClickItem_Delete() {
        return new ToolStripMenuItem("Delete", Icons.GetBitmap("menu.delete", 16), delegate(object sender, EventArgs e) { 
            //get the currently selected item
            object selectedObj = p_Tree.SelectedNode.Tag;

            #region Delete project
            if (selectedObj is Project) { 
                Project project = (Project)selectedObj;

                //prompt if the user wants to delete the project
                if (MessageBox.Show(
                        "Are you sure you want to delete the project \"" + project.ProjectName + "\"?",
                        "Are you sure?",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question) != DialogResult.Yes) { return; }
                
                //prompt the user if they want to delete the physical project as well.
                DialogResult result = MessageBox.Show(
                        "Do you want to delete the physical project files?\nWarning! This CANNOT be undone!",
                        "Question",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);
                if (result == DialogResult.Cancel) { return; }
                bool deletePhysical = result == DialogResult.Yes;

                //delete the physical files if requested
                if (deletePhysical) {
                    string dir = new FileInfo(project.Filename).Directory.FullName;
                    if (Directory.Exists(dir)) {
                        Directory.Delete(dir, true);
                    }
                }

                //trigger remove event of the entire project directory
                triggerRemoveDirectory(project.Root);

                //delete the project from the solution
                project.Solution.RemoveProject(project);

                //remove the project from the tree
                p_Tree.SelectedNode.Remove();
                return;
            }
            #endregion

            //get the entity that needs to be deleted
            ProjectEntity entity = (ProjectEntity)selectedObj;

            //prompt the user if they want to delete the entity
            string promptMessage = entity is ProjectDirectory ?
                "Are you sure you want to delete the directory \"" + entity.FullName + "\" and all of it's contents?\nThis cannot be undone." :
                "Are you sure you want to perminently delete the file \"" + entity.FullName + "\"?";
            if (MessageBox.Show(promptMessage, "Are you sure?", 
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) {
                return;
            }

            //trigger the events for deletion
            if (entity is ProjectFile && FileDelete != null) { FileDelete(this, (ProjectFile)entity); }
            if (entity is ProjectDirectory) { triggerRemoveDirectory((ProjectDirectory)entity); }

            //delete the entry
            ProjectDirectory parent = entity.Parent;
            entity.Delete();

            //update the tree
            TreeNode node = findNodeByTag((parent.IsRoot ? (object)parent.Project : (object)parent));
            object st = PushTreeState();
            IndexDirectory(parent, node);
            PopTreeState(st);
        });
    }

    private ToolStripMenuItem getRightClickItem_Add_NewFolder() {
        return new ToolStripMenuItem("New folder", Icons.GetBitmap("menu.newfolder", 16), delegate(object s, EventArgs e) { 
            //get the currently selected folder
            ProjectEntity folder = GetSelectedProjectEntity();
            if (folder == null) { folder = GetSelectedProject().Root; }
            ProjectDirectory dir = (ProjectDirectory)folder;

            //define what the new folder name would be
            //(use a counter for every time we hit one that already
            //is called newfolderx
            int folderCounter = 1;
            string folderName = "New folder";
            while (dir.DirectoryExists(folderName)) {
                folderName = "New folder (" + folderCounter + ")";
                folderCounter++;
            }

            //create the directory
            ProjectDirectory newDir = dir.CreateDirectory(folderName);

            //update the tree
            TreeNode node = findNodeByTag(dir.IsRoot ? (object)dir.Project : (object)dir);
            object st = PushTreeState();
            IndexDirectory(dir, node);
            PopTreeState(st);
            node.Expand();
        });
    }
    private ToolStripMenuItem getRightClickItem_Add_ExistingItem() {
        return new ToolStripMenuItem("Existing item", Icons.GetBitmap("menu.existingitem", 16), delegate(object sender, EventArgs e) { 
            //prompt for a filename
            OpenFileDialog opn = new OpenFileDialog { 
                Multiselect = true
            };
            if (opn.ShowDialog() != DialogResult.OK) { return; }

            //get the directory in which to add everything to
            TreeNode node = p_Tree.SelectedNode;
            ProjectDirectory dir = (node.Tag is Project ? ((Project)node.Tag).Root : (ProjectDirectory)node.Tag);

            //copy all the files that were selected into the project directory
            foreach (string s in opn.FileNames) {
                FileInfo f = new FileInfo(s);

                //create the file entry
                ProjectFile entry = dir.CreateFile(f.Name);
                File.Copy(f.FullName, entry.PhysicalLocation, true);
            }

            //refresh 
            object st = PushTreeState();
            IndexDirectory(dir, node);
            PopTreeState(st);
            node.Expand();
        });
    }
    private ToolStripMenuItem getRightClickItem_Add_NewItem() {
        return new ToolStripMenuItem("New item", Icons.GetBitmap("menu.newitem", 16), delegate(object sender, EventArgs e) { 
            //get the currently selected directory
            ProjectEntity folder = GetSelectedProjectEntity();
            if (folder == null) { folder = GetSelectedProject().Root; }
            ProjectDirectory dir = (ProjectDirectory)folder;

            #region prompt the user for a filename
            string filename = null;
            while (true) {
                filename = AddProjectItem.Show();
                if (filename == null) { break; }

                //does the file already exist?
                if (dir.EntityExists(filename)) {
                        MessageBox.Show("The name \"" + filename + "\" is already in use in directory \"" + dir.FullName + "\"",
                                        "Error",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                        continue;
                }
                break;
            }
            if (filename == null) { return; }
            #endregion

            //create the file entry
            ProjectFile file = dir.CreateFile(filename);

            //update the tree
            TreeNode node = findNodeByTag(dir.IsRoot ? (object)dir.Project : (object)dir);
            object st = PushTreeState();
            IndexDirectory(dir, node);
            PopTreeState(st);
            node.Expand();
        });      
    }
    #endregion
}