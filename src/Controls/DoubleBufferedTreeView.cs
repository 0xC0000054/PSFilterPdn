/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using TerraFX.Interop.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace PSFilterPdn.Controls
{
    // Adapted from https://www.codeproject.com/Articles/37253/Double-buffered-Tree-and-Listviews

    internal sealed class DoubleBufferedTreeView : TreeView
    {
        private Bitmap? collapseGlyph;
        private Bitmap? disabledGlyph;
        private Bitmap? expandGlyph;
        private SolidBrush backgroundBrush;
        private Color disabledGlyphBackColor;
        private Color previousForeColor;
        private string? disabledGlyphResourceName;
        private string? previousDisabledGlyphResourceName;

        private static readonly KeyValuePair<int, int>[] IconSizesToDpi = new KeyValuePair<int, int>[]
        {
            new KeyValuePair<int, int>(16, 96),
            new KeyValuePair<int, int>(20, 120),
            new KeyValuePair<int, int>(24, 144),
            new KeyValuePair<int, int>(32, 192),
            new KeyValuePair<int, int>(64, 384)
        };

        public DoubleBufferedTreeView()
        {
            // Enable default double buffering processing (DoubleBuffered returns true)
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            base.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            backgroundBrush = new SolidBrush(BackColor);
            previousForeColor = ForeColor;
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
        public new event DrawTreeNodeEventHandler? DrawNode;
#pragma warning restore 0067

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                backgroundBrush?.Dispose();

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
            if (disabledGlyph != null)
            {
                DrawDisbledGlyph();
            }
        }

        protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
        {
            if (e.Node != null)
            {
                e.Cancel = !NodeEnabled(e.Node);
            }

            base.OnBeforeExpand(e);
        }

        protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
        {
            if (e.Node != null)
            {
                e.Cancel = !NodeEnabled(e.Node);
            }

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

            if (e.Node is null)
            {
                return;
            }

            Rectangle bounds = e.Node.Bounds;

            if (bounds.IsEmpty || e.Node.TreeView.Nodes.Count == 0)
            {
                return;
            }

            bool enabled = NodeEnabled(e.Node);

            // Draw the expand / collapse glyphs

            int textLeft = bounds.Left + 1;

            if (e.Node.Parent == null)
            {
                int maxDimension = Math.Min(collapseGlyph!.Height, bounds.Height);

                if (maxDimension != bounds.Height)
                {
                    // Add additional padding when the glyph is smaller than the item height.
                    textLeft += bounds.Height - collapseGlyph.Height;
                }

                const int imageX = 0;
                int imageY = bounds.Y + (bounds.Height - maxDimension) / 2;
                if ((bounds.Height % 2) != 0)
                {
                    imageY++;
                }

                Rectangle cropRect = new(imageX, imageY, maxDimension, maxDimension);

                if (enabled)
                {
                    if (e.Node.IsExpanded)
                    {
                        e.Graphics.DrawImageUnscaledAndClipped(collapseGlyph, cropRect);
                    }
                    else
                    {
                        e.Graphics.DrawImageUnscaledAndClipped(expandGlyph!, cropRect);
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

                    e.Graphics.DrawImageUnscaledAndClipped(disabledGlyph!, cropRect);
                }
            }

            // Draw the node text

            Font nodeFont = e.Node.NodeFont ?? e.Node.TreeView.Font;
            Size textSize = TextRenderer.MeasureText(e.Graphics, e.Node.Text, nodeFont);
            Rectangle textBounds = new(textLeft, bounds.Top, textSize.Width, textSize.Height);

            if (enabled)
            {
                if ((e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected)
                {
                    e.Graphics.FillRectangle(SystemBrushes.Highlight, textBounds);

                    TextRenderer.DrawText(e.Graphics, e.Node.Text, nodeFont, Rectangle.Inflate(textBounds, 2, 2), ForeColor);
                }
                else
                {
                    e.Graphics.FillRectangle(backgroundBrush, textBounds);

                    TextRenderer.DrawText(e.Graphics, e.Node.Text, nodeFont, textBounds, ForeColor);
                }
            }
            else
            {
                e.Graphics.FillRectangle(backgroundBrush, textBounds);

                TextRenderer.DrawText(e.Graphics, e.Node.Text, nodeFont, textBounds, SystemColors.GrayText);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            Windows.SendMessageW(
                (HWND)Handle,
                TVM.TVM_SETEXTENDEDSTYLE,
                TVS.TVS_EX_DOUBLEBUFFER,
                TVS.TVS_EX_DOUBLEBUFFER);
            InitTreeNodeGlyphs();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM.WM_THEMECHANGED)
            {
                InitTreeNodeGlyphs();
            }
        }

        private void InitTreeNodeGlyphs()
        {
            collapseGlyph?.Dispose();
            expandGlyph?.Dispose();

            string collapseResourceName;
            string expandResourceName;

            if (VisualStyleRenderer.IsSupported)
            {
                collapseResourceName = GetBestResourceForItemHeight("Resources.Icons.VistaThemedCollapse-{0}.png");
                expandResourceName = GetBestResourceForItemHeight("Resources.Icons.VistaThemedExpand-{0}.png");
            }
            else
            {
                collapseResourceName = GetBestResourceForItemHeight("Resources.Icons.UnthemedCollapse-{0}.png");
                expandResourceName = GetBestResourceForItemHeight("Resources.Icons.UnthemedExpand-{0}.png");
            }

            collapseGlyph = new Bitmap(typeof(PSFilterPdnEffect), collapseResourceName);
            expandGlyph = new Bitmap(typeof(PSFilterPdnEffect), expandResourceName);
            disabledGlyphResourceName = expandResourceName;

            if (disabledGlyph != null)
            {
                DrawDisbledGlyph();
            }
        }

        private void DrawDisbledGlyph()
        {
            if (expandGlyph != null)
            {
                if (disabledGlyph == null ||
                    disabledGlyph.Size != expandGlyph.Size ||
                    disabledGlyphBackColor != BackColor ||
                    !string.Equals(previousDisabledGlyphResourceName, disabledGlyphResourceName, StringComparison.Ordinal))
                {
                    disabledGlyph?.Dispose();

                    disabledGlyph = new Bitmap(expandGlyph.Width, expandGlyph.Height, PixelFormat.Format32bppArgb);
                    disabledGlyphBackColor = BackColor;
                    previousDisabledGlyphResourceName = disabledGlyphResourceName;

                    using (Graphics graphics = Graphics.FromImage(disabledGlyph))
                    {
                        ControlPaint.DrawImageDisabled(graphics, expandGlyph, 0, 0, disabledGlyphBackColor);
                    }
                }
            }
        }

        private string GetBestResourceForItemHeight(string resourceFormat)
        {
            int bestDpi = 0;

            int itemHeight = ItemHeight;

            foreach (KeyValuePair<int, int> iconSize in IconSizesToDpi)
            {
                if (iconSize.Key <= itemHeight)
                {
                    bestDpi = iconSize.Value;
                }
                else
                {
                    break;
                }
            }

            if (bestDpi == 0)
            {
                bestDpi = 384;
            }

            return string.Format(CultureInfo.InvariantCulture, resourceFormat, bestDpi);
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
