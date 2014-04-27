using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Threading;

public partial class Splash : Form {

    private bool p_Ready = false;
    public void Ready() { p_Ready = true; }

    public delegate void BeginInitializeCallback(object splash);
    public Splash(BeginInitializeCallback initCallback) {
        InitializeComponent();

        //incriment the build number
        #if !releaseBuild
        Init.CurrentBuild = Convert.ToInt32(File.ReadAllText("build.txt")) + 1;
        File.WriteAllText("build.txt", Init.CurrentBuild.ToString());
        buildLbl.Text = "Build: " + Init.CurrentBuild;
        #else 
        buildLbl.Text = "Build: xxx";
        #endif


        //center the labels
        inReleaseLbl.Location = new Point(
            (Width / 2) - (inReleaseLbl.Width / 2),
            (Height / 2) - (inReleaseLbl.Height / 2));
        buildLbl.Location = new Point(
            (Width / 2) - (buildLbl.Width / 2),
            Height - buildLbl.Height - 10);

        //generate the background image
        Bitmap background = new Bitmap(Width, Height);
        Graphics buffer = Graphics.FromImage(background);
        buffer.SmoothingMode = SmoothingMode.HighQuality;
        buffer.InterpolationMode = InterpolationMode.HighQualityBicubic;
        buffer.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        buffer.FillRectangle(
                new LinearGradientBrush(
                        Point.Empty,
                        new Point(0, Height),
                        Color.FromArgb(255, 70, 180, 255),
                        Color.FromArgb(255, 30, 110, 160)),
                new Rectangle(
                        Point.Empty,
                        Size));

        int strokeWidth = 5;
        buffer.DrawRectangle(
                new Pen(
                    new LinearGradientBrush(
                        Point.Empty,
                        new Point(0, Height),
                        Color.FromArgb(255, 12, 80, 125),
                        Color.FromArgb(255, 0, 63, 105)
                    ),
                    strokeWidth),
                new Rectangle(
                    Point.Empty,
                    new Size(Width - (strokeWidth / 2), Height - (strokeWidth / 2))));

        //remove the labels and redraw them using GDI since the label
        //messes with the caption dragging trick we use to make the
        //window movable.
        Control.ControlCollection controls = Controls;
        for (int c = controls.Count - 1; c != -1; c--) {
            if (!(controls[c] is Label)) { continue; }
            buffer.DrawString(
                    controls[c].Text,
                    controls[c].Font,
                    new SolidBrush(Color.White),
                    controls[c].Location);
            Controls.Remove(controls[c]);
        }

        BackgroundImage = background;
        BackgroundImageLayout = ImageLayout.Stretch;


        //perform a fancy fade in.
        Opacity = 0;
        float vel = 0.04f;
        int speed = 17;
        fadeIn(vel, speed, delegate { 
            //begin initialization
            new Thread(new ParameterizedThreadStart(initCallback)) { 
                ApartmentState = ApartmentState.STA
            }.Start(this);

            //wait for initialization to finish
            wait(delegate {
                //fade and close
                fadeOut(vel, speed, delegate {
                    Close();
                });
            });
        });
    }

    private delegate void completeCallback();
    private void fadeIn(float vel, int speed, completeCallback callback) {
        System.Windows.Forms.Timer tmr = new System.Windows.Forms.Timer();
        tmr.Interval = speed;
        tmr.Start();
        tmr.Tick += delegate(object sender, EventArgs e) {
            Opacity += vel;
            if (Opacity >= 1) {
                tmr.Stop();
                callback();
                return;
            }
        };
    }
    private void fadeOut(float vel, int speed, completeCallback callback) {
        System.Windows.Forms.Timer tmr = new System.Windows.Forms.Timer();
        tmr.Interval = speed;
        tmr.Start();
        tmr.Tick += delegate(object sender, EventArgs e) {
            Opacity -= vel;
            if (Opacity <= 0) {
                tmr.Stop();
                callback();
                return;
            }
        };
    }
    private void wait(completeCallback callback) {
        /*Just waits until the Ready flag is set*/
        System.Windows.Forms.Timer tmr = new System.Windows.Forms.Timer();
        tmr.Interval = 100;
        tmr.Tick += delegate(object sender, EventArgs e) {
            if (p_Ready) {
                tmr.Stop();
                callback();
            }
        };
        tmr.Start();
    }

    protected override void WndProc(ref Message m) {
        if (m.Msg == 0x84) {
            base.WndProc(ref m);
            if (m.Result.ToInt32() == 0x1) {
                m.Result = (IntPtr)0x2;
            }
            return;
        }
        base.WndProc(ref m);
    }
}