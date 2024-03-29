﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PSFilterPdn
{
    internal static class PluginThemingUtil
    {
        public static bool IsDarkMode(Color backColor)
        {
            return backColor.R < 128 && backColor.G < 128 && backColor.B < 128;
        }

        public static void UpdateControlBackColor(Control root)
        {
            Color backColor = root.BackColor;

            Stack<Control> stack = new();
            stack.Push(root);

            while (stack.Count > 0)
            {
                Control parent = stack.Pop();

                Control.ControlCollection controls = parent.Controls;

                for (int i = 0; i < controls.Count; i++)
                {
                    Control control = controls[i];

                    // Update the BackColor for all child controls as some controls
                    // do not change the BackColor when the parent control does.

                    if (control is Button)
                    {
                        control.BackColor = Control.DefaultBackColor;
                    }
                    else
                    {
                        control.BackColor = backColor;

                        if (control.HasChildren)
                        {
                            stack.Push(control);
                        }
                    }
                }
            }
        }

        public static void UpdateControlForeColor(Control root)
        {
            Color foreColor = root.ForeColor;

            Stack<Control> stack = new();
            stack.Push(root);

            while (stack.Count > 0)
            {
                Control parent = stack.Pop();

                Control.ControlCollection controls = parent.Controls;

                for (int i = 0; i < controls.Count; i++)
                {
                    Control control = controls[i];

                    if (control is LinkLabel link)
                    {
                        if (foreColor != Control.DefaultForeColor)
                        {
                            link.LinkColor = foreColor;
                        }
                        else
                        {
                            // If the control is using the default foreground color set the link color
                            // to Color.Empty so the LinkLabel will use its default colors.
                            link.LinkColor = Color.Empty;
                        }
                    }
                    else if (control is Button)
                    {
                        control.ForeColor = Control.DefaultForeColor;
                    }
                    else
                    {
                        // Update the ForeColor for all child controls as some controls
                        // do not change the ForeColor when the parent control does.

                        control.ForeColor = foreColor;

                        if (control.HasChildren)
                        {
                            stack.Push(control);
                        }
                    }
                }
            }
        }
    }
}
