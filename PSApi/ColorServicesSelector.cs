namespace PSFilterLoad.PSApi
{

    static class ColorServicesSelector
    {
        /// plugIncolorServicesChooseColor -> 0
        public const short plugIncolorServicesChooseColor = 0;

        /// plugIncolorServicesConvertColor -> 1
        public const short plugIncolorServicesConvertColor = 1;

#if PSSDK_3_0_4 
        /// plugIncolorServicesSamplePoint -> 2
        public const short plugIncolorServicesSamplePoint = 2;

        /// plugIncolorServicesGetSpecialColor -> 3
        public const short plugIncolorServicesGetSpecialColor = 3; 
#endif
    }
}
