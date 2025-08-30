namespace StructuredStorageExplorer
{
    partial class PreferencesForm
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
            cbEnableValidation = new CheckBox();
            groupBox1 = new GroupBox();
            btnSavePreferences = new Button();
            btnCancelPreferences = new Button();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // cbEnableValidation
            // 
            cbEnableValidation.AutoSize = true;
            cbEnableValidation.Location = new Point(7, 31);
            cbEnableValidation.Margin = new Padding(3, 4, 3, 4);
            cbEnableValidation.Name = "cbEnableValidation";
            cbEnableValidation.Size = new Size(304, 29);
            cbEnableValidation.TabIndex = 0;
            cbEnableValidation.Text = "File Validation Exceptions enabled";
            cbEnableValidation.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cbEnableValidation);
            groupBox1.Location = new Point(13, 15);
            groupBox1.Margin = new Padding(3, 4, 3, 4);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(3, 4, 3, 4);
            groupBox1.Size = new Size(617, 324);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "Preferences";
            // 
            // btnSavePreferences
            // 
            btnSavePreferences.Location = new Point(526, 372);
            btnSavePreferences.Margin = new Padding(3, 4, 3, 4);
            btnSavePreferences.Name = "btnSavePreferences";
            btnSavePreferences.Size = new Size(104, 38);
            btnSavePreferences.TabIndex = 2;
            btnSavePreferences.Text = "OK";
            btnSavePreferences.UseVisualStyleBackColor = true;
            btnSavePreferences.Click += BtnSavePreferences_Click;
            // 
            // btnCancelPreferences
            // 
            btnCancelPreferences.Location = new Point(414, 372);
            btnCancelPreferences.Margin = new Padding(3, 4, 3, 4);
            btnCancelPreferences.Name = "btnCancelPreferences";
            btnCancelPreferences.Size = new Size(104, 38);
            btnCancelPreferences.TabIndex = 3;
            btnCancelPreferences.Text = "Cancel";
            btnCancelPreferences.UseVisualStyleBackColor = true;
            btnCancelPreferences.Click += BtnCancelPreferences_Click;
            // 
            // PreferencesForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(643, 425);
            Controls.Add(btnCancelPreferences);
            Controls.Add(btnSavePreferences);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(3, 4, 3, 4);
            Name = "PreferencesForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Preferences";
            Load += PreferencesForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox cbEnableValidation;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnSavePreferences;
        private System.Windows.Forms.Button btnCancelPreferences;
    }
}