/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace PSFilterLoad.ColorPicker
{
   

    /// <summary>
    /// Contains static methods related to the user interface.
    /// </summary>
    internal static class UI
    {
        private static bool initScales = false;
        private static float xScale;
        private static float yScale;

        private static void InitScaleFactors(Control c)
        {
            if (c == null)
            {
                xScale = 1.0f;
                yScale = 1.0f;
            }
            else
            {
                using (Graphics g = c.CreateGraphics())
                {
                    xScale = g.DpiX / 96.0f;
                    yScale = g.DpiY / 96.0f;
                }
            }

            initScales = true;
        }

        public static void InitScaling(Control c)
        {
            if (!initScales)
            {
                InitScaleFactors(c);
            }
        }

        /// <summary>
        /// Sets the control's redraw state.
        /// </summary>
        /// <param name="control">The control whose state should be modified.</param>
        /// <param name="enabled">The new state for redrawing ability.</param>
        /// <remarks>
        /// Note to implementors: This method is used by SuspendControlPainting() and ResumeControlPainting().
        /// This may be implemented as a no-op.
        /// </remarks>
        private static void SetControlRedrawImpl(Control control, bool enabled)
        {
            PSFilterLoad.PSApi.SafeNativeMethods.SendMessageW(control.Handle, PSFilterLoad.PSApi.NativeConstants.WM_SETREDRAW, enabled ? new IntPtr(1) : IntPtr.Zero, IntPtr.Zero);
            GC.KeepAlive(control);
        }

        private static Dictionary<Control, int> controlRedrawStack = new Dictionary<Control, int>();

        /// <summary>
        /// Suspends the control's ability to draw itself.
        /// </summary>
        /// <param name="control">The control to suspend drawing for.</param>
        /// <remarks>
        /// When drawing is suspended, any painting performed in the control's WM_PAINT, OnPaint(),
        /// WM_ERASEBKND, or OnPaintBackground() handlers is completely ignored. Invalidation rectangles
        /// are not accumulated during this period, so when drawing is resumed (with 
        /// ResumeControlPainting()), it is usually a good idea to call Invalidate(true) on the control.
        /// This method must be matched at a later time by a corresponding call to ResumeControlPainting().
        /// If you call SuspendControlPainting() multiple times for the same control, then you must
        /// call ResumeControlPainting() once for each call.
        /// Note to implementors: Do not modify this method. Instead, modify SetControlRedrawImpl(),
        /// which may be implemented as a no-op.
        /// </remarks>
        public static void SuspendControlPainting(Control control)
        {
            int pushCount;

            if (controlRedrawStack.TryGetValue(control, out pushCount))
            {
                ++pushCount;
            }
            else
            {
                pushCount = 1;
            }

            if (pushCount == 1)
            {
                SetControlRedrawImpl(control, false);
            }

            controlRedrawStack[control] = pushCount;
        }

        /// <summary>
        /// Resumes the control's ability to draw itself.
        /// </summary>
        /// <param name="control">The control to suspend drawing for.</param>
        /// <remarks>
        /// This method must be matched by a preceding call to SuspendControlPainting(). If that method
        /// was called multiple times, then this method must be called a corresponding number of times
        /// in order to enable drawing.
        /// This method must be matched at a later time by a corresponding call to ResumeControlPainting().
        /// If you call SuspendControlPainting() multiple times for the same control, then you must
        /// call ResumeControlPainting() once for each call.
        /// Note to implementors: Do not modify this method. Instead, modify SetControlRedrawImpl(),
        /// which may be implemented as a no-op.
        /// </remarks>        
        public static void ResumeControlPainting(Control control)
        {
            int pushCount;

            if (controlRedrawStack.TryGetValue(control, out pushCount))
            {
                --pushCount;
            }
            else
            {
                throw new InvalidOperationException("There was no previous matching SuspendControlPainting() for this control");
            }

            if (pushCount == 0)
            {
                SetControlRedrawImpl(control, true);
                controlRedrawStack.Remove(control);
            }
            else
            {
                controlRedrawStack[control] = pushCount;
            }
        }

      
        private static VisualStyleClass DetermineVisualStyleClassImpl()
        {
            VisualStyleClass vsClass;

            try
            {
                if (!VisualStyleInformation.IsSupportedByOS)
                {
                    vsClass = VisualStyleClass.Classic;
                }
                else if (!VisualStyleInformation.IsEnabledByUser)
                {
                    vsClass = VisualStyleClass.Classic;
                }
                else if (0 == string.Compare(VisualStyleInformation.Author, "MSX", StringComparison.InvariantCulture) &&
                         0 == string.Compare(VisualStyleInformation.DisplayName, "Aero style", StringComparison.InvariantCulture))
                {
                    vsClass = VisualStyleClass.Aero;
                }
                else if (0 == string.Compare(VisualStyleInformation.Company, "Microsoft Corporation", StringComparison.InvariantCulture) &&
                         0 == string.Compare(VisualStyleInformation.Author, "Microsoft Design Team", StringComparison.InvariantCulture))
                {
                    if (0 == string.Compare(VisualStyleInformation.DisplayName, "Windows XP style", StringComparison.InvariantCulture) ||  // Luna
                        0 == string.Compare(VisualStyleInformation.DisplayName, "Zune Style", StringComparison.InvariantCulture) ||        // Zune
                        0 == string.Compare(VisualStyleInformation.DisplayName, "Media Center style", StringComparison.InvariantCulture))  // Royale
                    {
                        vsClass = VisualStyleClass.Luna;
                    }
                    else
                    {
                        vsClass = VisualStyleClass.Other;
                    }
                }
                else
                {
                    vsClass = VisualStyleClass.Other;
                }
            }
            catch (Exception)
            {
                vsClass = VisualStyleClass.Other;
            }

            return vsClass;
        }

        public static VisualStyleClass VisualStyleClass
        {
            get
            {
                return DetermineVisualStyleClassImpl();
            }
        }

        public static float GetXScaleFactor()
        {
            if (!initScales)
            {
                throw new InvalidOperationException("Must call InitScaling() first");
            }

            return xScale;
        }

        public static float GetYScaleFactor()
        {
            if (!initScales)
            {
                throw new InvalidOperationException("Must call InitScaling() first");
            }

            return yScale;
        }

        public static int ScaleWidth(int width)
        {
            return (int)Math.Round((float)width * GetXScaleFactor());
        }

    }
}
