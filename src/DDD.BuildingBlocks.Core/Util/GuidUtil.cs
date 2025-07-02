using System;

namespace DDD.BuildingBlocks.Core.Util
{
    /// <summary>
    ///     Utility class for GUID handling with other representations.
    /// </summary>
    public static class GuidUtil
    {
        /// <summary>
        ///     Converts a raw guid (sequence of bytes) to a .net representation.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string RawToDotNet(string text)
        {
            var bytes = ParseHex(text);
            var guid = new Guid(bytes);
            return guid.ToString("N").ToUpperInvariant();
        }

        /// <summary>
        ///     Converts a .net guid to a raw byte sequence
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string DotNetToRaw(string text)
        {
            var guid = new Guid(text);
            return BitConverter.ToString(guid.ToByteArray()).Replace("-", "", StringComparison.InvariantCulture);
        }

        private static byte[] ParseHex(string text)
        {
            var ret = new byte[text.Length / 2];
            for (var i = 0; i < ret.Length; i++)
            {
                ret[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
            }

            return ret;
        }
    }
}
