using System;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;


public partial class About : Form {
    private About() {
        InitializeComponent();

        //create the background for the left side panel
        Bitmap leftBackground = new Bitmap(leftPnl.Width, leftPnl.Height);
        Graphics leftBackgroundBuffer = Graphics.FromImage(leftBackground);
        leftBackgroundBuffer.SmoothingMode = SmoothingMode.HighQuality;
        leftBackgroundBuffer.InterpolationMode = InterpolationMode.HighQualityBicubic;
        leftBackgroundBuffer.FillRectangle(
            new LinearGradientBrush(
                new Point(0, 0),
                new Point(0, leftBackground.Height),
                Color.FromArgb(255, 50, 80, 190),
                Color.FromArgb(255, 10, 25, 80)),
            new Rectangle(
                Point.Empty,
                leftBackground.Size));
        leftBackgroundBuffer.DrawLine(
            new Pen(Color.Black),
            new Point(leftBackground.Width - 1, 0),
            new Point(leftBackground.Width - 1, leftBackground.Height));
        leftPnl.BackgroundImage = leftBackground;

        //set the text
        titleLbl.Text = "OS Development Studio";
        mainLbl.Text =
                "Version: " + FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().Modules[0].FileName).ProductVersion + "\n" +
                "Build: " + Init.CurrentBuild + "\n\n" +

                "This product is for INTERNAL USE only and not intended in ANY WAY\n" +
                "to be a commercial product. Any distributions made to the public WILL\n" + 
                "result in termination of contract/employment/enrollment and possible\n" + 
                "civil or criminal penalties under the maximum extent of the law.";

        //vertically center the labels
        mainLbl.Location = new Point(
            mainLbl.Location.X,
            (ClientSize.Height / 2) - (mainLbl.Height / 2));
        titleLbl.Location = new Point(
            titleLbl.Location.X,
            mainLbl.Location.Y - titleLbl.Height);
    }

    public static void Show() {
        new About().ShowDialog();
    }

    private void sysInfoBtn_Click(object sender, EventArgs e) {
        Helpers.SpawnProcess("msinfo32.exe", "");
    }

    private void closeBtn_Click(object sender, EventArgs e) {
        Close();
    }
}
