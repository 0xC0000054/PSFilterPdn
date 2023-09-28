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

namespace PSFilterLoad.PSApi
{
    internal static class ColorPickerService
    {
        /// <summary>
        /// Shows the color picker dialog.
        /// </summary>
        /// <param name="prompt">The prompt for the user.</param>
        /// <returns>The user's chosen color; or null if the user canceled the dialog.</returns>
        public static ColorRgb24? ShowColorPickerDialog(string prompt)
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
        public static ColorRgb24? ShowColorPickerDialog(string prompt, byte red, byte green, byte blue)
        {
            ColorRgb24? chosenColor = null;

            ColorPickerDialog dialog = new(prompt)
            {
                Color = new ColorRgb24(red, green, blue)
            };

            if (dialog.ShowDialog())
            {
                chosenColor = dialog.Color;
            }

            return chosenColor;
        }
    }
}
