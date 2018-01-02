/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using System.Drawing;
using System.Windows.Forms;

namespace PSFilterLoad.PSApi
{
    internal static class ColorPickerService
    {
        private static Color formBackColor = Control.DefaultBackColor;
        private static Color formForeColor = Control.DefaultForeColor;

        /// <summary>
        /// Sets the background and foreground colors of the dialog.
        /// </summary>
        /// <param name="pluginUISettings">The plug-in UI settings.</param>
        public static void SetDialogColors(PluginUISettings pluginUISettings)
        {
            if (pluginUISettings != null)
            {
                formBackColor = pluginUISettings.ColorPickerBackColor;
                formForeColor = pluginUISettings.ColorPickerForeColor;
            }
        }

        /// <summary>
        /// Shows the color picker dialog.
        /// </summary>
        /// <param name="prompt">The prompt for the user.</param>
        /// <returns>The user's chosen color; or null if the user canceled the dialog.</returns>
        public static ColorBgra? ShowColorPickerDialog(string prompt)
        {
            return ShowColorPickerDialog(prompt, 0, 0, 0);
        }

        /// <summary>
        /// Shows the color picker dialog, with the specified initial color.
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="red">The red component of the initial color.</param>
        /// <param name="green">The green component of the initial color.</param>
        /// <param name="blue">The blue component of the initial color.</param>
        /// <returns>The user's chosen color; or null if the user canceled the dialog.</returns>
        public static ColorBgra? ShowColorPickerDialog(string prompt, short red, short green, short blue)
        {
            ColorBgra? chosenColor = null;

            using (ColorPickerForm form = new ColorPickerForm(prompt))
            {
                form.BackColor = formBackColor;
                form.ForeColor = formForeColor;
                form.SetDefaultColor(red, green, blue);

                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    chosenColor = form.UserPrimaryColor;
                }
            }

            return chosenColor;
        }
    }
}
