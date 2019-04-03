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

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace PSFilterLoad.ColorPicker
{
    internal sealed class ColorPickerForm : Form
    {
        private Label redLabel;
        private Label blueLabel;
        private Label greenLabel;
        private Label hueLabel;

        private NumericUpDown redUpDown;
        private NumericUpDown greenUpDown;
        private NumericUpDown blueUpDown;
        private NumericUpDown hueUpDown;
        private NumericUpDown valueUpDown;
        private NumericUpDown saturationUpDown;

        private System.ComponentModel.Container components = null;
        private Label saturationLabel;
        private Label valueLabel;
        private ColorGradientControl valueGradientControl;
        private ColorWheel colorWheel;

        private int ignoreChangedEvents = 0;

        private System.Windows.Forms.Label hexLabel;
        private System.Windows.Forms.TextBox hexBox;
        private uint ignore = 0;
        private HeaderLabel rgbHeader;
        private HeaderLabel hsvHeader;

        private ColorGradientControl hueGradientControl;
        private ColorGradientControl saturationGradientControl;
        private ColorGradientControl redGradientControl;
        private ColorGradientControl greenGradientControl;
        private ColorGradientControl blueGradientControl;

        private ColorRectangleControl colorDisplayWidget;
        private HeaderLabel swatchHeader;
        private SwatchControl swatchControl;
        private Button okBtn;
        private Button cancelBtn;

        private static readonly ColorBgra[] paletteColors;

        static ColorPickerForm()
        {
            paletteColors = new ColorBgra[64]
                {
                    // row 1
                    ColorBgra.FromUInt32(0xFF000000),
                    ColorBgra.FromUInt32(0xFF404040),
                    ColorBgra.FromUInt32(0xFFFF0000),
                    ColorBgra.FromUInt32(0xFFFF6A00),
                    ColorBgra.FromUInt32(0xFFFFD800),
                    ColorBgra.FromUInt32(0xFFB6FF00),
                    ColorBgra.FromUInt32(0xFF4CFF00),
                    ColorBgra.FromUInt32(0xFF00FF21),
                    ColorBgra.FromUInt32(0xFF00FF90),
                    ColorBgra.FromUInt32(0xFF00FFFF),
                    ColorBgra.FromUInt32(0xFF0094FF),
                    ColorBgra.FromUInt32(0xFF0026FF),
                    ColorBgra.FromUInt32(0xFF4800FF),
                    ColorBgra.FromUInt32(0xFFB200FF),
                    ColorBgra.FromUInt32(0xFFFF00DC),
                    ColorBgra.FromUInt32(0xFFFF006E),
                    // row 2
                    ColorBgra.FromUInt32(0xFFFFFFFF),
                    ColorBgra.FromUInt32(0xFF808080),
                    ColorBgra.FromUInt32(0xFF7F0000),
                    ColorBgra.FromUInt32(0xFF7F3300),
                    ColorBgra.FromUInt32(0xFF7F6A00),
                    ColorBgra.FromUInt32(0xFF5B7F00),
                    ColorBgra.FromUInt32(0xFF267F00),
                    ColorBgra.FromUInt32(0xFF007F0E),
                    ColorBgra.FromUInt32(0xFF007F46),
                    ColorBgra.FromUInt32(0xFF007F7F),
                    ColorBgra.FromUInt32(0xFF004A7F),
                    ColorBgra.FromUInt32(0xFF00137F),
                    ColorBgra.FromUInt32(0xFF21007F),
                    ColorBgra.FromUInt32(0xFF57007F),
                    ColorBgra.FromUInt32(0xFF7F006E),
                    ColorBgra.FromUInt32(0xFF7F006E),
                    // row 3
                    ColorBgra.FromUInt32(0xFFA0A0A0),
                    ColorBgra.FromUInt32(0xFF303030),
                    ColorBgra.FromUInt32(0xFFFF7F7F),
                    ColorBgra.FromUInt32(0xFFFFB27F),
                    ColorBgra.FromUInt32(0xFFFFE97F),
                    ColorBgra.FromUInt32(0xFFDAFF7F),
                    ColorBgra.FromUInt32(0xFFA5FF7F),
                    ColorBgra.FromUInt32(0xFF7FFF8E),
                    ColorBgra.FromUInt32(0xFF7FFFC5),
                    ColorBgra.FromUInt32(0xFF7FFFFF),
                    ColorBgra.FromUInt32(0xFF7FC9FF),
                    ColorBgra.FromUInt32(0xFF7F92FF),
                    ColorBgra.FromUInt32(0xFFA17FFF),
                    ColorBgra.FromUInt32(0xFFD67FFF),
                    ColorBgra.FromUInt32(0xFFFF7FED),
                    ColorBgra.FromUInt32(0xFFFF7FB6),
                    // row 4
                    ColorBgra.FromUInt32(0xFFC0C0C0),
                    ColorBgra.FromUInt32(0xFF606060),
                    ColorBgra.FromUInt32(0xFF7F3F3F),
                    ColorBgra.FromUInt32(0xFF7F593F),
                    ColorBgra.FromUInt32(0xFF7F743F),
                    ColorBgra.FromUInt32(0xFF6D7F3F),
                    ColorBgra.FromUInt32(0xFF527F3F),
                    ColorBgra.FromUInt32(0xFF3F7F47),
                    ColorBgra.FromUInt32(0xFF3F7F62),
                    ColorBgra.FromUInt32(0xFF3F7F7F),
                    ColorBgra.FromUInt32(0xFF3F647F),
                    ColorBgra.FromUInt32(0xFF3F497F),
                    ColorBgra.FromUInt32(0xFF503F7F),
                    ColorBgra.FromUInt32(0xFF6B3F7F),
                    ColorBgra.FromUInt32(0xFF7F3F76),
                    ColorBgra.FromUInt32(0xFF7F3F5B)
                };
        }

        private bool IgnoreChangedEvents
        {
            get
            {
                return ignoreChangedEvents != 0;
            }
        }

        private ColorBgra userPrimaryColor;
        public ColorBgra UserPrimaryColor
        {
            get
            {
                return userPrimaryColor;
            }

            private set
            {
                if (userPrimaryColor != value)
                {
                    userPrimaryColor = value;

                    ignore++;

                    // only do the update on the last one, so partial RGB info isn't parsed.
                    Utility.SetNumericUpDownValue(redUpDown, value.R);
                    Utility.SetNumericUpDownValue(greenUpDown, value.G);
                    SetColorGradientValuesRgb(value.R, value.G, value.B);
                    SetColorGradientMinMaxColorsRgb(value.R, value.G, value.B);

                    ignore--;
                    Utility.SetNumericUpDownValue(blueUpDown, value.B);
                    Update();

                    if (hexBox.Text.Length == 6) // skip this step if the hexBox is being edited
                    {
                        string hexText = GetHexNumericUpDownValue(value.R, value.G, value.B);
                        hexBox.Text = hexText;
                    }

                    SyncHsvFromRgb(value);
                    colorDisplayWidget.RectangleColor = userPrimaryColor.ToColor();

                    colorDisplayWidget.Invalidate();
                }
            }
        }

        internal void SetDefaultColor(short r, short g, short b)
        {
            redUpDown.Value = r;
            greenUpDown.Value = g;
            blueUpDown.Value = b;
        }

        private static string GetHexNumericUpDownValue(int red, int green, int blue)
        {
            int newHexNumber = (red << 16) | (green << 8) | blue;
            string newHexText = System.Convert.ToString(newHexNumber, 16);

            while (newHexText.Length < 6)
            {
                newHexText = "0" + newHexText;
            }

            return newHexText.ToUpperInvariant();
        }

        /// <summary>
        /// Whenever a color is changed via RGB methods, call this and the HSV
        /// counterparts will be sync'd up.
        /// </summary>
        /// <param name="newColor">The RGB color that should be converted to HSV.</param>
        private void SyncHsvFromRgb(ColorBgra newColor)
        {
            if (ignore == 0)
            {
                ignore++;
                HsvColor hsvColor = HsvColor.FromColor(newColor.ToColor());

                Utility.SetNumericUpDownValue(hueUpDown, hsvColor.Hue);
                Utility.SetNumericUpDownValue(saturationUpDown, hsvColor.Saturation);
                Utility.SetNumericUpDownValue(valueUpDown, hsvColor.Value);

                SetColorGradientValuesHsv(hsvColor.Hue, hsvColor.Saturation, hsvColor.Value);
                SetColorGradientMinMaxColorsHsv(hsvColor.Hue, hsvColor.Saturation, hsvColor.Value);

                colorWheel.HsvColor = hsvColor;
                ignore--;
            }
        }

        private void SetColorGradientValuesRgb(int r, int g, int b)
        {
            PushIgnoreChangedEvents();

            if (redGradientControl.Value != r)
            {
                redGradientControl.Value = r;
            }

            if (greenGradientControl.Value != g)
            {
                greenGradientControl.Value = g;
            }

            if (blueGradientControl.Value != b)
            {
                blueGradientControl.Value = b;
            }

            PopIgnoreChangedEvents();
        }

        private void SetColorGradientValuesHsv(int h, int s, int v)
        {
            PushIgnoreChangedEvents();

            if (((hueGradientControl.Value * 360) / 255) != h)
            {
                hueGradientControl.Value = (255 * h) / 360;
            }

            if (((saturationGradientControl.Value * 100) / 255) != s)
            {
                saturationGradientControl.Value = (255 * s) / 100;
            }

            if (((valueGradientControl.Value * 100) / 255) != v)
            {
                valueGradientControl.Value = (255 * v) / 100;
            }

            PopIgnoreChangedEvents();
        }

        /// <summary>
        /// Whenever a color is changed via HSV methods, call this and the RGB
        /// counterparts will be sync'd up.
        /// </summary>
        /// <param name="newColor">The HSV color that should be converted to RGB.</param>
        private void SyncRgbFromHsv(HsvColor newColor)
        {
            if (ignore == 0)
            {
                ignore++;
                RgbColor rgbColor = newColor.ToRgb();

                Utility.SetNumericUpDownValue(redUpDown, rgbColor.Red);
                Utility.SetNumericUpDownValue(greenUpDown, rgbColor.Green);
                Utility.SetNumericUpDownValue(blueUpDown, rgbColor.Blue);

                string hexText = GetHexNumericUpDownValue(rgbColor.Red, rgbColor.Green, rgbColor.Blue);
                hexBox.Text = hexText;

                SetColorGradientValuesRgb(rgbColor.Red, rgbColor.Green, rgbColor.Blue);
                SetColorGradientMinMaxColorsRgb(rgbColor.Red, rgbColor.Green, rgbColor.Blue);

                ignore--;
            }
        }

        public ColorPickerForm(string title)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            if (!string.IsNullOrEmpty(title))
            {
                Text = title;
            }

            redLabel.Text = "R:";
            greenLabel.Text = "G:";
            blueLabel.Text = "B:";

            hueLabel.Text = "H:";
            saturationLabel.Text = "S:";
            valueLabel.Text = "V:";

            rgbHeader.Text = "RGB";
            hexLabel.Text = "Hex:";
            hsvHeader.Text = "HSV";

            swatchControl.Colors = paletteColors;
            hexBox.Text = "000000";
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                    components = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);

            UpdateControlBackColor(this, BackColor);
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);

            UpdateControlForeColor(this, ForeColor);
        }

        private static void UpdateControlBackColor(Control parent, Color backColor)
        {
            foreach (Control item in parent.Controls)
            {
                if (item is Button button)
                {
                    // Reset the BackColor of all Button controls.
                    button.UseVisualStyleBackColor = true;
                }
                else
                {
                    item.BackColor = backColor;

                    if (item.HasChildren)
                    {
                        UpdateControlBackColor(item, backColor);
                    }
                }
            }
        }

        private static void UpdateControlForeColor(Control parent, Color foreColor)
        {
            foreach (Control item in parent.Controls)
            {
                if (item is Button button)
                {
                    // Reset the ForeColor of all Button controls.
                    button.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    item.ForeColor = foreColor;

                    if (item.HasChildren)
                    {
                        UpdateControlForeColor(item, foreColor);
                    }
                }
            }
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            redUpDown = new System.Windows.Forms.NumericUpDown();
            greenUpDown = new System.Windows.Forms.NumericUpDown();
            blueUpDown = new System.Windows.Forms.NumericUpDown();
            redLabel = new System.Windows.Forms.Label();
            blueLabel = new System.Windows.Forms.Label();
            greenLabel = new System.Windows.Forms.Label();
            saturationLabel = new System.Windows.Forms.Label();
            valueLabel = new System.Windows.Forms.Label();
            hueLabel = new System.Windows.Forms.Label();
            valueUpDown = new System.Windows.Forms.NumericUpDown();
            saturationUpDown = new System.Windows.Forms.NumericUpDown();
            hueUpDown = new System.Windows.Forms.NumericUpDown();
            hexBox = new System.Windows.Forms.TextBox();
            hexLabel = new System.Windows.Forms.Label();
            okBtn = new System.Windows.Forms.Button();
            cancelBtn = new System.Windows.Forms.Button();
            blueGradientControl = new PSFilterLoad.ColorPicker.ColorGradientControl();
            greenGradientControl = new PSFilterLoad.ColorPicker.ColorGradientControl();
            redGradientControl = new PSFilterLoad.ColorPicker.ColorGradientControl();
            saturationGradientControl = new PSFilterLoad.ColorPicker.ColorGradientControl();
            hueGradientControl = new PSFilterLoad.ColorPicker.ColorGradientControl();
            colorWheel = new PSFilterLoad.ColorPicker.ColorWheel();
            hsvHeader = new PSFilterLoad.ColorPicker.HeaderLabel();
            rgbHeader = new PSFilterLoad.ColorPicker.HeaderLabel();
            valueGradientControl = new PSFilterLoad.ColorPicker.ColorGradientControl();
            colorDisplayWidget = new PSFilterLoad.ColorPicker.ColorRectangleControl();
            swatchHeader = new PSFilterLoad.ColorPicker.HeaderLabel();
            swatchControl = new PSFilterLoad.ColorPicker.SwatchControl();
            ((System.ComponentModel.ISupportInitialize)(redUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(greenUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(blueUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(valueUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(saturationUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(hueUpDown)).BeginInit();
            SuspendLayout();
            //
            // redUpDown
            //
            redUpDown.Location = new System.Drawing.Point(318, 17);
            redUpDown.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            redUpDown.Name = "redUpDown";
            redUpDown.Size = new System.Drawing.Size(56, 20);
            redUpDown.TabIndex = 2;
            redUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            redUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            redUpDown.Enter += new System.EventHandler(UpDown_Enter);
            redUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            redUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // greenUpDown
            //
            greenUpDown.Location = new System.Drawing.Point(318, 41);
            greenUpDown.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            greenUpDown.Name = "greenUpDown";
            greenUpDown.Size = new System.Drawing.Size(56, 20);
            greenUpDown.TabIndex = 3;
            greenUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            greenUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            greenUpDown.Enter += new System.EventHandler(UpDown_Enter);
            greenUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            greenUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // blueUpDown
            //
            blueUpDown.Location = new System.Drawing.Point(318, 65);
            blueUpDown.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            blueUpDown.Name = "blueUpDown";
            blueUpDown.Size = new System.Drawing.Size(56, 20);
            blueUpDown.TabIndex = 4;
            blueUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            blueUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            blueUpDown.Enter += new System.EventHandler(UpDown_Enter);
            blueUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            blueUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // redLabel
            //
            redLabel.AutoSize = true;
            redLabel.Location = new System.Drawing.Point(220, 21);
            redLabel.Name = "redLabel";
            redLabel.Size = new System.Drawing.Size(15, 13);
            redLabel.TabIndex = 7;
            redLabel.Text = "R";
            redLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // blueLabel
            //
            blueLabel.AutoSize = true;
            blueLabel.Location = new System.Drawing.Point(220, 69);
            blueLabel.Name = "blueLabel";
            blueLabel.Size = new System.Drawing.Size(14, 13);
            blueLabel.TabIndex = 8;
            blueLabel.Text = "B";
            blueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // greenLabel
            //
            greenLabel.AutoSize = true;
            greenLabel.Location = new System.Drawing.Point(220, 45);
            greenLabel.Name = "greenLabel";
            greenLabel.Size = new System.Drawing.Size(15, 13);
            greenLabel.TabIndex = 9;
            greenLabel.Text = "G";
            greenLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // saturationLabel
            //
            saturationLabel.AutoSize = true;
            saturationLabel.Location = new System.Drawing.Point(220, 157);
            saturationLabel.Name = "saturationLabel";
            saturationLabel.Size = new System.Drawing.Size(17, 13);
            saturationLabel.TabIndex = 16;
            saturationLabel.Text = "S:";
            saturationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // valueLabel
            //
            valueLabel.AutoSize = true;
            valueLabel.Location = new System.Drawing.Point(220, 181);
            valueLabel.Name = "valueLabel";
            valueLabel.Size = new System.Drawing.Size(17, 13);
            valueLabel.TabIndex = 15;
            valueLabel.Text = "V:";
            valueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // hueLabel
            //
            hueLabel.AutoSize = true;
            hueLabel.Location = new System.Drawing.Point(220, 133);
            hueLabel.Name = "hueLabel";
            hueLabel.Size = new System.Drawing.Size(18, 13);
            hueLabel.TabIndex = 14;
            hueLabel.Text = "H:";
            hueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // valueUpDown
            //
            valueUpDown.Location = new System.Drawing.Point(318, 177);
            valueUpDown.Name = "valueUpDown";
            valueUpDown.Size = new System.Drawing.Size(56, 20);
            valueUpDown.TabIndex = 8;
            valueUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            valueUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            valueUpDown.Enter += new System.EventHandler(UpDown_Enter);
            valueUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            valueUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // saturationUpDown
            //
            saturationUpDown.Location = new System.Drawing.Point(318, 153);
            saturationUpDown.Name = "saturationUpDown";
            saturationUpDown.Size = new System.Drawing.Size(56, 20);
            saturationUpDown.TabIndex = 7;
            saturationUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            saturationUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            saturationUpDown.Enter += new System.EventHandler(UpDown_Enter);
            saturationUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            saturationUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // hueUpDown
            //
            hueUpDown.Location = new System.Drawing.Point(318, 129);
            hueUpDown.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            hueUpDown.Name = "hueUpDown";
            hueUpDown.Size = new System.Drawing.Size(56, 20);
            hueUpDown.TabIndex = 6;
            hueUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            hueUpDown.ValueChanged += new System.EventHandler(UpDown_ValueChanged);
            hueUpDown.Enter += new System.EventHandler(UpDown_Enter);
            hueUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(UpDown_KeyUp);
            hueUpDown.Leave += new System.EventHandler(UpDown_Leave);
            //
            // hexBox
            //
            hexBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            hexBox.Location = new System.Drawing.Point(318, 89);
            hexBox.Name = "hexBox";
            hexBox.Size = new System.Drawing.Size(56, 20);
            hexBox.TabIndex = 5;
            hexBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            hexBox.TextChanged += new System.EventHandler(UpDown_ValueChanged);
            hexBox.Enter += new System.EventHandler(HexUpDown_Enter);
            hexBox.KeyDown += new System.Windows.Forms.KeyEventHandler(hexBox_KeyDown);
            hexBox.Leave += new System.EventHandler(HexUpDown_Leave);
            //
            // hexLabel
            //
            hexLabel.AutoSize = true;
            hexLabel.Location = new System.Drawing.Point(220, 92);
            hexLabel.Name = "hexLabel";
            hexLabel.Size = new System.Drawing.Size(26, 13);
            hexLabel.TabIndex = 13;
            hexLabel.Text = "Hex";
            hexLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // okBtn
            //
            okBtn.Location = new System.Drawing.Point(218, 235);
            okBtn.Name = "okBtn";
            okBtn.Size = new System.Drawing.Size(75, 23);
            okBtn.TabIndex = 40;
            okBtn.Text = "Ok";
            okBtn.UseVisualStyleBackColor = true;
            okBtn.Click += new System.EventHandler(okBtn_Click);
            //
            // cancelBtn
            //
            cancelBtn.Location = new System.Drawing.Point(299, 235);
            cancelBtn.Name = "cancelBtn";
            cancelBtn.Size = new System.Drawing.Size(75, 23);
            cancelBtn.TabIndex = 41;
            cancelBtn.Text = "Cancel";
            cancelBtn.UseVisualStyleBackColor = true;
            cancelBtn.Click += new System.EventHandler(cancelBtn_Click);
            //
            // blueGradientControl
            //
            blueGradientControl.Count = 1;
            blueGradientControl.CustomGradient = null;
            blueGradientControl.DrawFarNub = true;
            blueGradientControl.DrawNearNub = false;
            blueGradientControl.Location = new System.Drawing.Point(241, 66);
            blueGradientControl.MaxColor = System.Drawing.Color.White;
            blueGradientControl.MinColor = System.Drawing.Color.Black;
            blueGradientControl.Name = "blueGradientControl";
            blueGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            blueGradientControl.Size = new System.Drawing.Size(73, 19);
            blueGradientControl.TabIndex = 39;
            blueGradientControl.TabStop = false;
            blueGradientControl.Value = 0;
            blueGradientControl.ValueChanged += new System.EventHandler<PSFilterLoad.ColorPicker.IndexEventArgs>(RgbGradientControl_ValueChanged);
            //
            // greenGradientControl
            //
            greenGradientControl.Count = 1;
            greenGradientControl.CustomGradient = null;
            greenGradientControl.DrawFarNub = true;
            greenGradientControl.DrawNearNub = false;
            greenGradientControl.Location = new System.Drawing.Point(241, 42);
            greenGradientControl.MaxColor = System.Drawing.Color.White;
            greenGradientControl.MinColor = System.Drawing.Color.Black;
            greenGradientControl.Name = "greenGradientControl";
            greenGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            greenGradientControl.Size = new System.Drawing.Size(73, 19);
            greenGradientControl.TabIndex = 38;
            greenGradientControl.TabStop = false;
            greenGradientControl.Value = 0;
            greenGradientControl.ValueChanged += new System.EventHandler<PSFilterLoad.ColorPicker.IndexEventArgs>(RgbGradientControl_ValueChanged);
            //
            // redGradientControl
            //
            redGradientControl.Count = 1;
            redGradientControl.CustomGradient = null;
            redGradientControl.DrawFarNub = true;
            redGradientControl.DrawNearNub = false;
            redGradientControl.Location = new System.Drawing.Point(241, 18);
            redGradientControl.MaxColor = System.Drawing.Color.White;
            redGradientControl.MinColor = System.Drawing.Color.Black;
            redGradientControl.Name = "redGradientControl";
            redGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            redGradientControl.Size = new System.Drawing.Size(73, 19);
            redGradientControl.TabIndex = 37;
            redGradientControl.TabStop = false;
            redGradientControl.Value = 0;
            redGradientControl.ValueChanged += new System.EventHandler<PSFilterLoad.ColorPicker.IndexEventArgs>(RgbGradientControl_ValueChanged);
            //
            // saturationGradientControl
            //
            saturationGradientControl.Count = 1;
            saturationGradientControl.CustomGradient = null;
            saturationGradientControl.DrawFarNub = true;
            saturationGradientControl.DrawNearNub = false;
            saturationGradientControl.Location = new System.Drawing.Point(241, 154);
            saturationGradientControl.MaxColor = System.Drawing.Color.White;
            saturationGradientControl.MinColor = System.Drawing.Color.Black;
            saturationGradientControl.Name = "saturationGradientControl";
            saturationGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            saturationGradientControl.Size = new System.Drawing.Size(73, 19);
            saturationGradientControl.TabIndex = 35;
            saturationGradientControl.TabStop = false;
            saturationGradientControl.Value = 0;
            saturationGradientControl.ValueChanged += new System.EventHandler<PSFilterLoad.ColorPicker.IndexEventArgs>(HsvGradientControl_ValueChanged);
            //
            // hueGradientControl
            //
            hueGradientControl.Count = 1;
            hueGradientControl.CustomGradient = null;
            hueGradientControl.DrawFarNub = true;
            hueGradientControl.DrawNearNub = false;
            hueGradientControl.Location = new System.Drawing.Point(241, 130);
            hueGradientControl.MaxColor = System.Drawing.Color.White;
            hueGradientControl.MinColor = System.Drawing.Color.Black;
            hueGradientControl.Name = "hueGradientControl";
            hueGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            hueGradientControl.Size = new System.Drawing.Size(73, 19);
            hueGradientControl.TabIndex = 34;
            hueGradientControl.TabStop = false;
            hueGradientControl.Value = 0;
            hueGradientControl.ValueChanged += new System.EventHandler<PSFilterLoad.ColorPicker.IndexEventArgs>(HsvGradientControl_ValueChanged);
            //
            // colorWheel
            //
            colorWheel.Location = new System.Drawing.Point(54, 24);
            colorWheel.Name = "colorWheel";
            colorWheel.Size = new System.Drawing.Size(146, 147);
            colorWheel.TabIndex = 3;
            colorWheel.TabStop = false;
            colorWheel.ColorChanged += new System.EventHandler(ColorWheel_ColorChanged);
            //
            // hsvHeader
            //
            hsvHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            hsvHeader.Location = new System.Drawing.Point(220, 113);
            hsvHeader.Name = "hsvHeader";
            hsvHeader.RightMargin = 0;
            hsvHeader.Size = new System.Drawing.Size(154, 14);
            hsvHeader.TabIndex = 28;
            hsvHeader.TabStop = false;
            //
            // rgbHeader
            //
            rgbHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            rgbHeader.Location = new System.Drawing.Point(220, 1);
            rgbHeader.Name = "rgbHeader";
            rgbHeader.RightMargin = 0;
            rgbHeader.Size = new System.Drawing.Size(154, 14);
            rgbHeader.TabIndex = 27;
            rgbHeader.TabStop = false;
            //
            // valueGradientControl
            //
            valueGradientControl.Count = 1;
            valueGradientControl.CustomGradient = null;
            valueGradientControl.DrawFarNub = true;
            valueGradientControl.DrawNearNub = false;
            valueGradientControl.Location = new System.Drawing.Point(241, 178);
            valueGradientControl.MaxColor = System.Drawing.Color.White;
            valueGradientControl.MinColor = System.Drawing.Color.Black;
            valueGradientControl.Name = "valueGradientControl";
            valueGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            valueGradientControl.Size = new System.Drawing.Size(73, 19);
            valueGradientControl.TabIndex = 2;
            valueGradientControl.TabStop = false;
            valueGradientControl.Value = 0;
            valueGradientControl.ValueChanged += new System.EventHandler<PSFilterLoad.ColorPicker.IndexEventArgs>(HsvGradientControl_ValueChanged);
            //
            // colorDisplayWidget
            //
            colorDisplayWidget.Location = new System.Drawing.Point(7, 16);
            colorDisplayWidget.Name = "colorDisplayWidget";
            colorDisplayWidget.RectangleColor = System.Drawing.Color.Empty;
            colorDisplayWidget.Size = new System.Drawing.Size(42, 42);
            colorDisplayWidget.TabIndex = 32;
            //
            // swatchHeader
            //
            swatchHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            swatchHeader.Location = new System.Drawing.Point(8, 177);
            swatchHeader.Name = "swatchHeader";
            swatchHeader.RightMargin = 0;
            swatchHeader.Size = new System.Drawing.Size(193, 14);
            swatchHeader.TabIndex = 30;
            swatchHeader.TabStop = false;
            //
            // swatchControl
            //
            swatchControl.BlinkHighlight = false;
            swatchControl.Colors = new PaintDotNet.ColorBgra[0];
            swatchControl.Location = new System.Drawing.Point(8, 189);
            swatchControl.Name = "swatchControl";
            swatchControl.Size = new System.Drawing.Size(192, 74);
            swatchControl.TabIndex = 31;
            swatchControl.Text = "swatchControl1";
            swatchControl.ColorClicked += new System.EventHandler<PSFilterLoad.ColorPicker.IndexEventArgs>(swatchControl_ColorClicked);
            //
            // ColorPickerForm
            //
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(386, 270);
            Controls.Add(cancelBtn);
            Controls.Add(okBtn);
            Controls.Add(valueLabel);
            Controls.Add(saturationLabel);
            Controls.Add(hueLabel);
            Controls.Add(greenLabel);
            Controls.Add(blueLabel);
            Controls.Add(redLabel);
            Controls.Add(hexLabel);
            Controls.Add(blueGradientControl);
            Controls.Add(greenGradientControl);
            Controls.Add(redGradientControl);
            Controls.Add(saturationGradientControl);
            Controls.Add(hueGradientControl);
            Controls.Add(colorWheel);
            Controls.Add(hsvHeader);
            Controls.Add(rgbHeader);
            Controls.Add(valueGradientControl);
            Controls.Add(blueUpDown);
            Controls.Add(greenUpDown);
            Controls.Add(redUpDown);
            Controls.Add(hexBox);
            Controls.Add(hueUpDown);
            Controls.Add(saturationUpDown);
            Controls.Add(valueUpDown);
            Controls.Add(colorDisplayWidget);
            Controls.Add(swatchHeader);
            Controls.Add(swatchControl);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ColorPickerForm";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Color Picker";
            ((System.ComponentModel.ISupportInitialize)(redUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(greenUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(blueUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(valueUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(saturationUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(hueUpDown)).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private void ColorWheel_ColorChanged(object sender, EventArgs e)
        {
            if (IgnoreChangedEvents)
            {
                return;
            }

            PushIgnoreChangedEvents();

            HsvColor hsvColor = colorWheel.HsvColor;
            RgbColor rgbColor = hsvColor.ToRgb();
            ColorBgra color = ColorBgra.FromBgra((byte)rgbColor.Blue, (byte)rgbColor.Green, (byte)rgbColor.Red, 255);

            Utility.SetNumericUpDownValue(hueUpDown, hsvColor.Hue);
            Utility.SetNumericUpDownValue(saturationUpDown, hsvColor.Saturation);
            Utility.SetNumericUpDownValue(valueUpDown, hsvColor.Value);

            Utility.SetNumericUpDownValue(redUpDown, color.R);
            Utility.SetNumericUpDownValue(greenUpDown, color.G);
            Utility.SetNumericUpDownValue(blueUpDown, color.B);

            string hexText = GetHexNumericUpDownValue(color.R, color.G, color.B);
            hexBox.Text = hexText;

            SetColorGradientValuesHsv(hsvColor.Hue, hsvColor.Saturation, hsvColor.Value);
            SetColorGradientMinMaxColorsHsv(hsvColor.Hue, hsvColor.Saturation, hsvColor.Value);

            SetColorGradientValuesRgb(color.R, color.G, color.B);
            SetColorGradientMinMaxColorsRgb(color.R, color.G, color.B);

            UserPrimaryColor = color;

            PopIgnoreChangedEvents();

            Update();
        }

        private void SetColorGradientMinMaxColorsHsv(int h, int s, int v)
        {
            Color[] hueColors = new Color[361];

            for (int newH = 0; newH <= 360; ++newH)
            {
                HsvColor hsv = new HsvColor(newH, 100, 100);
                hueColors[newH] = hsv.ToColor();
            }

            hueGradientControl.CustomGradient = hueColors;

            Color[] satColors = new Color[101];

            for (int newS = 0; newS <= 100; ++newS)
            {
                HsvColor hsv = new HsvColor(h, newS, v);
                satColors[newS] = hsv.ToColor();
            }

            saturationGradientControl.CustomGradient = satColors;

            valueGradientControl.MaxColor = new HsvColor(h, s, 100).ToColor();
            valueGradientControl.MinColor = new HsvColor(h, s, 0).ToColor();
        }

        private void SetColorGradientMinMaxColorsRgb(int r, int g, int b)
        {
            redGradientControl.MaxColor = Color.FromArgb(255, g, b);
            redGradientControl.MinColor = Color.FromArgb(0, g, b);
            greenGradientControl.MaxColor = Color.FromArgb(r, 255, b);
            greenGradientControl.MinColor = Color.FromArgb(r, 0, b);
            blueGradientControl.MaxColor = Color.FromArgb(r, g, 255);
            blueGradientControl.MinColor = Color.FromArgb(r, g, 0);
        }

        private void RgbGradientControl_ValueChanged(object sender, IndexEventArgs ce)
        {
            if (IgnoreChangedEvents)
            {
                return;
            }

            int red;
            if (sender == redGradientControl)
            {
                red = redGradientControl.Value;
            }
            else
            {
                red = (int)redUpDown.Value;
            }

            int green;
            if (sender == greenGradientControl)
            {
                green = greenGradientControl.Value;
            }
            else
            {
                green = (int)greenUpDown.Value;
            }

            int blue;
            if (sender == blueGradientControl)
            {
                blue = blueGradientControl.Value;
            }
            else
            {
                blue = (int)blueUpDown.Value;
            }

            Color rgbColor = Color.FromArgb(255, red, green, blue);
            HsvColor hsvColor = HsvColor.FromColor(rgbColor);

            PushIgnoreChangedEvents();
            Utility.SetNumericUpDownValue(hueUpDown, hsvColor.Hue);
            Utility.SetNumericUpDownValue(saturationUpDown, hsvColor.Saturation);
            Utility.SetNumericUpDownValue(valueUpDown, hsvColor.Value);

            Utility.SetNumericUpDownValue(redUpDown, rgbColor.R);
            Utility.SetNumericUpDownValue(greenUpDown, rgbColor.G);
            Utility.SetNumericUpDownValue(blueUpDown, rgbColor.B);
            PopIgnoreChangedEvents();

            string hexText = GetHexNumericUpDownValue(rgbColor.R, rgbColor.G, rgbColor.B);
            hexBox.Text = hexText;

            ColorBgra color = ColorBgra.FromColor(rgbColor);

            UserPrimaryColor = color;

            Update();
        }

        private void HsvGradientControl_ValueChanged(object sender, IndexEventArgs e)
        {
            if (IgnoreChangedEvents)
            {
                return;
            }

            int hue;
            if (sender == hueGradientControl)
            {
                hue = (hueGradientControl.Value * 360) / 255;
            }
            else
            {
                hue = (int)hueUpDown.Value;
            }

            int saturation;
            if (sender == saturationGradientControl)
            {
                saturation = (saturationGradientControl.Value * 100) / 255;
            }
            else
            {
                saturation = (int)saturationUpDown.Value;
            }

            int value;
            if (sender == valueGradientControl)
            {
                value = (valueGradientControl.Value * 100) / 255;
            }
            else
            {
                value = (int)valueUpDown.Value;
            }

            HsvColor hsvColor = new HsvColor(hue, saturation, value);
            colorWheel.HsvColor = hsvColor;
            RgbColor rgbColor = hsvColor.ToRgb();
            ColorBgra color = ColorBgra.FromBgra((byte)rgbColor.Blue, (byte)rgbColor.Green, (byte)rgbColor.Red, 255);

            Utility.SetNumericUpDownValue(hueUpDown, hsvColor.Hue);
            Utility.SetNumericUpDownValue(saturationUpDown, hsvColor.Saturation);
            Utility.SetNumericUpDownValue(valueUpDown, hsvColor.Value);

            Utility.SetNumericUpDownValue(redUpDown, rgbColor.Red);
            Utility.SetNumericUpDownValue(greenUpDown, rgbColor.Green);
            Utility.SetNumericUpDownValue(blueUpDown, rgbColor.Blue);

            string hexText = GetHexNumericUpDownValue(rgbColor.Red, rgbColor.Green, rgbColor.Blue);
            hexBox.Text = hexText;

            UserPrimaryColor = color;

            Update();
        }

        private void UpDown_Enter(object sender, System.EventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            nud.Select(0, nud.Text.Length);
        }

        private void UpDown_Leave(object sender, System.EventArgs e)
        {
            UpDown_ValueChanged(sender, e);
        }

        private void HexUpDown_Enter(object sender, System.EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Select(0, tb.Text.Length);
        }

        private void HexUpDown_Leave(object sender, System.EventArgs e)
        {
            hexBox.Text = hexBox.Text.ToUpperInvariant();
            UpDown_ValueChanged(sender, e);
        }

        private void hexBox_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) || (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) && ModifierKeys != Keys.Shift)
            {
                e.Handled = true;
                e.SuppressKeyPress = false;
            }
            else if (e.KeyCode == Keys.A || e.KeyCode == Keys.B || e.KeyCode == Keys.C || e.KeyCode == Keys.D || e.KeyCode == Keys.E || e.KeyCode == Keys.F || e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                e.Handled = true;
                e.SuppressKeyPress = false;
            }
            else
            {
                e.Handled = false;
                e.SuppressKeyPress = true;
            }
        }

        private void UpDown_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;

            if (Utility.CheckNumericUpDown(nud))
            {
                UpDown_ValueChanged(sender, e);
            }
        }

        private void UpDown_ValueChanged(object sender, System.EventArgs e)
        {
            if (IgnoreChangedEvents)
            {
                return;
            }
            else
            {
                PushIgnoreChangedEvents();
                if (sender == redUpDown || sender == greenUpDown || sender == blueUpDown)
                {
                    string hexText = GetHexNumericUpDownValue((int)redUpDown.Value, (int)greenUpDown.Value, (int)blueUpDown.Value);
                    hexBox.Text = hexText;

                    ColorBgra rgbColor = ColorBgra.FromBgra((byte)blueUpDown.Value, (byte)greenUpDown.Value, (byte)redUpDown.Value, 255);

                    SetColorGradientMinMaxColorsRgb(rgbColor.R, rgbColor.G, rgbColor.B);
                    SetColorGradientValuesRgb(rgbColor.R, rgbColor.G, rgbColor.B);

                    SyncHsvFromRgb(rgbColor);

                    UserPrimaryColor = rgbColor;
                }
                else if (sender == hexBox)
                {
                    int hexInt = 0;

                    if (hexBox.Text.Length > 0)
                    {
                        try
                        {
                            hexInt = int.Parse(hexBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        }

                        // Needs to be changed so it reads what the RGB values were last
                        catch (FormatException)
                        {
                            hexInt = 0;
                            hexBox.Text = "";
                        }
                        catch (OverflowException)
                        {
                            hexInt = 16777215;
                            hexBox.Text = "FFFFFF";
                        }

                        if (!((hexInt <= 16777215) && (hexInt >= 0)))
                        {
                            hexInt = 16777215;
                            hexBox.Text = "FFFFFF";
                        }
                    }

                    int newRed = ((hexInt & 0xff0000) >> 16);
                    int newGreen = ((hexInt & 0x00ff00) >> 8);
                    int newBlue = (hexInt & 0x0000ff);

                    Utility.SetNumericUpDownValue(redUpDown, newRed);
                    Utility.SetNumericUpDownValue(greenUpDown, newGreen);
                    Utility.SetNumericUpDownValue(blueUpDown, newBlue);

                    SetColorGradientMinMaxColorsRgb(newRed, newGreen, newBlue);
                    SetColorGradientValuesRgb(newRed, newGreen, newBlue);

                    ColorBgra rgbColor = ColorBgra.FromBgra((byte)newBlue, (byte)newGreen, (byte)newRed, 255);
                    SyncHsvFromRgb(rgbColor);
                    UserPrimaryColor = rgbColor;
                }
                else if (sender == hueUpDown || sender == saturationUpDown || sender == valueUpDown)
                {
                    HsvColor oldHsvColor = colorWheel.HsvColor;
                    HsvColor newHsvColor = new HsvColor((int)hueUpDown.Value, (int)saturationUpDown.Value, (int)valueUpDown.Value);

                    if (oldHsvColor != newHsvColor)
                    {
                        colorWheel.HsvColor = newHsvColor;

                        SetColorGradientValuesHsv(newHsvColor.Hue, newHsvColor.Saturation, newHsvColor.Value);
                        SetColorGradientMinMaxColorsHsv(newHsvColor.Hue, newHsvColor.Saturation, newHsvColor.Value);

                        SyncRgbFromHsv(newHsvColor);
                        RgbColor rgbColor = newHsvColor.ToRgb();
                        UserPrimaryColor = ColorBgra.FromBgra((byte)rgbColor.Blue, (byte)rgbColor.Green, (byte)rgbColor.Red, 255);
                    }
                }
                PopIgnoreChangedEvents();
            }
        }

        private void PushIgnoreChangedEvents()
        {
            ++ignoreChangedEvents;
        }

        private void PopIgnoreChangedEvents()
        {
            --ignoreChangedEvents;
        }

        private void swatchControl_ColorClicked(object sender, IndexEventArgs e)
        {
            UserPrimaryColor = paletteColors[e.Index];
        }

        private void okBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
