namespace PSFilterLoad.PSApi
{
#if DEBUG
    static class Utility
    {
        /// <summary>
        /// Prints a Rect16 structure for debugging 
        /// </summary>
        /// <param name="rect">The Rect16 rectangle to print</param>
        /// <returns>The rectangle bounds printed as a string</returns>
        public static string RectToString(Rect16 rect)
        {
            return ("Top=" + rect.top.ToString() + ",Bottom=" + rect.bottom.ToString() + ",Left=" + rect.left.ToString() + ",Right=" + rect.right.ToString());
        }
        /// <summary>
        /// Prints a VRect structure for debugging 
        /// </summary>
        /// <param name="rect">The VRect rectangle to print</param>
        /// <returns>The rectangle bounds printed as a string</returns>
        public static string RectToString(VRect rect)
        {
            return ("Top=" + rect.top.ToString() + ",Bottom=" + rect.bottom.ToString() + ",Left=" + rect.left.ToString() + ",Right=" + rect.right.ToString());
        }
       
    }
#endif
}
