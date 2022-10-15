/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Collections;
using System.Windows.Forms;

namespace PSFilterPdn
{
    internal sealed class TreeNodeItemComparer : IComparer
    {
        private static readonly TreeNodeItemComparer instance = new();

        private TreeNodeItemComparer()
        {
        }

        public static TreeNodeItemComparer Instance => instance;

        public int Compare(object x, object y)
        {
            if (ReferenceEquals(x, y))
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
