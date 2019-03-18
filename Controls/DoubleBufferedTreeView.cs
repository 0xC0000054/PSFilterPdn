/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace PSFilterPdn.Controls
{
    // Adapted from https://www.codeproject.com/Articles/37253/Double-buffered-Tree-and-Listviews

    internal sealed class DoubleBufferedTreeView : TreeView
    {
        private Bitmap collapseGlyph;
        private Bitmap disabledGlyph;
        private Bitmap expandGlyph;
        private SolidBrush backgroundBrush;
        private Color disabledGlyphBackColor;
        private Color previousForeColor;

        public DoubleBufferedTreeView()
        {
            // Enable default double buffering processing (DoubleBuffered returns true)
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // Disable default CommCtrl painting on non-Vista systems
            if (!OS.IsVistaOrLater)
            {
                SetStyle(ControlStyles.UserPaint, true);
            }
            base.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            backgroundBrush = new SolidBrush(BackColor);
            previousForeColor = ForeColor;
            InitTreeNodeGlyphs();
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new bool CheckBoxes => false;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new TreeViewDrawMode DrawMode => TreeViewDrawMode.OwnerDrawAll;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new bool ShowLines => false;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new bool ShowPlusMinus => true;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new bool ShowRootLines => false;

#pragma warning disable 0067
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event DrawTreeNodeEventHandler DrawNode;
#pragma warning restore 0067

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (backgroundBrush != null)
                {
                    backgroundBrush.Dispose();
                    backgroundBrush = null;
                }
                if (collapseGlyph != null)
                {
                    collapseGlyph.Dispose();
                    collapseGlyph = null;
                }
                if (disabledGlyph != null)
                {
                    disabledGlyph.Dispose();
                    disabledGlyph = null;
                }
                if (expandGlyph != null)
                {
                    expandGlyph.Dispose();
                    expandGlyph = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);

            if (backgroundBrush.Color != BackColor)
            {
                backgroundBrush.Dispose();
                backgroundBrush = new SolidBrush(BackColor);
            }
            if (disabledGlyph != null && disabledGlyphBackColor != BackColor)
            {
                DrawDisbledGlyph();
            }
        }

        protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
        {
            e.Cancel = !NodeEnabled(e.Node);

            base.OnBeforeExpand(e);
        }

        protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
        {
            e.Cancel = !NodeEnabled(e.Node);

            base.OnBeforeSelect(e);
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);

            if (previousForeColor != ForeColor)
            {
                previousForeColor = ForeColor;
                InitTreeNodeGlyphs();
            }
        }

        protected override void OnDrawNode(DrawTreeNodeEventArgs e)
        {
            base.OnDrawNode(e);

            Rectangle bounds = e.Node.Bounds;

            if (bounds.IsEmpty || e.Node.TreeView.Nodes.Count == 0)
            {
                return;
            }

            bool enabled = NodeEnabled(e.Node);

            // Draw the expand / collapse glyphs

            if (e.Node.Parent == null)
            {
                const int ImageSize = 16;

                int imageX = bounds.X - ImageSize - 4;
                int imageY = bounds.Y + 2;

                if (enabled)
                {
                    if (e.Node.IsExpanded)
                    {
                        e.Graphics.DrawImage(collapseGlyph, imageX, imageY);
                    }
                    else
                    {
                        e.Graphics.DrawImage(expandGlyph, imageX, imageY);
                    }
                }
                else
                {
                    // A separate image is used because ControlPaint.DrawImageDisabled does not render
                    // the image at the correct x and y offsets.
                    if (disabledGlyph == null)
                    {
                        DrawDisbledGlyph();
                    }

                    e.Graphics.DrawImage(disabledGlyph, imageX, imageY);
                }
            }

            // Draw the node text

            Font nodeFont = e.Node.NodeFont ?? e.Node.TreeView.Font;

            if (enabled)
            {
                if ((e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected)
                {
                    e.Graphics.FillRectangle(SystemBrushes.Highlight, bounds);

                    TextRenderer.DrawText(e.Graphics, e.Node.Text, nodeFont, Rectangle.Inflate(bounds, 2, 2), ForeColor);
                }
                else
                {
                    e.Graphics.FillRectangle(backgroundBrush, bounds);

                    TextRenderer.DrawText(e.Graphics, e.Node.Text, nodeFont, bounds, ForeColor);
                }
            }
            else
            {
                e.Graphics.FillRectangle(backgroundBrush, bounds);

                TextRenderer.DrawText(e.Graphics, e.Node.Text, nodeFont, bounds, SystemColors.GrayText);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (OS.IsVistaOrLater)
            {
                SafeNativeMethods.SendMessage(
                    Handle,
                    NativeConstants.TVM_SETEXTENDEDSTYLE,
                    (IntPtr)NativeConstants.TVS_EX_DOUBLEBUFFER,
                    (IntPtr)NativeConstants.TVS_EX_DOUBLEBUFFER);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (GetStyle(ControlStyles.UserPaint))
            {
                IntPtr hdc = e.Graphics.GetHdc();

                try
                {
                    Message m = new Message
                    {
                        HWnd = Handle,
                        Msg = NativeConstants.WM_PRINTCLIENT,
                        WParam = hdc,
                        LParam = (IntPtr)NativeConstants.PRF_CLIENT
                    };
                    DefWndProc(ref m);
                }
                finally
                {
                    e.Graphics.ReleaseHdc(hdc);
                }
            }

            base.OnPaint(e);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            const int WM_THEMECHANGED = 0x031A;

            if (m.Msg == WM_THEMECHANGED)
            {
                InitTreeNodeGlyphs();
            }
        }

        private void InitTreeNodeGlyphs()
        {
            collapseGlyph?.Dispose();
            expandGlyph?.Dispose();

            if (VisualStyleRenderer.IsSupported)
            {
                if (OS.IsVistaOrLater)
                {
                    if (ForeColor == Color.White)
                    {
                        collapseGlyph = new Bitmap(typeof(PSFilterPdnEffect), "Resources.Icons.VistaThemedCollapseDark.png");
                        expandGlyph = new Bitmap(typeof(PSFilterPdnEffect), "Resources.Icons.VistaThemedExpandDark.png");
                    }
                    else
                    {
                        collapseGlyph = new Bitmap(typeof(PSFilterPdnEffect), "Resources.Icons.VistaThemedCollapse.png");
                        expandGlyph = new Bitmap(typeof(PSFilterPdnEffect), "Resources.Icons.VistaThemedExpand.png");
                    }
                }
                else
                {
                    collapseGlyph = new Bitmap(typeof(PSFilterPdnEffect), "Resources.Icons.XPThemedCollapse.png");
                    expandGlyph = new Bitmap(typeof(PSFilterPdnEffect), "Resources.Icons.XPThemedExpand.png");
                }
            }
            else
            {
                collapseGlyph = new Bitmap(typeof(PSFilterPdnEffect), "Resources.Icons.UnthemedCollapse.png");
                expandGlyph = new Bitmap(typeof(PSFilterPdnEffect), "Resources.Icons.UnthemedExpand.png");
            }
        }

        private void DrawDisbledGlyph()
        {
            if (expandGlyph != null)
            {
                if (disabledGlyph == null || disabledGlyph.Size != expandGlyph.Size || disabledGlyphBackColor != BackColor)
                {
                    disabledGlyph?.Dispose();

                    disabledGlyph = new Bitmap(expandGlyph.Width, expandGlyph.Height, PixelFormat.Format32bppArgb);
                    disabledGlyphBackColor = BackColor;

                    using (Graphics graphics = Graphics.FromImage(disabledGlyph))
                    {
                        ControlPaint.DrawImageDisabled(graphics, expandGlyph, 0, 0, disabledGlyphBackColor);
                    }
                }
            }
        }

        private static bool NodeEnabled(TreeNode node)
        {
            bool enabled = true;

            if (node is TreeNodeEx nodeEx)
            {
                enabled = nodeEx.Enabled;
            }

            return enabled;
        }
    }
}
