﻿using OpenMcdf;

namespace StructuredStorageExplorer;

static class Utils
{
    public static DialogResult InputBox(string title, string promptText, ref string value)
    {
        using Form form = new();
        using Label label = new();
        using TextBox textBox = new();
        using Button buttonOK = new();
        using Button buttonCancel = new();

        form.Text = title;
        label.Text = promptText;
        textBox.Text = value;

        buttonOK.Text = "OK";
        buttonCancel.Text = "Cancel";
        buttonOK.DialogResult = DialogResult.OK;
        buttonCancel.DialogResult = DialogResult.Cancel;

        label.SetBounds(9, 20, 372, 13);
        textBox.SetBounds(12, 36, 372, 20);
        buttonOK.SetBounds(228, 72, 75, 23);
        buttonCancel.SetBounds(309, 72, 75, 23);

        label.AutoSize = true;
        textBox.Anchor |= AnchorStyles.Right;
        buttonOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

        form.ClientSize = new Size(396, 107);
        form.Controls.AddRange([label, textBox, buttonOK, buttonCancel]);
        form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.MinimizeBox = false;
        form.MaximizeBox = false;
        form.AcceptButton = buttonOK;
        form.CancelButton = buttonCancel;

        DialogResult dialogResult = form.ShowDialog();
        value = textBox.Text;
        return dialogResult;
    }

    public static EntryInfo WithEscaped(this EntryInfo entry) => entry with { Name = EscapedControl(entry.Name), Path = EscapedControl(entry.Path) };

    public static string WithEscaped(this string entry) => EscapedControl(entry);

    private static string EscapedControl(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new System.Text.StringBuilder();
        foreach (char c in s)
        {
            if (char.IsControl(c))
                sb.Append($"\\u{((int)c):x4}");
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}
