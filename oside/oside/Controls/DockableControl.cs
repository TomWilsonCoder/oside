using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

public class DockableControl : Control {
    private Control p_Parent;
    private Form p_UndockWindow;
    private Panel p_WorkingArea;
    private string p_Title;
    private bool p_IsDocked = true;
    private const int CaptionHeight = 24;

    private Rectangle p_CloseButtonBounds;
    private Rectangle p_DockButtonBounds;


    public DockableControl() {
        Size = new Size(200, 400);
        
        /*initialize the working area*/
        p_WorkingArea = new Panel() { 
            Location = new Point(0, CaptionHeight),
            Size = new Size(
                Width,
                Height - CaptionHeight),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
        };

        Controls.Add(p_WorkingArea);
        p_WorkingArea.BringToFront();
    }

    public Panel WorkingArea { get { return p_WorkingArea; } }

    public string Title {
        get { return p_Title; }
        set {
            p_Title = value;
            triggerRender();
        }
    }

    private void drawCaption(Graphics screen) { 
        Rectangle captionBounds = new Rectangle(0, 0, Width, CaptionHeight);

        /*Initialize the GDI buffer*/
        if (Width == 0 || Height == 0) { return; }
        Bitmap bitmapBuffer = new Bitmap(Width, Height);
        Graphics buffer = Graphics.FromImage(bitmapBuffer);
        buffer.SmoothingMode = SmoothingMode.HighQuality;
        buffer.InterpolationMode = InterpolationMode.HighQualityBicubic;
        buffer.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        //define the actual region the caption has which with an 
        //applied min-width
        Rectangle captionWorkingBounds = captionBounds;
        if (captionWorkingBounds.Width < 150) { captionWorkingBounds.Width = 150; }

        #region Draw the caption background
        buffer.FillRectangle(
                new LinearGradientBrush(
                    new Point(0, 0),
                    new Point(0, CaptionHeight),
                    Color.FromArgb(255, 230, 230, 230),
                    Color.FromArgb(255, 150, 150, 150)),
                captionBounds);
        #endregion

        #region Draw border
        //define the border color
        Color borderColor = Color.FromArgb(255, 130, 135, 144);
        Pen borderPen = new Pen(borderColor);

        //draw the border around the control
        buffer.DrawRectangle(
            new Pen(borderColor),
            new Rectangle(
                0, 0,
                Width - 1,
                Height - 1));
        #endregion

        #region Draw close icon
        //get the bitmap
        Size iconSize = new Size(13, 13);
        Bitmap closeBitmap = Icons.GetBitmap("icons.close", iconSize.Width);

        //define where the close button is being rendered
        Rectangle closeBitmapBounds = new Rectangle(
            captionWorkingBounds.Width - iconSize.Width - 5,
            (captionWorkingBounds.Height / 2) - (iconSize.Height / 2),
            iconSize.Width,
            iconSize.Height);
        p_CloseButtonBounds = closeBitmapBounds;

        //is the mouse currently over the close button?
        Point cursorPosition = Cursor.Position;
        cursorPosition = PointToClient(cursorPosition);
        if (Helpers.PointCollide(closeBitmapBounds, cursorPosition)) {
            Helpers.ChangeBitmapLight(ref closeBitmap, 80);
        }


        //draw it.
        buffer.DrawImage(closeBitmap, closeBitmapBounds);
        #endregion

        #region Draw dock/undock icon
        //get the bitmap
        Bitmap dockBitmap = Icons.GetBitmap("icons." + (p_IsDocked ? "undock" : "dock"), iconSize.Width);

        //define the bounds where the icon is
        Rectangle dockBitmapBounds = new Rectangle(
            closeBitmapBounds.X - iconSize.Width - 5,
            closeBitmapBounds.Y,
            iconSize.Width,
            iconSize.Height);
        p_DockButtonBounds = dockBitmapBounds;

        //is the cursor over the bitmap?
        if (Helpers.PointCollide(dockBitmapBounds, cursorPosition)) {
            Helpers.ChangeBitmapLight(ref dockBitmap, 80);
        }

        //draw the icon
        buffer.DrawImage(dockBitmap, dockBitmapBounds);

        #endregion

        #region Draw title
        Font titleFont = new Font("Arial", 10);
        buffer.DrawString(
            p_Title,
            titleFont,
            new SolidBrush(Color.Black),
            new Point(
                5,
                (CaptionHeight / 2) - (titleFont.Height / 2)));
        #endregion

        //draw the gdi buffer
        screen.DrawImage(bitmapBuffer, Point.Empty);

        //clean up
        buffer.Dispose();
        bitmapBuffer.Dispose();
    }

    protected override void OnPaintBackground(PaintEventArgs e) {
        drawCaption(e.Graphics);
    }
    protected override void OnMouseMove(MouseEventArgs e) {
        triggerRender();
        base.OnMouseMove(e);
    }
    protected override void OnClick(EventArgs e) {
        base.OnClick(e);

        //get the relative location of the cursor
        Point cursorPosition = Cursor.Position;
        cursorPosition = PointToClient(cursorPosition);

        //close clicked?
        if (Helpers.PointCollide(p_CloseButtonBounds, cursorPosition)) { 
            //close the undocked window (if undocked)
            if (!p_IsDocked) {
                p_UndockWindow.Close();
                p_UndockWindow.Dispose();
            }
            
            //remove this control from the parent
            if (p_Parent == null) { p_Parent = Parent; }
            p_Parent.Controls.Remove(this);

            //trigger close
            if (CloseClicked != null) {
                CloseClicked(this, null);
            }
        }

        //dock button clicked?
        if (Helpers.PointCollide(p_DockButtonBounds, cursorPosition)) {
            if (p_IsDocked) { UndockControl(); }
            else { DockControl(); }
        }
    }
    protected override void OnResize(EventArgs e) {
        triggerRender();
        base.OnResize(e);
    }

    private void triggerRender() {
        drawCaption(CreateGraphics());
    }

    public void DockControl() { 
        //already docked?
        if (p_IsDocked) { return; }
        p_IsDocked = true;

        //add this control to it's original parent
        p_UndockWindow.Controls.Remove(this);
        p_Parent.Controls.Add(this);

        //close the undock form
        p_UndockWindow.Close();
        p_UndockWindow.Dispose();

        //fire the dock event
        if (DockClicked != null) {
            DockClicked(this, null);
        }
    }
    public void UndockControl() { 
        //already undocked?
        if (!p_IsDocked) { return; }
        p_IsDocked = false;

        //create a new form to host the dockable window
        p_UndockWindow = new Form() { 
            ClientSize = Size,
            ControlBox = false,
            Location = PointToScreen(Location),
            StartPosition = FormStartPosition.Manual,
            Text = " ",
            FormBorderStyle = FormBorderStyle.SizableToolWindow
        };
        p_UndockWindow.SizeChanged += delegate(object sender, EventArgs e) {
            triggerRender();
        };

        //add this control to it
        p_Parent = Parent;
        Parent.Controls.Remove(this);
        p_UndockWindow.Controls.Add(this);

        //show the window
        p_UndockWindow.Show();

        //trigger the undock event
        if (UndockClicked != null) {
            UndockClicked(this, null);
        }
    }

    public event EventHandler CloseClicked;
    public event EventHandler DockClicked;
    public event EventHandler UndockClicked;
}