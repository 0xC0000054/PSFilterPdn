using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
    static class DescriptorTypes
    {
        public const uint typeAlias = 0x976c6973U; // 'alis'
        public const uint typeBoolean = 0x626f6f6cU; // 'bool'
        public const uint typeChar = 0x54455854U; // 'TEXT'
        public const uint typeClass = 0x74797065U; // 'type'
        public const uint typeFloat = 0x646f7562U; // 'doub'
        public const uint typeInteger = 0x6c6f6e67U; // 'long'
        public const uint typeNull = 0x6e756c6cU; // 'null'
        public const uint typeObjectRefrence = 0x6f626a20U; // 'obj '
        public const uint typePath = 0x50746820U; // 'Pat '
        public const uint typeUintFloat = 0x556e7446; // 'UntF'

        public const uint classRGBColor = 0x52474243U; // 'RGBC'
        public const uint classCMYKColor = 0x434d5943U; // 'CMYC'
        public const uint classGrayscale = 0x47727363U; // 'Grsc'
        public const uint classLabColor = 0x4c62436cU; // 'LbCl'
        public const uint classHSBColor = 0x48534243U; // 'HSBC'
    }
}
