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
            this.cbEnableValidation = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnSavePreferences = new System.Windows.Forms.Button();
            this.btnCancelPreferences = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbEnableValidation
            // 
            this.cbEnableValidation.AutoSize = true;
            this.cbEnableValidation.Location = new System.Drawing.Point(6, 25);
            this.cbEnableValidation.Name = "cbEnableValidation";
            this.cbEnableValidation.Size = new System.Drawing.Size(277, 24);
            this.cbEnableValidation.TabIndex = 0;
            this.cbEnableValidation.Text = "File Validation Exceptions enabled";
            this.cbEnableValidation.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cbEnableValidation);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(555, 259);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Preferences";
            // 
            // btnSavePreferences
            // 
            this.btnSavePreferences.Location = new System.Drawing.Point(473, 298);
            this.btnSavePreferences.Name = "btnSavePreferences";
            this.btnSavePreferences.Size = new System.Drawing.Size(94, 30);
            this.btnSavePreferences.TabIndex = 2;
            this.btnSavePreferences.Text = "OK";
            this.btnSavePreferences.UseVisualStyleBackColor = true;
            this.btnSavePreferences.Click += new System.EventHandler(this.BtnSavePreferences_Click);
            // 
            // btnCancelPreferences
            // 
            this.btnCancelPreferences.Location = new System.Drawing.Point(373, 298);
            this.btnCancelPreferences.Name = "btnCancelPreferences";
            this.btnCancelPreferences.Size = new System.Drawing.Size(94, 30);
            this.btnCancelPreferences.TabIndex = 3;
            this.btnCancelPreferences.Text = "Cancel";
            this.btnCancelPreferences.UseVisualStyleBackColor = true;
            this.btnCancelPreferences.Click += new System.EventHandler(this.BtnCancelPreferences_Click);
            // 
            // PreferencesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(579, 340);
            this.Controls.Add(this.btnCancelPreferences);
            this.Controls.Add(this.btnSavePreferences);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "PreferencesForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Preferences";
            this.Load += new System.EventHandler(this.PreferencesForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox cbEnableValidation;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnSavePreferences;
        private System.Windows.Forms.Button btnCancelPreferences;
    }
}