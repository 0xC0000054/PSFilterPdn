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

using System;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace PSFilterPdn
{
    [Serializable]
    public sealed class TreeNodeEx : TreeNode
    {
        private const string EnabledSerializationName = "TreeNodeEx_Enabled";

        public TreeNodeEx() : base()
        {
            Enabled = true;
        }

        public TreeNodeEx(string text) : base(text)
        {
            Enabled = true;
        }

        public TreeNodeEx(string text, TreeNode[] children) : base(text, children)
        {
            Enabled = true;
        }

        public bool Enabled
        {
            get;
            set;
        }

        public override object Clone()
        {
            TreeNodeEx treeNode = (TreeNodeEx)base.Clone();
            treeNode.Enabled = Enabled;

            return treeNode;
        }

        protected override void Deserialize(SerializationInfo serializationInfo, StreamingContext context)
        {
            base.Deserialize(serializationInfo, context);

            foreach (SerializationEntry item in serializationInfo)
            {
                if (string.Equals(item.Name, EnabledSerializationName, StringComparison.Ordinal))
                {
                    Enabled = (bool)item.Value;
                }
            }
        }

        protected override void Serialize(SerializationInfo si, StreamingContext context)
        {
            base.Serialize(si, context);

            si.AddValue(EnabledSerializationName, Enabled);
        }
    }
}
