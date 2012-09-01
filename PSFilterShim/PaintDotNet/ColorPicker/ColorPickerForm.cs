/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using System.Windows.Forms;
using PaintDotNet.SystemLayer;

namespace PaintDotNet
{
    internal sealed class ColorPickerForm  : Form
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
                return this.ignoreChangedEvents != 0;
            }
        }

       
        public void SetColorControlsRedraw(bool enabled)
        {
            Control[] controls =
                new Control[]
                {
                    this.colorWheel,
                    this.hueGradientControl,
                    this.saturationGradientControl,
                    this.valueGradientControl,
                    this.redGradientControl,
                    this.greenGradientControl,
                    this.blueGradientControl,
                    this.hueUpDown,
                    this.saturationUpDown,
                    this.valueUpDown,
                    this.redUpDown,
                    this.greenUpDown,
                    this.blueUpDown,
                };

            foreach (Control control in controls)
            {
                if (enabled)
                {
                    UI.ResumeControlPainting(control);
                    control.Invalidate(true);
                }
                else
                {
                    UI.SuspendControlPainting(control);
                }
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
                    this.colorDisplayWidget.RectangleColor = this.userPrimaryColor.ToColor();

                    this.colorDisplayWidget.Invalidate();
                }
            }
        }

        internal void SetColorString(short r, short g, short b)
        {
            hexBox.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:X2}{1:X2}{2:X2}", r, g, b);
        }

        private string GetHexNumericUpDownValue(int red, int green, int blue)
        {
            int newHexNumber = (red << 16) | (green << 8) | blue;
            string newHexText = System.Convert.ToString(newHexNumber, 16);
            
            while (newHexText.Length < 6)
            {
                newHexText = "0" + newHexText;
            }

            return newHexText.ToUpper();
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

            this.Text = title;
            this.redLabel.Text = "R:";
            this.greenLabel.Text = "G:";            
            this.blueLabel.Text = "B:";

            this.hueLabel.Text = "H:";
            this.saturationLabel.Text = "S:";
            this.valueLabel.Text = "V:";
            
            this.rgbHeader.Text = "RGB";
            this.hexLabel.Text = "Hex:";
            this.hsvHeader.Text = "HSV";

            this.swatchControl.Colors = paletteColors;
            this.hexBox.Text = "000000"; 
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

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.redUpDown = new System.Windows.Forms.NumericUpDown();
            this.greenUpDown = new System.Windows.Forms.NumericUpDown();
            this.blueUpDown = new System.Windows.Forms.NumericUpDown();
            this.redLabel = new System.Windows.Forms.Label();
            this.blueLabel = new System.Windows.Forms.Label();
            this.greenLabel = new System.Windows.Forms.Label();
            this.saturationLabel = new System.Windows.Forms.Label();
            this.valueLabel = new System.Windows.Forms.Label();
            this.hueLabel = new System.Windows.Forms.Label();
            this.valueUpDown = new System.Windows.Forms.NumericUpDown();
            this.saturationUpDown = new System.Windows.Forms.NumericUpDown();
            this.hueUpDown = new System.Windows.Forms.NumericUpDown();
            this.hexBox = new System.Windows.Forms.TextBox();
            this.hexLabel = new System.Windows.Forms.Label();
            this.okBtn = new System.Windows.Forms.Button();
            this.cancelBtn = new System.Windows.Forms.Button();
            this.blueGradientControl = new ColorGradientControl();
            this.greenGradientControl = new ColorGradientControl();
            this.redGradientControl = new ColorGradientControl();
            this.saturationGradientControl = new ColorGradientControl();
            this.hueGradientControl = new ColorGradientControl();
            this.colorWheel = new ColorWheel();
            this.hsvHeader = new HeaderLabel();
            this.rgbHeader = new HeaderLabel();
            this.valueGradientControl = new ColorGradientControl();
            this.colorDisplayWidget = new ColorRectangleControl();
            this.swatchHeader = new HeaderLabel();
            this.swatchControl = new SwatchControl();
            ((System.ComponentModel.ISupportInitialize)(this.redUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.greenUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.blueUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.saturationUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.hueUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // redUpDown
            // 
            this.redUpDown.Location = new System.Drawing.Point(318, 17);
            this.redUpDown.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.redUpDown.Name = "redUpDown";
            this.redUpDown.Size = new System.Drawing.Size(56, 20);
            this.redUpDown.TabIndex = 2;
            this.redUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.redUpDown.ValueChanged += new System.EventHandler(this.UpDown_ValueChanged);
            this.redUpDown.Enter += new System.EventHandler(this.UpDown_Enter);
            this.redUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(this.UpDown_KeyUp);
            this.redUpDown.Leave += new System.EventHandler(this.UpDown_Leave);
            // 
            // greenUpDown
            // 
            this.greenUpDown.Location = new System.Drawing.Point(318, 41);
            this.greenUpDown.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.greenUpDown.Name = "greenUpDown";
            this.greenUpDown.Size = new System.Drawing.Size(56, 20);
            this.greenUpDown.TabIndex = 3;
            this.greenUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.greenUpDown.ValueChanged += new System.EventHandler(this.UpDown_ValueChanged);
            this.greenUpDown.Enter += new System.EventHandler(this.UpDown_Enter);
            this.greenUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(this.UpDown_KeyUp);
            this.greenUpDown.Leave += new System.EventHandler(this.UpDown_Leave);
            // 
            // blueUpDown
            // 
            this.blueUpDown.Location = new System.Drawing.Point(318, 65);
            this.blueUpDown.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.blueUpDown.Name = "blueUpDown";
            this.blueUpDown.Size = new System.Drawing.Size(56, 20);
            this.blueUpDown.TabIndex = 4;
            this.blueUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.blueUpDown.ValueChanged += new System.EventHandler(this.UpDown_ValueChanged);
            this.blueUpDown.Enter += new System.EventHandler(this.UpDown_Enter);
            this.blueUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(this.UpDown_KeyUp);
            this.blueUpDown.Leave += new System.EventHandler(this.UpDown_Leave);
            // 
            // redLabel
            // 
            this.redLabel.AutoSize = true;
            this.redLabel.Location = new System.Drawing.Point(220, 21);
            this.redLabel.Name = "redLabel";
            this.redLabel.Size = new System.Drawing.Size(15, 13);
            this.redLabel.TabIndex = 7;
            this.redLabel.Text = "R";
            this.redLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // blueLabel
            // 
            this.blueLabel.AutoSize = true;
            this.blueLabel.Location = new System.Drawing.Point(220, 69);
            this.blueLabel.Name = "blueLabel";
            this.blueLabel.Size = new System.Drawing.Size(14, 13);
            this.blueLabel.TabIndex = 8;
            this.blueLabel.Text = "B";
            this.blueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // greenLabel
            // 
            this.greenLabel.AutoSize = true;
            this.greenLabel.Location = new System.Drawing.Point(220, 45);
            this.greenLabel.Name = "greenLabel";
            this.greenLabel.Size = new System.Drawing.Size(15, 13);
            this.greenLabel.TabIndex = 9;
            this.greenLabel.Text = "G";
            this.greenLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // saturationLabel
            // 
            this.saturationLabel.AutoSize = true;
            this.saturationLabel.Location = new System.Drawing.Point(220, 157);
            this.saturationLabel.Name = "saturationLabel";
            this.saturationLabel.Size = new System.Drawing.Size(17, 13);
            this.saturationLabel.TabIndex = 16;
            this.saturationLabel.Text = "S:";
            this.saturationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // valueLabel
            // 
            this.valueLabel.AutoSize = true;
            this.valueLabel.Location = new System.Drawing.Point(220, 181);
            this.valueLabel.Name = "valueLabel";
            this.valueLabel.Size = new System.Drawing.Size(17, 13);
            this.valueLabel.TabIndex = 15;
            this.valueLabel.Text = "V:";
            this.valueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // hueLabel
            // 
            this.hueLabel.AutoSize = true;
            this.hueLabel.Location = new System.Drawing.Point(220, 133);
            this.hueLabel.Name = "hueLabel";
            this.hueLabel.Size = new System.Drawing.Size(18, 13);
            this.hueLabel.TabIndex = 14;
            this.hueLabel.Text = "H:";
            this.hueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // valueUpDown
            // 
            this.valueUpDown.Location = new System.Drawing.Point(318, 177);
            this.valueUpDown.Name = "valueUpDown";
            this.valueUpDown.Size = new System.Drawing.Size(56, 20);
            this.valueUpDown.TabIndex = 8;
            this.valueUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.valueUpDown.ValueChanged += new System.EventHandler(this.UpDown_ValueChanged);
            this.valueUpDown.Enter += new System.EventHandler(this.UpDown_Enter);
            this.valueUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(this.UpDown_KeyUp);
            this.valueUpDown.Leave += new System.EventHandler(this.UpDown_Leave);
            // 
            // saturationUpDown
            // 
            this.saturationUpDown.Location = new System.Drawing.Point(318, 153);
            this.saturationUpDown.Name = "saturationUpDown";
            this.saturationUpDown.Size = new System.Drawing.Size(56, 20);
            this.saturationUpDown.TabIndex = 7;
            this.saturationUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.saturationUpDown.ValueChanged += new System.EventHandler(this.UpDown_ValueChanged);
            this.saturationUpDown.Enter += new System.EventHandler(this.UpDown_Enter);
            this.saturationUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(this.UpDown_KeyUp);
            this.saturationUpDown.Leave += new System.EventHandler(this.UpDown_Leave);
            // 
            // hueUpDown
            // 
            this.hueUpDown.Location = new System.Drawing.Point(318, 129);
            this.hueUpDown.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            this.hueUpDown.Name = "hueUpDown";
            this.hueUpDown.Size = new System.Drawing.Size(56, 20);
            this.hueUpDown.TabIndex = 6;
            this.hueUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.hueUpDown.ValueChanged += new System.EventHandler(this.UpDown_ValueChanged);
            this.hueUpDown.Enter += new System.EventHandler(this.UpDown_Enter);
            this.hueUpDown.KeyUp += new System.Windows.Forms.KeyEventHandler(this.UpDown_KeyUp);
            this.hueUpDown.Leave += new System.EventHandler(this.UpDown_Leave);
            // 
            // hexBox
            // 
            this.hexBox.CharacterCasing = CharacterCasing.Upper;
            this.hexBox.Location = new System.Drawing.Point(318, 89);
            this.hexBox.Name = "hexBox";
            this.hexBox.Size = new System.Drawing.Size(56, 20);
            this.hexBox.TabIndex = 5;
            this.hexBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.hexBox.Enter += new System.EventHandler(this.HexUpDown_Enter);
            this.hexBox.Leave += new System.EventHandler(this.HexUpDown_Leave);
            this.hexBox.KeyDown += new KeyEventHandler(hexBox_KeyDown);
            this.hexBox.TextChanged += new System.EventHandler(this.UpDown_ValueChanged);
            // 
            // hexLabel
            // 
            this.hexLabel.AutoSize = true;
            this.hexLabel.Location = new System.Drawing.Point(220, 92);
            this.hexLabel.Name = "hexLabel";
            this.hexLabel.Size = new System.Drawing.Size(26, 13);
            this.hexLabel.TabIndex = 13;
            this.hexLabel.Text = "Hex";
            this.hexLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // okBtn
            // 
            this.okBtn.Location = new System.Drawing.Point(218, 235);
            this.okBtn.Name = "okBtn";
            this.okBtn.Size = new System.Drawing.Size(75, 23);
            this.okBtn.TabIndex = 40;
            this.okBtn.Text = "Ok";
            this.okBtn.UseVisualStyleBackColor = true;
            this.okBtn.Click += new System.EventHandler(this.okBtn_Click);
            // 
            // cancelBtn
            // 
            this.cancelBtn.Location = new System.Drawing.Point(299, 235);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(75, 23);
            this.cancelBtn.TabIndex = 41;
            this.cancelBtn.Text = "Cancel";
            this.cancelBtn.UseVisualStyleBackColor = true;
            this.cancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
            // 
            // blueGradientControl
            // 
            this.blueGradientControl.Count = 1;
            this.blueGradientControl.CustomGradient = null;
            this.blueGradientControl.DrawFarNub = true;
            this.blueGradientControl.DrawNearNub = false;
            this.blueGradientControl.Location = new System.Drawing.Point(241, 66);
            this.blueGradientControl.MaxColor = System.Drawing.Color.White;
            this.blueGradientControl.MinColor = System.Drawing.Color.Black;
            this.blueGradientControl.Name = "blueGradientControl";
            this.blueGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.blueGradientControl.Size = new System.Drawing.Size(73, 19);
            this.blueGradientControl.TabIndex = 39;
            this.blueGradientControl.TabStop = false;
            this.blueGradientControl.Value = 0;
            this.blueGradientControl.ValueChanged += new EventHandler<IndexEventArgs>(this.RgbGradientControl_ValueChanged);
            // 
            // greenGradientControl
            // 
            this.greenGradientControl.Count = 1;
            this.greenGradientControl.CustomGradient = null;
            this.greenGradientControl.DrawFarNub = true;
            this.greenGradientControl.DrawNearNub = false;
            this.greenGradientControl.Location = new System.Drawing.Point(241, 42);
            this.greenGradientControl.MaxColor = System.Drawing.Color.White;
            this.greenGradientControl.MinColor = System.Drawing.Color.Black;
            this.greenGradientControl.Name = "greenGradientControl";
            this.greenGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.greenGradientControl.Size = new System.Drawing.Size(73, 19);
            this.greenGradientControl.TabIndex = 38;
            this.greenGradientControl.TabStop = false;
            this.greenGradientControl.Value = 0;
            this.greenGradientControl.ValueChanged += new EventHandler<IndexEventArgs>(this.RgbGradientControl_ValueChanged);
            // 
            // redGradientControl
            // 
            this.redGradientControl.Count = 1;
            this.redGradientControl.CustomGradient = null;
            this.redGradientControl.DrawFarNub = true;
            this.redGradientControl.DrawNearNub = false;
            this.redGradientControl.Location = new System.Drawing.Point(241, 18);
            this.redGradientControl.MaxColor = System.Drawing.Color.White;
            this.redGradientControl.MinColor = System.Drawing.Color.Black;
            this.redGradientControl.Name = "redGradientControl";
            this.redGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.redGradientControl.Size = new System.Drawing.Size(73, 19);
            this.redGradientControl.TabIndex = 37;
            this.redGradientControl.TabStop = false;
            this.redGradientControl.Value = 0;
            this.redGradientControl.ValueChanged += new EventHandler<IndexEventArgs>(this.RgbGradientControl_ValueChanged);
            // 
            // saturationGradientControl
            // 
            this.saturationGradientControl.Count = 1;
            this.saturationGradientControl.CustomGradient = null;
            this.saturationGradientControl.DrawFarNub = true;
            this.saturationGradientControl.DrawNearNub = false;
            this.saturationGradientControl.Location = new System.Drawing.Point(241, 154);
            this.saturationGradientControl.MaxColor = System.Drawing.Color.White;
            this.saturationGradientControl.MinColor = System.Drawing.Color.Black;
            this.saturationGradientControl.Name = "saturationGradientControl";
            this.saturationGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.saturationGradientControl.Size = new System.Drawing.Size(73, 19);
            this.saturationGradientControl.TabIndex = 35;
            this.saturationGradientControl.TabStop = false;
            this.saturationGradientControl.Value = 0;
            this.saturationGradientControl.ValueChanged += new EventHandler<IndexEventArgs>(this.HsvGradientControl_ValueChanged);
            // 
            // hueGradientControl
            // 
            this.hueGradientControl.Count = 1;
            this.hueGradientControl.CustomGradient = null;
            this.hueGradientControl.DrawFarNub = true;
            this.hueGradientControl.DrawNearNub = false;
            this.hueGradientControl.Location = new System.Drawing.Point(241, 130);
            this.hueGradientControl.MaxColor = System.Drawing.Color.White;
            this.hueGradientControl.MinColor = System.Drawing.Color.Black;
            this.hueGradientControl.Name = "hueGradientControl";
            this.hueGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.hueGradientControl.Size = new System.Drawing.Size(73, 19);
            this.hueGradientControl.TabIndex = 34;
            this.hueGradientControl.TabStop = false;
            this.hueGradientControl.Value = 0;
            this.hueGradientControl.ValueChanged += new EventHandler<IndexEventArgs>(this.HsvGradientControl_ValueChanged);
            // 
            // colorWheel
            // 
            this.colorWheel.Location = new System.Drawing.Point(54, 24);
            this.colorWheel.Name = "colorWheel";
            this.colorWheel.Size = new System.Drawing.Size(146, 147);
            this.colorWheel.TabIndex = 3;
            this.colorWheel.TabStop = false;
            this.colorWheel.ColorChanged += new System.EventHandler(this.ColorWheel_ColorChanged);
            // 
            // hsvHeader
            // 
            this.hsvHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            this.hsvHeader.Location = new System.Drawing.Point(220, 113);
            this.hsvHeader.Name = "hsvHeader";
            this.hsvHeader.RightMargin = 0;
            this.hsvHeader.Size = new System.Drawing.Size(154, 14);
            this.hsvHeader.TabIndex = 28;
            this.hsvHeader.TabStop = false;
            // 
            // rgbHeader
            // 
            this.rgbHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            this.rgbHeader.Location = new System.Drawing.Point(220, 1);
            this.rgbHeader.Name = "rgbHeader";
            this.rgbHeader.RightMargin = 0;
            this.rgbHeader.Size = new System.Drawing.Size(154, 14);
            this.rgbHeader.TabIndex = 27;
            this.rgbHeader.TabStop = false;
            // 
            // valueGradientControl
            // 
            this.valueGradientControl.Count = 1;
            this.valueGradientControl.CustomGradient = null;
            this.valueGradientControl.DrawFarNub = true;
            this.valueGradientControl.DrawNearNub = false;
            this.valueGradientControl.Location = new System.Drawing.Point(241, 178);
            this.valueGradientControl.MaxColor = System.Drawing.Color.White;
            this.valueGradientControl.MinColor = System.Drawing.Color.Black;
            this.valueGradientControl.Name = "valueGradientControl";
            this.valueGradientControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.valueGradientControl.Size = new System.Drawing.Size(73, 19);
            this.valueGradientControl.TabIndex = 2;
            this.valueGradientControl.TabStop = false;
            this.valueGradientControl.Value = 0;
            this.valueGradientControl.ValueChanged += new EventHandler<IndexEventArgs>(this.HsvGradientControl_ValueChanged);
            // 
            // colorDisplayWidget
            // 
            this.colorDisplayWidget.Location = new System.Drawing.Point(7, 16);
            this.colorDisplayWidget.Name = "colorDisplayWidget";
            this.colorDisplayWidget.RectangleColor = System.Drawing.Color.Empty;
            this.colorDisplayWidget.Size = new System.Drawing.Size(42, 42);
            this.colorDisplayWidget.TabIndex = 32;
            // 
            // swatchHeader
            // 
            this.swatchHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            this.swatchHeader.Location = new System.Drawing.Point(8, 177);
            this.swatchHeader.Name = "swatchHeader";
            this.swatchHeader.RightMargin = 0;
            this.swatchHeader.Size = new System.Drawing.Size(193, 14);
            this.swatchHeader.TabIndex = 30;
            this.swatchHeader.TabStop = false;
            // 
            // swatchControl
            // 
            this.swatchControl.BlinkHighlight = false;
            this.swatchControl.Colors = new PaintDotNet.ColorBgra[0];
            this.swatchControl.Location = new System.Drawing.Point(8, 189);
            this.swatchControl.Name = "swatchControl";
            this.swatchControl.Size = new System.Drawing.Size(192, 74);
            this.swatchControl.TabIndex = 31;
            this.swatchControl.Text = "swatchControl1";
            this.swatchControl.ColorClicked += new System.EventHandler<IndexEventArgs>(this.swatchControl_ColorClicked);
            // 
            // ColorPicker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(386, 270);
            this.Controls.Add(this.cancelBtn);
            this.Controls.Add(this.okBtn);
            this.Controls.Add(this.valueLabel);
            this.Controls.Add(this.saturationLabel);
            this.Controls.Add(this.hueLabel);
            this.Controls.Add(this.greenLabel);
            this.Controls.Add(this.blueLabel);
            this.Controls.Add(this.redLabel);
            this.Controls.Add(this.hexLabel);
            this.Controls.Add(this.blueGradientControl);
            this.Controls.Add(this.greenGradientControl);
            this.Controls.Add(this.redGradientControl);
            this.Controls.Add(this.saturationGradientControl);
            this.Controls.Add(this.hueGradientControl);
            this.Controls.Add(this.colorWheel);
            this.Controls.Add(this.hsvHeader);
            this.Controls.Add(this.rgbHeader);
            this.Controls.Add(this.valueGradientControl);
            this.Controls.Add(this.blueUpDown);
            this.Controls.Add(this.greenUpDown);
            this.Controls.Add(this.redUpDown);
            this.Controls.Add(this.hexBox);
            this.Controls.Add(this.hueUpDown);
            this.Controls.Add(this.saturationUpDown);
            this.Controls.Add(this.valueUpDown);
            this.Controls.Add(this.colorDisplayWidget);
            this.Controls.Add(this.swatchHeader);
            this.Controls.Add(this.swatchControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ColorPicker";
            ((System.ComponentModel.ISupportInitialize)(this.redUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.greenUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.blueUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.saturationUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.hueUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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

            this.UserPrimaryColor = color;

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

            this.hueGradientControl.CustomGradient = hueColors;

            Color[] satColors = new Color[101];

            for (int newS = 0; newS <= 100; ++newS)
            {
                HsvColor hsv = new HsvColor(h, newS, v);
                satColors[newS] = hsv.ToColor();
            }

            this.saturationGradientControl.CustomGradient = satColors;

            this.valueGradientControl.MaxColor = new HsvColor(h, s, 100).ToColor();
            this.valueGradientControl.MinColor = new HsvColor(h, s, 0).ToColor();
        }

        private void SetColorGradientMinMaxColorsRgb(int r, int g, int b)
        {
            this.redGradientControl.MaxColor = Color.FromArgb(255, g, b);
            this.redGradientControl.MinColor = Color.FromArgb(0, g, b);
            this.greenGradientControl.MaxColor = Color.FromArgb(r, 255, b);
            this.greenGradientControl.MinColor = Color.FromArgb(r, 0, b);
            this.blueGradientControl.MaxColor = Color.FromArgb(r, g, 255);
            this.blueGradientControl.MinColor = Color.FromArgb(r, g, 0);
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

                    this.UserPrimaryColor = rgbColor;
                }
                else if (sender == hexBox)
                {
                    int hexInt = 0;

                    if (hexBox.Text.Length > 0)
                    {
                        try
                        {
                            hexInt = int.Parse(hexBox.Text,System.Globalization.NumberStyles.HexNumber);
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
                    this.UserPrimaryColor = rgbColor;
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
                        this.UserPrimaryColor = ColorBgra.FromBgra((byte)rgbColor.Blue, (byte)rgbColor.Green, (byte)rgbColor.Red, 255);
                    }
                }
                PopIgnoreChangedEvents();

            }
        }

        private void PushIgnoreChangedEvents()
        {
            ++this.ignoreChangedEvents;
        }

        private void PopIgnoreChangedEvents()
        {
            --this.ignoreChangedEvents;
        }

        private ColorBgra GetColorFromUpDowns()
        {
            int r = (int)this.redUpDown.Value;
            int g = (int)this.greenUpDown.Value;
            int b = (int)this.blueUpDown.Value;

            return ColorBgra.FromBgra((byte)b, (byte)g, (byte)r, 255);
        }

        private void swatchControl_ColorClicked(object sender, IndexEventArgs e)
        {
            this.UserPrimaryColor = paletteColors[e.Index];
        }

        private void okBtn_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

       

    }
}
