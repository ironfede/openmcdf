// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 3673 $</version>
// </file>

// Changed by Federico Blaseotto 
// 28/11/2011 
// Added managed methods for caret manipulation
// to achieve cross-platform portability

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Be.Windows.Forms
{
    public class Caret : System.IDisposable
    {
        private const int CARET_BLINK_INTERVAL = 450;

        System.Windows.Forms.Timer timer
            = new System.Windows.Forms.Timer { Interval = CARET_BLINK_INTERVAL };

        bool _disposed = false;
        bool blink = false;
        bool visible = false;

        int width;
        int height;

        private int x, y;

        Control parent;
        Point currentPos = new Point(-1, -1);

        public Caret(Control parent, int width, int height)
        {

            this.parent = parent;

            parent.GotFocus += new EventHandler(GotFocus);
            parent.LostFocus += new EventHandler(LostFocus);
            parent.Paint += new PaintEventHandler(Paint);

            this.width = width;
            this.height = height;

            timer.Tick += new EventHandler(CaretTick);
            timer.Start();

            parent.Invalidate(true);

            //if (Environment.OSVersion.Platform == PlatformID.Unix)
            //    caretImplementation = new ManagedCaret(this);
            //else
            //    caretImplementation = new Win32Caret(this);
        }

        void Paint(object sender, PaintEventArgs e)
        {
            if (parent != null && blink && visible)
                PaintCaret(e.Graphics);
        }

        public void Hide()
        {
            visible = false;
        }

        public void Show()
        {
            visible = true;
        }

        void CaretTick(object sender, EventArgs e)
        {
            blink = !blink;
            parent.Invalidate(true);
        }

        public bool SetPosition(int x, int y)
        {
            this.x = x - 1;
            this.y = y;

            return true;
        }

        public void PaintCaret(Graphics g)
        {
            g.DrawRectangle(Pens.Blue, x, y, width, height);
        }

        public void Destroy()
        {
            visible = false;
            timer.Enabled = false;

            Dispose();
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    timer.Dispose();
                    parent.GotFocus -= new EventHandler(GotFocus);
                    parent.LostFocus -= new EventHandler(LostFocus);
                    parent = null;
                }

                _disposed = true;
            }

        }

        //public TextLocation ValidatePosition(TextLocation pos)
        //{
        //    //int line = Math.Max(0, Math.Min(textArea.Document.TotalNumberOfLines - 1, pos.Y));
        //    //int column = Math.Max(0, pos.X);

        //    //if (column == int.MaxValue || !textArea.TextEditorProperties.AllowCaretBeyondEOL)
        //    //{
        //    //    LineSegment lineSegment = textArea.Document.GetLineSegment(line);
        //    //    column = Math.Min(column, lineSegment.Length);
        //    //}
        //    //return new TextLocation(column, line);
        //    return pos;
        //}

        /// <remarks>
        /// If the caret position is outside the document text bounds
        /// it is set to the correct position by calling ValidateCaretPos.
        /// </remarks>
        public void ValidateCaretPos()
        {
            //line = Math.Max(0, Math.Min(textArea.Document.TotalNumberOfLines - 1, line));
            //column = Math.Max(0, column);

            //if (column == int.MaxValue || !textArea.TextEditorProperties.AllowCaretBeyondEOL)
            //{
            //    LineSegment lineSegment = textArea.Document.GetLineSegment(line);
            //    column = Math.Min(column, lineSegment.Length);
            //}
        }

        void CreateCaret()
        {
            //while (!caretCreated)
            //{
            //    switch (caretMode)
            //    {
            //        //case CaretMode.InsertMode:
            //        //    caretCreated = caretImplementation.Create(2, textArea.TextView.FontHeight);
            //        //    break;
            //        case CaretMode.OverwriteMode:

            //            break;
            //    }
            //}

            //caretCreated = caretImplementation.Create(4, 8);

            //if (currentPos.X < 0)
            //{
            //    ValidateCaretPos();
            //    currentPos = ScreenPosition;
            //}

            //caretImplementation.SetPosition(currentPos.X, currentPos.Y);
            //caretImplementation.Show();
        }



        void GotFocus(object sender, EventArgs e)
        {
            //Log("GotFocus, IsInUpdate=" + textArea.IsInUpdate);
            //hidden = false;
            //if (!textArea.MotherTextEditorControl.IsInUpdate)
            //{
            //CreateCaret();
            //UpdateCaretPosition();
            visible = true;
            //}
        }

        void LostFocus(object sender, EventArgs e)
        {

            //hidden = true;
            visible = false;
            //Dispose();
        }

        void PaintCaretLine(Graphics g)
        {
            //if (!textArea.Document.TextEditorProperties.CaretLine)
            //    return;

            Color caretLineColor = Color.Blue;

            g.DrawLine(new Pen((caretLineColor)),
                       currentPos.X,
                       0,
                       currentPos.X,
                       parent.DisplayRectangle.Height);
        }

    }
}
