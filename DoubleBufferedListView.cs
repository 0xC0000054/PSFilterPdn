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

using System.Windows.Forms;

namespace PSFilterPdn
{
    internal sealed class DoubleBufferedListView : ListView
    {
        public DoubleBufferedListView() : base()
        {
            this.DoubleBuffered = true;
        }
    }
}
