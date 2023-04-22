using StructuredStorageExplorer.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace StructuredStorageExplorer
{
    public partial class PreferencesForm : Form
    {
        public PreferencesForm()
        {
            InitializeComponent();
        }

        

        private void btnSavePreferences_Click(object sender, EventArgs e)
        {
            Settings.Default.EnableValidation = cbEnableValidation.Checked;
            Settings.Default.Save();
            this.DialogResult = DialogResult.OK;
            this.Close();

        }

        private void btnCancelPreferences_Click(object sender, EventArgs e)
        {
            cbEnableValidation.Checked = Settings.Default.EnableValidation;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void PreferencesForm_Load(object sender, EventArgs e)
        {
            cbEnableValidation.Checked = Settings.Default.EnableValidation;
        }
    }
}
