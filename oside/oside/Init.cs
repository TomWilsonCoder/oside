using System;
using System.IO;
using System.Windows.Forms;

static class Init {
    public static int CurrentBuild = -1;

    [STAThread]
    static void Main() {
        Application.EnableVisualStyles();

        //show the splash screen
        Splash splash = new Splash(delegate(object state) {
            Splash st = (Splash)state;
            /*Load the icons*/
            Icons.Load("FileType.", "./icons/filetypes", true);
            Icons.Load("SolutionExplorer.", "./icons/solutionexplorer", true);
            Icons.Load("Menu.", "./icons/menu", true);
            Icons.Load("Icons.", "./icons/icons", true);
            Icons.Load("Tools.", "./icons/tools", true);

            new MainIDE(st).ShowDialog();
        });
        splash.ShowDialog();
    }
}