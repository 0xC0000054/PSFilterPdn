using System.Globalization;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal readonly struct Win32ErrorCodeHexStringFormatter
    {
        private readonly int value;

        public Win32ErrorCodeHexStringFormatter(int value) => this.value = value;

        public override string ToString() => value.ToString("X8", CultureInfo.InvariantCulture);
    }
}
