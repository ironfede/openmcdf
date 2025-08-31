namespace StructuredStorageExplorer;

partial class AddEntryDialog
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
        tableLayoutPanel1 = new TableLayoutPanel();
        labelName = new Label();
        nameTextBox = new TextBox();
        flowLayoutPanel1 = new FlowLayoutPanel();
        buttonOK = new Button();
        buttonCancel = new Button();
        tableLayoutPanel1.SuspendLayout();
        flowLayoutPanel1.SuspendLayout();
        SuspendLayout();
        // 
        // tableLayoutPanel1
        // 
        tableLayoutPanel1.AutoSize = true;
        tableLayoutPanel1.ColumnCount = 2;
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
        tableLayoutPanel1.Controls.Add(labelName, 0, 0);
        tableLayoutPanel1.Controls.Add(nameTextBox, 1, 0);
        tableLayoutPanel1.Dock = DockStyle.Fill;
        tableLayoutPanel1.Location = new Point(0, 0);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        tableLayoutPanel1.RowCount = 1;
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayoutPanel1.Size = new Size(451, 29);
        tableLayoutPanel1.TabIndex = 0;
        // 
        // labelName
        // 
        labelName.AutoSize = true;
        labelName.Location = new Point(3, 0);
        labelName.Name = "labelName";
        labelName.Size = new Size(39, 15);
        labelName.TabIndex = 0;
        labelName.Text = "Name";
        // 
        // nameTextBox
        // 
        nameTextBox.Location = new Point(48, 3);
        nameTextBox.MaxLength = 64;
        nameTextBox.Name = "nameTextBox";
        nameTextBox.Size = new Size(397, 23);
        nameTextBox.TabIndex = 1;
        // 
        // flowLayoutPanel1
        // 
        flowLayoutPanel1.AutoSize = true;
        flowLayoutPanel1.Controls.Add(buttonOK);
        flowLayoutPanel1.Controls.Add(buttonCancel);
        flowLayoutPanel1.Dock = DockStyle.Bottom;
        flowLayoutPanel1.FlowDirection = FlowDirection.RightToLeft;
        flowLayoutPanel1.Location = new Point(0, 29);
        flowLayoutPanel1.Name = "flowLayoutPanel1";
        flowLayoutPanel1.Size = new Size(451, 31);
        flowLayoutPanel1.TabIndex = 1;
        flowLayoutPanel1.WrapContents = false;
        // 
        // buttonOK
        // 
        buttonOK.AutoSize = true;
        buttonOK.DialogResult = DialogResult.OK;
        buttonOK.Location = new Point(373, 3);
        buttonOK.Name = "buttonOK";
        buttonOK.Size = new Size(75, 25);
        buttonOK.TabIndex = 0;
        buttonOK.Text = "OK";
        buttonOK.UseVisualStyleBackColor = true;
        // 
        // buttonCancel
        // 
        buttonCancel.AutoSize = true;
        buttonCancel.DialogResult = DialogResult.Cancel;
        buttonCancel.Location = new Point(292, 3);
        buttonCancel.Name = "buttonCancel";
        buttonCancel.Size = new Size(75, 25);
        buttonCancel.TabIndex = 1;
        buttonCancel.Text = "Cancel";
        buttonCancel.UseVisualStyleBackColor = true;
        // 
        // AddEntryDialog
        // 
        AcceptButton = buttonOK;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        CancelButton = buttonCancel;
        ClientSize = new Size(451, 60);
        Controls.Add(tableLayoutPanel1);
        Controls.Add(flowLayoutPanel1);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AddEntryDialog";
        Text = "Add stream/storage";
        tableLayoutPanel1.ResumeLayout(false);
        tableLayoutPanel1.PerformLayout();
        flowLayoutPanel1.ResumeLayout(false);
        flowLayoutPanel1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel tableLayoutPanel1;
    private Label labelName;
    private TextBox nameTextBox;
    private FlowLayoutPanel flowLayoutPanel1;
    private Button buttonOK;
    private Button buttonCancel;
}