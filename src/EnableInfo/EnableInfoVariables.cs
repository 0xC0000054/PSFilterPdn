/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi;
using System;
using System.ComponentModel;

namespace PSFilterPdn.EnableInfo
{
    internal sealed class EnableInfoVariables : IEquatable<EnableInfoVariables>
    {
        private readonly string imageMode;
        private readonly int imageDepth;
        private readonly bool hasLayerMask;
        private readonly bool hasSelectionMask;
        private readonly bool hasTransparencyMask;
        private readonly int targetChannelCount;
        private readonly int trueChannelCount;
        private readonly bool isTargetComposite;
        private readonly int imageWidth;
        private readonly int imageHeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableInfoVariables"/> class.
        /// </summary>
        /// <param name="imageWidth">The width of the image in pixels.</param>
        /// <param name="imageHeight">The height of the image in pixels.</param>
        /// <param name="imageMode">The image mode.</param>
        /// <param name="hasTransparencyMask"><c>true</c> if the image has transparency; otherwise, <c>false</c>.</param>
        /// <param name="targetChannelCount">The target channel count.</param>
        /// <param name="trueChannelCount">The true channel count.</param>
        /// <param name="hostState">The current host state.</param>
        /// <exception cref="ArgumentNullException"><paramref name="hostState"/> is null.</exception>
        /// <exception cref="InvalidEnumArgumentException"><paramref name="imageMode"/> is not a supported value.</exception>
        public EnableInfoVariables(int imageWidth,
                                   int imageHeight,
                                   ImageMode imageMode,
                                   bool hasTransparencyMask,
                                   int targetChannelCount,
                                   int trueChannelCount,
                                   HostState hostState)
        {
            if (hostState == null)
            {
                throw new ArgumentNullException(nameof(hostState));
            }

            switch (imageMode)
            {
                case ImageMode.Bitmap:
                    this.imageMode = "BitmapMode";
                    imageDepth = 1;
                    break;
                case ImageMode.GrayScale:
                    this.imageMode = "GrayScaleMode";
                    imageDepth = 8;
                    break;
                case ImageMode.Indexed:
                    this.imageMode = "IndexedMode";
                    imageDepth = 8;
                    break;
                case ImageMode.RGB:
                    this.imageMode = "RGBMode";
                    imageDepth = 8;
                    break;
                case ImageMode.CMYK:
                    this.imageMode = "CMYKMode";
                    imageDepth = 8;
                    break;
                case ImageMode.HSL:
                    this.imageMode = "HSLMode";
                    imageDepth = 8;
                    break;
                case ImageMode.HSB:
                    this.imageMode = "HSBMode";
                    imageDepth = 8;
                    break;
                case ImageMode.Multichannel:
                    this.imageMode = "MultichannelMode";
                    imageDepth = 8;
                    break;
                case ImageMode.Duotone:
                    this.imageMode = "DuotoneMode";
                    imageDepth = 8;
                    break;
                case ImageMode.Lab:
                    this.imageMode = "LabMode";
                    imageDepth = 8;
                    break;
                case ImageMode.Gray16:
                    this.imageMode = "Gray16Mode";
                    imageDepth = 16;
                    break;
                case ImageMode.RGB48:
                    this.imageMode = "RGB48Mode";
                    imageDepth = 16;
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(imageMode), (int)imageMode, typeof(ImageMode));
            }

            // Layer masks are not supported.
            hasLayerMask = false;
            hasSelectionMask = hostState.HasSelection;
            this.hasTransparencyMask = hasTransparencyMask;
            this.targetChannelCount = targetChannelCount;
            this.trueChannelCount = trueChannelCount;
            isTargetComposite = !hostState.HasMultipleLayers;
            this.imageWidth = imageWidth;
            this.imageHeight = imageHeight;
        }

        /// <summary>
        /// Gets the value associated with the specified variable name.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <returns>
        /// A <see cref="ConstantExpression"/> containing the value the specified variable.
        /// </returns>
        public ConstantExpression GetValue(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (name.Equals("PSHOP_ImageMode", StringComparison.OrdinalIgnoreCase))
                {
                    return new StringConstantExpression(imageMode);
                }
                else if (name.Equals("PSHOP_ImageDepth", StringComparison.OrdinalIgnoreCase))
                {
                    return new IntegerConstantExpression(imageDepth);
                }
                else if (name.Equals("PSHOP_HasLayerMask", StringComparison.OrdinalIgnoreCase))
                {
                    return new BooleanConstantExpression(hasLayerMask);
                }
                else if (name.Equals("PSHOP_HasSelectionMask", StringComparison.OrdinalIgnoreCase))
                {
                    return new BooleanConstantExpression(hasSelectionMask);
                }
                else if (name.Equals("PSHOP_HasTransparencyMask", StringComparison.OrdinalIgnoreCase))
                {
                    return new BooleanConstantExpression(hasTransparencyMask);
                }
                else if (name.Equals("PSHOP_NumTargetChannels", StringComparison.OrdinalIgnoreCase))
                {
                    return new IntegerConstantExpression(targetChannelCount);
                }
                else if (name.Equals("PSHOP_NumTrueChannels", StringComparison.OrdinalIgnoreCase))
                {
                    return new IntegerConstantExpression(trueChannelCount);
                }
                else if (name.Equals("PSHOP_IsTargetComposite", StringComparison.OrdinalIgnoreCase))
                {
                    return new BooleanConstantExpression(isTargetComposite);
                }
                else if (name.Equals("PSHOP_ImageWidth", StringComparison.OrdinalIgnoreCase))
                {
                    return new IntegerConstantExpression(imageWidth);
                }
                else if (name.Equals("PSHOP_ImageHeight", StringComparison.OrdinalIgnoreCase))
                {
                    return new IntegerConstantExpression(imageHeight);
                }
            }

            return new UndefinedVariableConstantExpression(name);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as EnableInfoVariables);
        }

        public bool Equals(EnableInfoVariables? other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(imageMode, other.imageMode, StringComparison.Ordinal) &&
                   imageDepth == other.imageDepth &&
                   hasLayerMask == other.hasLayerMask &&
                   hasSelectionMask == other.hasSelectionMask &&
                   hasTransparencyMask == other.hasTransparencyMask &&
                   targetChannelCount == other.targetChannelCount &&
                   trueChannelCount == other.trueChannelCount &&
                   isTargetComposite == other.isTargetComposite &&
                   imageWidth == other.imageWidth &&
                   imageHeight == other.imageHeight;
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new();

            hashCode.Add(imageMode);
            hashCode.Add(imageDepth);
            hashCode.Add(hasLayerMask);
            hashCode.Add(hasSelectionMask);
            hashCode.Add(hasTransparencyMask);
            hashCode.Add(targetChannelCount);
            hashCode.Add(trueChannelCount);
            hashCode.Add(isTargetComposite);
            hashCode.Add(imageWidth);
            hashCode.Add(imageHeight);

            return hashCode.ToHashCode();
        }

        public static bool operator ==(EnableInfoVariables? variables1, EnableInfoVariables? variables2)
        {
            if (ReferenceEquals(variables1, variables2))
            {
                return true;
            }

            if (((object?)variables1) == null || ((object?)variables2) == null)
            {
                return false;
            }

            return variables1.Equals(variables2);
        }

        public static bool operator !=(EnableInfoVariables? variables1, EnableInfoVariables? variables2)
        {
            return !(variables1 == variables2);
        }
    }
}
