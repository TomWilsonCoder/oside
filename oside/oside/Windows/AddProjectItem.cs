using System;
using System.Drawing;
using System.Windows.Forms;

public static class AddProjectItem {
    public static string Show() { 
        //define the image list to contain all the file type icons
        ImageList imageList = new ImageList { 
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(32, 32)
        };
        imageList.Images.Add("asm", Icons.GetBitmap("filetype.asm", 32));
        imageList.Images.Add("c", Icons.GetBitmap("filetype.c", 32));
        imageList.Images.Add("cpp", Icons.GetBitmap("filetype.cpp", 32));
        imageList.Images.Add("h", Icons.GetBitmap("filetype.h", 32));
        imageList.Images.Add("cs", Icons.GetBitmap("filetype.cs", 32));
        imageList.Images.Add("vb", Icons.GetBitmap("filetype.vb", 32));
        imageList.Images.Add("unknown", Icons.GetBitmap("filetype.unknown", 32));
        

        //get the screen size so we can set the form
        //to fill 75% of the screen.
        Size screenSize = Screen.FromPoint(Cursor.Position).WorkingArea.Size;

        //create the form instance
        Form form = new Form() { 
            Text = "Add solution item",
            ShowIcon = false,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false,
            MinimizeBox = false,
            Size = new Size(600, 400)
                    //(int)(screenSize.Width * 0.75),
                    //(int)(screenSize.Height * 0.75)),
        };

        //create a panel that would contain everything so
        //we can easily get the actual working size we have
        //for the window
        Panel workingArea = new Panel() { Dock = DockStyle.Fill };
        form.Controls.Add(workingArea);

        //define the "add"/"cancel" buttons first so we can deturmine how
        //big everything would be to match it
        Button addButton = new Button() { Text = "Add" };
        Button cancelButton = new Button() { Text = "Cancel" };

        //deturmine the height of the input region
        int padding = 5;
        int inputRegionHeight = addButton.Height + (padding * 2);

        #region create the the list
        ListView list = new ListView() { 
            Location = Point.Empty, 
            Size = new Size(workingArea.Width, workingArea.Height - inputRegionHeight),
            LargeImageList = imageList,
            SmallImageList = imageList,
            View = View.LargeIcon,
        };
        workingArea.Controls.Add(list);
        addType(list, "asm", "c", "cpp", "h", "cs", "vb", "unknown");
        list.Focus();
        #endregion

        #region add the buttons
        Button[] buttons = { addButton, cancelButton };
        
        //add the buttons and calculate there position to be
        //displayed on the right side
        int currentButtonX = 0;
        for (int c = 0; c < buttons.Length; c++) {
            currentButtonX += buttons[c].Width + padding;

            int x = workingArea.Width - currentButtonX;
            int y =
                    (workingArea.Height - inputRegionHeight) +
                    (inputRegionHeight / 2) - (buttons[c].Height / 2);

            buttons[c].Location = new Point(x, y);
            workingArea.Controls.Add(buttons[c]);
        }
        
        #endregion

        #region Filename label
        //define the label
        Font filenameFont = new Font("Arial", 10, FontStyle.Bold);
        Label filenameLabel = new Label() { 
            Text = "Filename:",
            Font = filenameFont
        };

        //define the location of the label to be at the beginning and centered
        //vertically.
        int filenameX = padding;
        int filenameY = (workingArea.Height - inputRegionHeight) +
                        (inputRegionHeight / 2) - (((int)filenameFont.GetHeight() + 1) / 2);
        filenameLabel.Location = new Point(filenameX, filenameY);
        workingArea.Controls.Add(filenameLabel);

        //calculate the end position x position of the label
        //so we know were to position the text box
        int filenameEndX = 75;//filenameLabel.Width + (padding * 2);
        #endregion

        #region Filename text box
        TextBox filenameTextBox = new TextBox() { 
            BorderStyle = BorderStyle.FixedSingle,
            Text = "New file"
        };

        //position the text box in the middle of the input region
        int txtX = filenameEndX;
        int txtY = (workingArea.Height - inputRegionHeight) +
                   (inputRegionHeight / 2) - (filenameTextBox.Height / 2);
        filenameTextBox.Location = new Point(txtX, txtY);

        //stretch the width of the text box
        filenameTextBox.Width =
            workingArea.Width - currentButtonX - filenameEndX - padding;

        //add the text box
        workingArea.Controls.Add(filenameTextBox);
        filenameTextBox.BringToFront();
        #endregion

        bool cancelled = true;

        /*Define the events for the buttons and text box*/
        cancelButton.Click += delegate(object sender, EventArgs e) { form.Close(); };
        addButton.Click += delegate(object sender, EventArgs e) { validate(filenameTextBox, form, ref cancelled); };
        list.KeyDown += delegate(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Escape) { form.Close(); }
        };
        filenameTextBox.KeyDown += delegate(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) { validate(filenameTextBox, form, ref cancelled); }
            if (e.KeyCode == Keys.Escape) { form.Close(); }
        };

        /*Define the selection change event for the list so that 
          when the user selects an item, it automatically adds
          the extension in filename*/
        list.ItemSelectionChanged += delegate(object sender, ListViewItemSelectionChangedEventArgs e) {
            if (e.Item == null) { return; }

            //define the new extesion
            string newExt = (string)e.Item.Tag;

            //add the extension to the filename
            string[] fileSplit = filenameTextBox.Text.Split('.');
            if (fileSplit.Length == 1) {
                Array.Resize(ref fileSplit, fileSplit.Length + 1);
            }
            fileSplit[fileSplit.Length - 1] = newExt;

            //new extension is blank?
            if (newExt == "") {
                Array.Resize(ref fileSplit, fileSplit.Length - 1);
            }

            //set the new filename
            filenameTextBox.Text = Helpers.Flatten(fileSplit, ".");
        };

        /*Define tab indexes*/
        filenameTextBox.TabIndex = 0;
        cancelButton.TabIndex = 1;
        addButton.TabIndex = 2;
        list.TabIndex = 3;

        //show the form and await user interaction.
        form.ShowDialog();

        //return what the filename was if the user didnt cancel
        //the dialog.
        return (cancelled ? null : filenameTextBox.Text);
    }

    private static void validate(TextBox input, Form form, ref bool cancel) { 
        //valid filename?
        if (!Helpers.ValidFilename(input.Text)) {
            MessageBox.Show("The name \"" + input.Text + "\" is an invalid filename",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
            return;
        }

        //close
        cancel = false;
        form.Close();
    }
    private static void addType(ListView list, params string[] type) {
        foreach (string s in type) {
            addType(s, list);
        }
    }
    private static void addType(string type, ListView list) {
        ListViewItem lsti = new ListViewItem();
        lsti.ImageKey = type;

        //deturmine the name of the file from the type
        string name = "Other";
        switch (type.ToLower()) {
            case "asm": name = "Assembly"; break;
            case "c": name = "C"; break;
            case "cpp": name = "C++"; break;
            case "h": name = "Header"; break;
            case "cs": name = "C#"; break;
            case "vb": name = "VB"; break;
        }
        lsti.Text = "Blank " + name + " file";
        lsti.Tag = (name == "Other" ? "" : type);
        list.Items.Add(lsti);
    }
}