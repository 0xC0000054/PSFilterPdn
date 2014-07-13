/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2014 Nicholas Hayes
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
        private SortOrder order;
        public TreeNodeItemComparer()
        {
            this.order = SortOrder.Ascending;
        }

        public int Compare(object x, object y)
        {
            int returnVal = -1;
            returnVal = StringLogicalComparer.Compare(((TreeNode)x).Text,
                                    ((TreeNode)y).Text);
            // Determine whether the sort order is descending.
            if (order == SortOrder.Descending)
                // Invert the value returned by String.Compare.
                returnVal *= -1;
            return returnVal;
        }
    }
}
