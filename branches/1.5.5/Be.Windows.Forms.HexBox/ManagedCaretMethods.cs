using System;
using System.Collections.Generic;

using System.Text;
using System.Drawing;
using System.Windows.Forms;

//Class Author: Federico Blaseotto

namespace Be.Windows.Forms
{
    /// <summary>
    /// Managed (minimalistic) implementation of Caret for cross-platform porting
    /// </summary>
    public static class ManagedCaretMethods
    {
        private static Caret caret;

        public static void CreateCaret(Control parent, int nWidth, int nHeight)
        {
            caret = new Caret(parent, nWidth, nHeight);
            caret.Show();
        }

        public static void ShowCaret(Control parent)
        {
            if (caret != null)
                caret.Show();
        }

        public static void DestroyCaret()
        {
            if (caret != null)
            {
                caret.Destroy();
                caret = null;
            }
        }

        public static void SetCaretPos(int X, int Y)
        {
            if (caret != null)
                caret.SetPosition(X, Y);
        }

        // Key definitions
        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_CHAR = 0x102;
    }
}
