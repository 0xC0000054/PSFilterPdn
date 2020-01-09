/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet.Effects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace PSFilterPdn
{
    internal static class PluginThemingUtil
    {
        private static Action<EffectConfigDialog, bool> useAppThemeSetter;
        private static bool initAppThemeSetter = false;

        public static void EnableEffectDialogTheme(EffectConfigDialog dialog)
        {
            try
            {
                if (!initAppThemeSetter)
                {
                    initAppThemeSetter = true;

                    PropertyInfo propertyInfo = typeof(EffectConfigDialog).GetProperty("UseAppThemeColors", BindingFlags.Public | BindingFlags.Instance);
                    if (propertyInfo != null)
                    {
                        useAppThemeSetter = (Action<EffectConfigDialog, bool>)Delegate.CreateDelegate(typeof(Action<EffectConfigDialog, bool>), propertyInfo.GetSetMethod());
                    }
                }

                useAppThemeSetter?.Invoke(dialog, true);
            }
            catch
            {
                // Ignore any exceptions that are thrown when trying to enable the dialog theming.
                // The dialog should be shown to the user even if theming could not be enabled.
            }
        }

        public static void UpdateControlBackColor(Control root)
        {
            Color backColor = root.BackColor;

            Stack<Control> stack = new Stack<Control>();
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

                    control.BackColor = backColor;

                    if (control.HasChildren)
                    {
                        stack.Push(control);
                    }
                }
            }
        }

        public static void UpdateControlForeColor(Control root)
        {
            Color foreColor = root.ForeColor;

            Stack<Control> stack = new Stack<Control>();
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
