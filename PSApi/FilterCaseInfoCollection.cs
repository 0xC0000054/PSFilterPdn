/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PSFilterLoad.PSApi
{
    [Serializable]
    public sealed class FilterCaseInfoCollection : ReadOnlyCollection<FilterCaseInfo>
    {
        public FilterCaseInfoCollection(IList<FilterCaseInfo> list) : base(list)
        {
        }

        public FilterCaseInfo this[FilterCase filterCase] => this[(int)filterCase - 1];
    }
}
