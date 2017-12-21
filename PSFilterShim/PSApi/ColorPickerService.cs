/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;

namespace PSFilterLoad.PSApi
{
    internal static class ColorPickerService
    {
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
