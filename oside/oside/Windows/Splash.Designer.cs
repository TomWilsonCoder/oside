
partial class Splash
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
        this.inReleaseLbl = new System.Windows.Forms.Label();
        this.buildLbl = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // inReleaseLbl
        // 
        this.inReleaseLbl.AutoSize = true;
        this.inReleaseLbl.BackColor = System.Drawing.Color.Transparent;
        this.inReleaseLbl.Font = new System.Drawing.Font("Calibri", 21.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.inReleaseLbl.ForeColor = System.Drawing.Color.White;
        this.inReleaseLbl.Location = new System.Drawing.Point(12, 9);
        this.inReleaseLbl.Name = "inReleaseLbl";
        this.inReleaseLbl.Size = new System.Drawing.Size(266, 36);
        this.inReleaseLbl.TabIndex = 0;
        this.inReleaseLbl.Text = "For internal use only";
        // 
        // buildLbl
        // 
        this.buildLbl.AutoSize = true;
        this.buildLbl.BackColor = System.Drawing.Color.Transparent;
        this.buildLbl.Font = new System.Drawing.Font("Calibri", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.buildLbl.ForeColor = System.Drawing.Color.White;
        this.buildLbl.Location = new System.Drawing.Point(12, 60);
        this.buildLbl.Name = "buildLbl";
        this.buildLbl.Size = new System.Drawing.Size(34, 15);
        this.buildLbl.TabIndex = 1;
        this.buildLbl.Text = "Build";
        // 
        // Splash
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(404, 105);
        this.Controls.Add(this.buildLbl);
        this.Controls.Add(this.inReleaseLbl);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
        this.Name = "Splash";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.TopMost = true;
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label inReleaseLbl;
    private System.Windows.Forms.Label buildLbl;
}