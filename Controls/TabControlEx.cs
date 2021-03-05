/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PSFilterPdn.Controls
{
    // Adapted from http://dotnetrix.co.uk/tabcontrol.htm#tip2

    /// <summary>
    /// A TabControl that supports custom background and foreground colors
    /// </summary>
    internal sealed class TabControlEx : TabControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components;
        private Color backColor;
        private Color foreColor;
        private Color borderColor;
        private Color hotTrackColor;
        private int hotTabIndex;
        private SolidBrush backgroundBrush;
        private Pen borderPen;
        private SolidBrush hotTrackBrush;

        private static readonly Color DefaultBorderColor = SystemColors.ControlDark;
        private static readonly Color DefaultHotTrackColor = Color.FromArgb(128, SystemColors.HotTrack);

        /// <summary>
        /// Initializes a new instance of the <see cref="TabControlEx"/> class.
        /// </summary>
        public TabControlEx()
        {
            // This call is required by the Windows Forms Designer.
            InitializeComponent();
            backColor = Color.Empty;
            foreColor = Color.Empty;
            borderColor = DefaultBorderColor;
            hotTrackColor = DefaultHotTrackColor;
            hotTabIndex = -1;
        }

        private enum TabState
        {
            Active = 0,
            MouseOver,
            Inactive
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                backgroundBrush?.Dispose();
                borderPen?.Dispose();
                hotTrackBrush?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new Container();
        }
        #endregion

        [Browsable(true), Description("The background color of the control.")]
        public override Color BackColor
        {
            get
            {
                if (backColor.IsEmpty)
                {
                    if (Parent == null)
                    {
                        return DefaultBackColor;
                    }
                    else
                    {
                        return Parent.BackColor;
                    }
                }

                return backColor;
            }
            set
            {
                if (backColor != value)
                {
                    backColor = value;
                    DetermineDrawingMode();
                    // Let the Tabpages know that the backcolor has changed.
                    OnBackColorChanged(EventArgs.Empty);
                }
            }
        }

        [Browsable(true), Description("The foreground color of the control.")]
        public override Color ForeColor
        {
            get
            {
                if (foreColor.IsEmpty)
                {
                    if (Parent == null)
                    {
                        return DefaultForeColor;
                    }
                    else
                    {
                        return Parent.ForeColor;
                    }
                }

                return foreColor;
            }
            set
            {
                if (foreColor != value)
                {
                    foreColor = value;
                    DetermineDrawingMode();

                    // Let the Tabpages know that the forecolor has changed.
                    OnForeColorChanged(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets or sets the border color of the tabs.
        /// </summary>
        /// <value>
        /// The border color of the tabs.
        /// </value>
        [Description("The border color of the tabs.")]
        public Color BorderColor
        {
            get => borderColor;
            set
            {
                if (borderColor != value)
                {
                    borderColor = value;

                    if (borderPen != null)
                    {
                        borderPen.Dispose();
                        borderPen = new Pen(borderColor);
                    }

                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the color displayed when the mouse is over an inactive tab.
        /// </summary>
        /// <value>
        /// The color displayed when the mouse is over an inactive tab.
        /// </value>
        [Description("The color displayed when the mouse is over an inactive tab.")]
        public Color HotTrackColor
        {
            get => hotTrackColor;
            set
            {
                if (hotTrackColor != value)
                {
                    hotTrackColor = value;

                    if (hotTrackBrush != null)
                    {
                        hotTrackBrush.Dispose();
                        hotTrackBrush = new SolidBrush(hotTrackColor);
                    }

                    Invalidate();
                }
            }
        }

        private bool HotTrackingEnabled
        {
            get
            {
                if (SystemInformation.IsHotTrackingEnabled)
                {
                    return true;
                }

                return HotTrack;
            }
        }

        private bool UseOwnerDraw
        {
            get
            {
                if (SystemInformation.HighContrast)
                {
                    return false;
                }

                return !backColor.IsEmpty && backColor != DefaultBackColor || !foreColor.IsEmpty && foreColor != DefaultForeColor;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void ResetBackColor()
        {
            BackColor = Color.Empty;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void ResetForeColor()
        {
            ForeColor = Color.Empty;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ResetBorderColor()
        {
            BorderColor = DefaultBorderColor;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ResetHotTrackColor()
        {
            HotTrackColor = DefaultHotTrackColor;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeBackColor()
        {
            return !backColor.IsEmpty;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeForeColor()
        {
            return !foreColor.IsEmpty;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeBorderColor()
        {
            return borderColor != DefaultBorderColor;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeHotTrackColor()
        {
            return hotTrackColor != DefaultHotTrackColor;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (!DesignMode)
            {
                if (hotTabIndex != -1)
                {
                    hotTabIndex = -1;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!DesignMode)
            {
                int index = GetTabIndexUnderCursor();

                if (index != hotTabIndex)
                {
                    hotTabIndex = index;
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (GetStyle(ControlStyles.UserPaint))
            {
                e.Graphics.Clear(BackColor);

                if (TabCount <= 0)
                {
                    return;
                }
                int activeTabIndex = SelectedIndex;

                // Draw the inactive tabs.
                for (int index = 0; index < TabCount; index++)
                {
                    if (index != activeTabIndex)
                    {
                        TabState state = TabState.Inactive;
                        if (index == hotTabIndex && HotTrackingEnabled)
                        {
                            state = TabState.MouseOver;
                        }

                        DrawTab(e.Graphics, TabPages[index], GetTabRect(index), state);
                    }
                }

                // Draw the active tab.
                DrawTab(e.Graphics, TabPages[activeTabIndex], GetTabRect(activeTabIndex), TabState.Active);
            }
        }

        protected override void OnSystemColorsChanged(EventArgs e)
        {
            base.OnSystemColorsChanged(e);

            DetermineDrawingMode();
        }

        private void DetermineDrawingMode()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, UseOwnerDraw);
            UpdateTabPageVisualStyleBackColor();
            if (GetStyle(ControlStyles.UserPaint))
            {
                if (backgroundBrush == null || backgroundBrush.Color != BackColor)
                {
                    backgroundBrush?.Dispose();
                    backgroundBrush = new SolidBrush(BackColor);
                }
                if (borderPen == null || borderPen.Color != borderColor)
                {
                    borderPen?.Dispose();
                    borderPen = new Pen(borderColor);
                }
                if (hotTrackBrush == null || hotTrackBrush.Color != hotTrackColor)
                {
                    hotTrackBrush?.Dispose();
                    hotTrackBrush = new SolidBrush(hotTrackColor);
                }
            }

            Invalidate();
        }

        private void DrawTab(Graphics graphics, TabPage page, Rectangle bounds, TabState state)
        {
            if (state == TabState.Active)
            {
                Rectangle activeTabRect = Rectangle.Inflate(bounds, 2, 2);

                graphics.FillRectangle(backgroundBrush, activeTabRect);
                DrawTabBorder(graphics, activeTabRect);
            }
            else if (state == TabState.MouseOver)
            {
                graphics.FillRectangle(hotTrackBrush, bounds);
                DrawTabBorder(graphics, bounds);
            }
            else
            {
                graphics.FillRectangle(backgroundBrush, bounds);
                DrawTabBorder(graphics, bounds);
            }

            DrawTabText(graphics, bounds, page);
        }

        private void DrawTabBorder(Graphics graphics, Rectangle bounds)
        {
            Point[] points = new Point[6];

            switch (Alignment)
            {
                case TabAlignment.Top:
                    points[0] = new Point(bounds.Left, bounds.Top);
                    points[1] = new Point(bounds.Left, bounds.Bottom - 1);
                    points[2] = new Point(bounds.Left, bounds.Top);
                    points[3] = new Point(bounds.Right - 1, bounds.Top);
                    points[4] = new Point(bounds.Right - 1, bounds.Top);
                    points[5] = new Point(bounds.Right - 1, bounds.Bottom - 1);
                    break;
                case TabAlignment.Bottom:
                    points[0] = new Point(bounds.Left, bounds.Top);
                    points[1] = new Point(bounds.Left, bounds.Bottom - 1);
                    points[2] = new Point(bounds.Left, bounds.Bottom - 1);
                    points[3] = new Point(bounds.Right - 1, bounds.Bottom - 1);
                    points[4] = new Point(bounds.Right - 1, bounds.Bottom - 1);
                    points[5] = new Point(bounds.Right - 1, bounds.Top);
                    break;
                case TabAlignment.Left:
                    points[0] = new Point(bounds.Left, bounds.Top);
                    points[1] = new Point(bounds.Right - 1, bounds.Top);
                    points[2] = new Point(bounds.Left, bounds.Top);
                    points[3] = new Point(bounds.Left, bounds.Bottom - 1);
                    points[4] = new Point(bounds.Left, bounds.Bottom - 1);
                    points[5] = new Point(bounds.Right, bounds.Bottom - 1);
                    break;
                case TabAlignment.Right:
                    points[0] = new Point(bounds.Left, bounds.Top);
                    points[1] = new Point(bounds.Right - 1, bounds.Top);
                    points[2] = new Point(bounds.Right - 1, bounds.Top);
                    points[3] = new Point(bounds.Right - 1, bounds.Bottom - 1);
                    points[4] = new Point(bounds.Right - 1, bounds.Bottom - 1);
                    points[5] = new Point(bounds.Left, bounds.Bottom - 1);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }

            graphics.DrawLines(borderPen, points);
        }

        private void DrawTabText(Graphics graphics, Rectangle bounds, TabPage page)
        {
            // Set up rotation for left and right aligned tabs.
            if (Alignment == TabAlignment.Left || Alignment == TabAlignment.Right)
            {
                float rotateAngle = 90;
                if (Alignment == TabAlignment.Left)
                {
                    rotateAngle = 270;
                }

                PointF cp = new PointF(bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));
                graphics.TranslateTransform(cp.X, cp.Y);
                graphics.RotateTransform(rotateAngle);

                bounds = new Rectangle(-(bounds.Height / 2), -(bounds.Width / 2), bounds.Height, bounds.Width);
            }

            Rectangle textBounds = Rectangle.Inflate(bounds, -3, -3);
            Color textColor = page.Enabled ? ForeColor : SystemColors.GrayText;

            // Draw the Tab text.
            TextRenderer.DrawText(graphics, page.Text, Font, textBounds, textColor, BackColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            graphics.ResetTransform();
        }

        private int GetTabIndexUnderCursor()
        {
            Point cursor = PointToClient(MousePosition);

            NativeStructs.TCHITTESTINFO hti = new NativeStructs.TCHITTESTINFO
            {
                pt = new NativeStructs.POINT(cursor.X, cursor.Y),
                flags = 0
            };

            int index = SafeNativeMethods.SendMessage(Handle, NativeConstants.TCM_HITTEST, IntPtr.Zero, ref hti).ToInt32();

            return index;
        }

        private void UpdateTabPageVisualStyleBackColor()
        {
            bool useVisualStyleBackColor = false;
            if (SystemInformation.HighContrast || BackColor == DefaultBackColor)
            {
                useVisualStyleBackColor = true;
            }

            // When the BackColor is changed the TabControl only updates the UseVisualStyleBackColor property for the active tab.
            // Set the property on all tabs so that the correct color is displayed when the user switches tabs.

            for (int i = 0; i < TabCount; i++)
            {
                TabPages[i].UseVisualStyleBackColor = useVisualStyleBackColor;
            }
        }
    }
}
