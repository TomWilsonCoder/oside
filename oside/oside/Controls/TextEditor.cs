using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

using Alsing;
using Alsing.SourceCode;
using Alsing.Windows.Forms;

public class TextEditor : Control {
    private SyntaxBoxControl p_Base;
    private DateTime p_Modifed;
    private SyntaxDefinition p_SyntaxDefinition;

    private static SyntaxDefinition[] p_SyntaxDefinitions = new SyntaxDefinition[0];
    private static string[] p_SyntaxNames = new string[0];
    private static string[] p_SyntaxDefinitionFilenames = new string[0];

    public TextEditor(string filename) {
        BackColor = Color.FromArgb(255, 34, 40, 42);

        //find the syntax definition
        p_SyntaxDefinition = null;
        string syntaxName = new FileInfo(filename).Extension.Replace(".", "").ToLower();
        for (int c = 0; c < p_SyntaxNames.Length; c++) {
            if (p_SyntaxNames[c] == syntaxName) {
                p_SyntaxDefinition = p_SyntaxDefinitions[c];
                break;
            }
        }

        //default to a unknown syntax definition if the file extension
        //isn't known.
        if (p_SyntaxDefinition == null) {
            for (int c = 0; c < p_SyntaxNames.Length; c++) {
                if (p_SyntaxNames[c] == "unknown") {
                    p_SyntaxDefinition = p_SyntaxDefinitions[c];
                    break;
                }
            }
        }

        //create the syntaxbox control
        p_Base = new SyntaxBoxControl() {
            Dock = DockStyle.Fill,
            BackColor = BackColor,
            BracketBackColor = Color.Transparent,
            BracketForeColor = Color.White,
            BracketBorderColor = Color.FromArgb(255, 60, 70, 70),

            ShowLineNumbers = false,
            ShowGutterMargin = false,

            SplitView = false,

            FontName = "Consolas",
            FontSize = 10
        };
        p_Base.Document = new SyntaxDocument();
        p_Base.Document.Parser.Init(p_SyntaxDefinition);
        p_Base.Document.Text = File.ReadAllText(filename);
       
        p_Base.Document.Change += delegate(object sender, EventArgs e) {
            if (Modified == null) { return; }
            Modified(this, e);
        };

        //create the right click menu
        ContextMenuStrip rightClickMenu = new ContextMenuStrip();
        rightClickMenu.Opening += delegate(object sender, System.ComponentModel.CancelEventArgs e) {
            presentRightClickMenu(rightClickMenu);
        };
        ContextMenuStrip = rightClickMenu;

        //add the control
        Controls.Add(p_Base);
    }

    public static void Initialize() {
        //load every syntax definition in the /syntax folder
        foreach (FileInfo f in new DirectoryInfo("./syntax").GetFiles()) {
            if (f.Extension.ToLower() != ".syn") { continue; }

            Array.Resize(ref p_SyntaxDefinitions, p_SyntaxDefinitions.Length + 1);
            Array.Resize(ref p_SyntaxNames, p_SyntaxNames.Length + 1);
            Array.Resize(ref p_SyntaxDefinitionFilenames, p_SyntaxDefinitionFilenames.Length + 1);

            p_SyntaxDefinitions[p_SyntaxDefinitions.Length - 1] =
                SyntaxDefinition.FromSyntaxFile(f.FullName);
            
            p_SyntaxNames[p_SyntaxNames.Length - 1] =
                f.Name.Replace(f.Extension, "").ToLower();

            p_SyntaxDefinitionFilenames[p_SyntaxDefinitionFilenames.Length - 1] =
                f.FullName;

            //apply the style set 
            applyStyle(
                p_SyntaxDefinitions[p_SyntaxDefinitions.Length - 1].Styles,
                Color.FromArgb(255, 34, 40, 42),
                Color.White
            );
        }
        
        //trigger initialization of the syntax highlighter core
        new SyntaxBoxControl().Dispose();
    }

    public DateTime ModifiedTime {
        get { return p_Modifed; }
        set { p_Modifed = value; }
    }
    public string Contents { get { return p_Base.Document.Text; } }

    public bool ReadOnly {
        get { return p_Base.ReadOnly; }
        set {
            p_Base.ReadOnly = value;
        }
    }

    public int LineNumber {
        get { return p_Base.Caret.CurrentRow.Index + 1; }
    }
    public int LineCount {
        get { return p_Base.Document.Lines.Length; }
    }
    public int ColumnNumber {
        get {
            return p_Base.Caret.Position.X + 1;
        }
    }

    public void Print(string name) {
        SourceCodePrintDocument printDocument = getPrintDocument(name);
        PrintDialog print = new PrintDialog() { 
            Document = printDocument
        };
        if (print.ShowDialog() == DialogResult.OK) {
            printDocument.Print();
        }
    }
    public void PrintPreview(string name) {
        SourceCodePrintDocument printDocument = getPrintDocument(name);
        PrintPreviewDialog printPreview = new PrintPreviewDialog() { 
            Document = printDocument,
            ShowIcon = false
        };
        printPreview.ShowDialog();
    }

