
partial class Input
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
        this.inputTxt = new System.Windows.Forms.TextBox();
        this.inputLbl = new System.Windows.Forms.Label();
        this.okBtn = new System.Windows.Forms.Button();
        this.cancelBtn = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // inputTxt
        // 
        this.inputTxt.Location = new System.Drawing.Point(15, 25);
        this.inputTxt.Name = "inputTxt";
        this.inputTxt.Size = new System.Drawing.Size(257, 20);
        this.inputTxt.TabIndex = 0;
        this.inputTxt.KeyDown += new System.Windows.Forms.KeyEventHandler(this.inputTxt_KeyDown);
        // 
        // inputLbl
        // 
        this.inputLbl.AutoSize = true;
        this.inputLbl.Location = new System.Drawing.Point(12, 9);
        this.inputLbl.Name = "inputLbl";
        this.inputLbl.Size = new System.Drawing.Size(26, 13);
        this.inputLbl.TabIndex = 1;
        this.inputLbl.Text = "{txt}";
        // 
        // okBtn
        // 
        this.okBtn.Location = new System.Drawing.Point(197, 51);
        this.okBtn.Name = "okBtn";
        this.okBtn.Size = new System.Drawing.Size(75, 23);
        this.okBtn.TabIndex = 2;
        this.okBtn.Text = "Ok";
        this.okBtn.UseVisualStyleBackColor = true;
        this.okBtn.Click += new System.EventHandler(this.okBtn_Click);
        // 
        // cancelBtn
        // 
        this.cancelBtn.Location = new System.Drawing.Point(116, 51);
        this.cancelBtn.Name = "cancelBtn";
        this.cancelBtn.Size = new System.Drawing.Size(75, 23);
        this.cancelBtn.TabIndex = 1;
        this.cancelBtn.Text = "Cancel";
        this.cancelBtn.UseVisualStyleBackColor = true;
        this.cancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
        // 
        // Input
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(284, 83);
        this.Controls.Add(this.cancelBtn);
        this.Controls.Add(this.okBtn);
        this.Controls.Add(this.inputTxt);
        this.Controls.Add(this.inputLbl);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "Input";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "{text}";
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox inputTxt;
    private System.Windows.Forms.Label inputLbl;
    private System.Windows.Forms.Button okBtn;
    private System.Windows.Forms.Button cancelBtn;
}