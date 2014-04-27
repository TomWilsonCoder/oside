
partial class About
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
        this.leftPnl = new System.Windows.Forms.Panel();
        this.mainPnl = new System.Windows.Forms.Panel();
        this.closeBtn = new System.Windows.Forms.Button();
        this.sysInfoBtn = new System.Windows.Forms.Button();
        this.titleLbl = new System.Windows.Forms.Label();
        this.mainLbl = new System.Windows.Forms.Label();
        this.iconPb = new System.Windows.Forms.PictureBox();
        this.leftPnl.SuspendLayout();
        this.mainPnl.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.iconPb)).BeginInit();
        this.SuspendLayout();
        // 
        // leftPnl
        // 
        this.leftPnl.Controls.Add(this.iconPb);
        this.leftPnl.Dock = System.Windows.Forms.DockStyle.Left;
        this.leftPnl.Location = new System.Drawing.Point(0, 0);
        this.leftPnl.Name = "leftPnl";
        this.leftPnl.Size = new System.Drawing.Size(111, 266);
        this.leftPnl.TabIndex = 0;
        // 
        // mainPnl
        // 
        this.mainPnl.Controls.Add(this.closeBtn);
        this.mainPnl.Controls.Add(this.sysInfoBtn);
        this.mainPnl.Controls.Add(this.titleLbl);
        this.mainPnl.Controls.Add(this.mainLbl);
        this.mainPnl.Dock = System.Windows.Forms.DockStyle.Fill;
        this.mainPnl.Location = new System.Drawing.Point(111, 0);
        this.mainPnl.Name = "mainPnl";
        this.mainPnl.Size = new System.Drawing.Size(394, 266);
        this.mainPnl.TabIndex = 1;
        // 
        // closeBtn
        // 
        this.closeBtn.Location = new System.Drawing.Point(319, 222);
        this.closeBtn.Name = "closeBtn";
        this.closeBtn.Size = new System.Drawing.Size(63, 23);
        this.closeBtn.TabIndex = 3;
        this.closeBtn.Text = "Close";
        this.closeBtn.UseVisualStyleBackColor = true;
        this.closeBtn.Click += new System.EventHandler(this.closeBtn_Click);
        // 
        // sysInfoBtn
        // 
        this.sysInfoBtn.Location = new System.Drawing.Point(197, 222);
        this.sysInfoBtn.Name = "sysInfoBtn";
        this.sysInfoBtn.Size = new System.Drawing.Size(116, 23);
        this.sysInfoBtn.TabIndex = 2;
        this.sysInfoBtn.Text = "System Information";
        this.sysInfoBtn.UseVisualStyleBackColor = true;
        this.sysInfoBtn.Click += new System.EventHandler(this.sysInfoBtn_Click);
        // 
        // titleLbl
        // 
        this.titleLbl.AutoSize = true;
        this.titleLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.titleLbl.Location = new System.Drawing.Point(20, 33);
        this.titleLbl.Name = "titleLbl";
        this.titleLbl.Size = new System.Drawing.Size(56, 29);
        this.titleLbl.TabIndex = 1;
        this.titleLbl.Text = "title";
        // 
        // mainLbl
        // 
        this.mainLbl.AutoSize = true;
        this.mainLbl.Location = new System.Drawing.Point(22, 62);
        this.mainLbl.Name = "mainLbl";
        this.mainLbl.Size = new System.Drawing.Size(18, 13);
        this.mainLbl.TabIndex = 0;
        this.mainLbl.Text = "txt";
        // 
        // iconPb
        // 
        this.iconPb.BackColor = System.Drawing.Color.Transparent;
        this.iconPb.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("iconPb.BackgroundImage")));
        this.iconPb.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        this.iconPb.Location = new System.Drawing.Point(9, 12);
        this.iconPb.Name = "iconPb";
        this.iconPb.Size = new System.Drawing.Size(93, 93);
        this.iconPb.TabIndex = 4;
        this.iconPb.TabStop = false;
        // 
        // About
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(505, 266);
        this.Controls.Add(this.mainPnl);
        this.Controls.Add(this.leftPnl);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "About";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "About";
        this.leftPnl.ResumeLayout(false);
        this.mainPnl.ResumeLayout(false);
        this.mainPnl.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.iconPb)).EndInit();
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Panel leftPnl;
    private System.Windows.Forms.Panel mainPnl;
    private System.Windows.Forms.Label titleLbl;
    private System.Windows.Forms.Label mainLbl;
    private System.Windows.Forms.Button sysInfoBtn;
    private System.Windows.Forms.Button closeBtn;
    private System.Windows.Forms.PictureBox iconPb;
}