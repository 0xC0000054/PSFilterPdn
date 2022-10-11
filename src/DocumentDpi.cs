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

using PaintDotNet;
using System;
using System.Diagnostics.CodeAnalysis;

namespace PSFilterPdn
{
    internal readonly struct DocumentDpi : IEquatable<DocumentDpi>
    {
        public DocumentDpi(Resolution resolution)
        {
            switch (resolution.Units)
            {
                case MeasurementUnit.Inch:
                    X = resolution.X;
                    Y = resolution.Y;
                    break;
                case MeasurementUnit.Centimeter:
                    X = Document.CentimetersToInches(resolution.X);
                    Y = Document.CentimetersToInches(resolution.Y);
                    break;
                case MeasurementUnit.Pixel:
                default:
                    X = Y = Document.DefaultDpi;
                    break;
            }
        }

        public double X { get; }

        public double Y { get; }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            return obj is DocumentDpi other && Equals(other);
        }

        public bool Equals(DocumentDpi other)
        {
            return X == other.X && Y == other.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(DocumentDpi left, DocumentDpi right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DocumentDpi left, DocumentDpi right)
        {
            return !left.Equals(right);
        }
    }
}
