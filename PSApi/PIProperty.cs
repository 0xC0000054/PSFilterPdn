using System;

namespace PSFilterLoad.PSApi
{
// Disable CS0649, Field 'field' is never assigned to, and will always have its default value 'value'
#pragma warning disable 0649 

    struct PIProperty
    {
        public uint vendorID;  /* Vendor specific identifier. */
        public uint propertyKey;		/* Identification key for this property type. */
	    public int propertyID;		/* Index within this property type. Must be unique for properties of a given type in a PiPL. */
	    public int propertyLength;	/* Length of following data array. Will be rounded to a multiple of 4. */
        public IntPtr propertyData;
    }
#pragma warning restore 0649

    enum PIPropertyID : uint
    {
        /// <summary>
        /// The property giving the plug-in's kind, 8BFM for Photoshop Filters
        /// </summary>
        PIKindProperty = 0x6b696e64U,
        /// <summary>
        /// Win32 Intel code descriptor, Entrypoint
        /// </summary>
        PIWin32X86CodeProperty = 0x77783836U,
        /// <summary>
        /// Win64 Intel code descriptor, Entrypoint 
        /// </summary>
        /// <remarks>Taken from the PiPL resources of a 64-bit Photoshop Plugin.</remarks>
        PIWin64X86CodeProperty = 0x38363634U,
        /// <summary>
        /// Major(int16).Minor(int16) version number
        /// </summary>
        PIVersionProperty = 0x76657273U,
        /// <summary>
        /// Image modes supported flags. (bitmask)
        /// </summary>
        PIImageModesProperty = 0x6d6f6465U,
        /// <summary>
        /// Category name that appears on top level menu
        /// </summary>
        PICategoryProperty = 0x63617467U,
        /// <summary>
        /// Menu name
        /// </summary>
        PINameProperty = 0x6e616d65U,
#if PSSDK4
        /// <summary>
        /// Has Terminology Property
        /// </summary>
        PIHasTerminologyProperty = 0x6873746DU,
#endif
        /// <summary>
        /// FilterCaseInfo Property
        /// </summary>
        PIFilterCaseInfoProperty = 0x66696369U

    }
    
}
