using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

public partial class Input : Form {
    private Input() {
        InitializeComponent();

        KeyDown += inputTxt_KeyDown;
        okBtn.KeyDown += inputTxt_KeyDown;
        cancelBtn.KeyDown += inputTxt_KeyDown;
    }

    private void okBtn_Click(object sender, EventArgs e) {
        DialogResult = DialogResult.OK;
        Close();
    }
    private void cancelBtn_Click(object sender, EventArgs e) {
        DialogResult = DialogResult.Cancel;
        Close();
    }
    private void inputTxt_KeyDown(object sender, KeyEventArgs e) {
        if (e.KeyCode == Keys.Escape) { cancelBtn_Click(sender, null); }
        if (e.KeyCode == Keys.Enter) {
            okBtn_Click(sender, null);
        }
    }

    public static string Show(string title, string label, string defaultValue) {
        Input inp = new Input() { 
            Text = title
        };
        inp.inputLbl.Text = label + ":";
        inp.inputTxt.Text = defaultValue == null ? "" : defaultValue;
        return inp.ShowDialog() == DialogResult.OK ?
            inp.inputTxt.Text :
            null;
    }
}