    private int getAbsolutePosition(int line, out string[] lines) {
        //get the absolute position of the line in the file
        lines = p_Base.Document.Text.Split('\n');
        int buffer = 0;
        for (int c = 0; c < line; c++) {
            buffer += lines[c].Length + 1;
        }
        return buffer;
    }
    public void SelectLine(int line) {
        string[] lines;
        int absolutePosition = getAbsolutePosition(line, out lines);
        
        //select the line
        p_Base.Selection.ClearSelection();
        p_Base.Selection.SelStart = absolutePosition;
        p_Base.Selection.SelLength = lines[line].Length;
        p_Base.Refresh();
        p_Base.ScrollIntoView(line);
    }
    public void SelectWordAtColumn(int line, int column) {
        //select the whole line?
        if (column == 0) {
            SelectLine(line);
            return;
        }

        //get the line absolute position
        string[] lines;
        int absolutePosition = getAbsolutePosition(line, out lines);

        //find the word where the column fits
        string[] words = lines[line].Split(' ');
        int x = 0;
        for (int c = 0; c < words.Length; c++) {
            x += words[c].Length + 1;
            if (column < x) {

                //select the word
                p_Base.Selection.SelStart = absolutePosition + x - (words[c].Length + 1);
                p_Base.Selection.SelLength = words[c].Length;
                p_Base.Refresh();
                p_Base.ScrollIntoView(line);
                break;
            }
        }
    }

    public event EventHandler Modified;

    public void Undo() { p_Base.Undo(); }
    public void Redo() { p_Base.Redo(); }
    public void Cut() { p_Base.Cut(); }
    public void Copy() { p_Base.Copy(); }
    public void Paste() { p_Base.Paste(); }
    
    public bool ToggleReadOnly() {
        p_Base.ReadOnly = !p_Base.ReadOnly;
        return p_Base.ReadOnly;
    }

    private void presentRightClickMenu(ContextMenuStrip menu) {
        //clear the menu
        menu.Items.Clear();

        /*Clipboard*/
        menu.Items.Add("Cut", Icons.GetBitmap("menu.cut", 16), menu_cut);
        menu.Items.Add("Copy", Icons.GetBitmap("menu.copy", 16), menu_copy);
        menu.Items.Add("Paste", Icons.GetBitmap("menu.paste", 16), menu_paste);
        menu.Items.Add(new ToolStripSeparator());

        /*Collapse menu*/
        menu.Items.Add(new ToolStripMenuItem("Outlining", null, new ToolStripItem[] { 
            new ToolStripMenuItem("Toggle folding", null, menu_collapse_foldToggle),
            new ToolStripMenuItem("Collapse to definitions", null, menu_collapse_foldAll)
        }));
    }

    #region Right click menu

    private void menu_cut(object sender, EventArgs e) {
        p_Base.Cut();
    }
    private void menu_copy(object sender, EventArgs e) {
        p_Base.Copy();
    }
    private void menu_paste(object sender, EventArgs e) {
        p_Base.Paste();
    }

    private void menu_collapse_foldToggle(object sender, EventArgs e) {
        p_Base.Document.Folding = !p_Base.Document.Folding;
    }
    private void menu_collapse_foldAll(object sender, EventArgs e) {
        p_Base.Document.Folding = true;
        p_Base.Document.FoldAll();
    }


    #endregion

    private SourceCodePrintDocument getPrintDocument(string name) { 
        //get the filename for the syntax definition so we can
        //reload it and apply our own styleset for printing.
        int index = Array.IndexOf(p_SyntaxDefinitions, p_SyntaxDefinition);
        string filename = p_SyntaxDefinitionFilenames[index];

        //load the syntax defintion
        SyntaxDefinition definition = SyntaxDefinition.FromSyntaxFile(filename);
        applyStyle(definition.Styles, Color.White, Color.Black);

        //clone the current document under the new syntax definitions
        SyntaxDocument document = new SyntaxDocument();
        document.Parser.Init(definition);
        document.Text = Contents;

        //return a printable document
        return new SourceCodePrintDocument() {
            Document = document,
            DocumentName = name          
        };

    }
    private static void applyStyle(TextStyle[] styles, Color backgroundColor, Color foreColor) { 
        for (int c = 0; c < styles.Length; c++) {
            TextStyle style = styles[c];
            style.BackColor = backgroundColor;

            //strip out the code name prefix from the style name
            string name = style.Name;
            //if (name == null) { style.ForeColor = Color.White; continue; }
            name = name.Substring(name.IndexOf(' ') + 1);

            switch (name.ToLower()) {
                case "code":
                case "operator":
                case "scope": 
                case "text":
                    style.ForeColor = foreColor; 
                    break;

                case "comment": style.ForeColor = Color.FromArgb(255, 110, 120, 120); break;
                case "number": style.ForeColor = Color.FromArgb(255, 255, 210, 30); break;
                case "block": style.ForeColor = Color.FromArgb(255, 100, 140, 180); break;

                case "keyword3":
                case "region": 
                    style.ForeColor = Color.FromArgb(255, 160, 130, 190); 
                    break;
                
                case "opcodes":
                case "keyword":
                case "keyword2":
                case "keywords":
                case "datatype":
                    style.ForeColor = Color.FromArgb(255, 150, 200, 100); 
                    break;
                
                case "string":
                case "xml string":
                    style.ForeColor = Color.FromArgb(255, 240, 120, 20);
                    break;
            }
        }
    }
}
