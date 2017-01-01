/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Windows.Forms;

namespace PSFilterPdn
{
    internal sealed class TreeNodeItemComparer : IComparer
    {
        public TreeNodeItemComparer()
        {
        }

        public int Compare(object x, object y)
        {
            if (Object.ReferenceEquals(x, y))
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }

            return StringLogicalComparer.Compare(((TreeNode)x).Text, ((TreeNode)y).Text);
        }
    }
}
