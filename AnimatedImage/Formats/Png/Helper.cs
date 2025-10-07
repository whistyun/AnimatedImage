// Copyright (c) 2013 Amemiya
// Licensed under the MIT License.
// Ported from: https://github.com/xupefei/APNG.NET

using System;

namespace AnimatedImage.Formats.Png
{
    internal class Helper
    {
        /// <summary>
        ///     Convert big-endian to little-endian or reserve
        /// </summary>
        internal static byte[] ConvertEndian(byte[] i)
        {
            if (i.Length % 2 != 0)
                throw new PngDecoderException("byte array length must multiply of 2");

            Array.Reverse(i);

            return i;
        }

        /// <summary>
        ///     Convert big-endian to little-endian or reserve
        /// </summary>
        internal static int ConvertEndian(int i)
        {
            return BitConverter.ToInt32(ConvertEndian(BitConverter.GetBytes(i)), 0);
        }

        /// <summary>
        ///     Convert big-endian to little-endian or reserve
        /// </summary>
        internal static uint ConvertEndian(uint i)
        {
            return BitConverter.ToUInt32(ConvertEndian(BitConverter.GetBytes(i)), 0);
        }

        /// <summary>
        ///     Convert big-endian to little-endian or reserve
        /// </summary>
        internal static short ConvertEndian(short i)
        {
            return BitConverter.ToInt16(ConvertEndian(BitConverter.GetBytes(i)), 0);
        }

        /// <summary>
        ///     Convert big-endian to little-endian or reserve
        /// </summary>
        internal static ushort ConvertEndian(ushort i)
        {
            return BitConverter.ToUInt16(ConvertEndian(BitConverter.GetBytes(i)), 0);
        }

        /// <summary>
        ///     Compare two byte array
        /// </summary>
        public static bool IsBytesEqual(byte[] byte1, byte[] byte2)
        {
            if (byte1.Length != byte2.Length)
                return false;

            for (int i = 0; i < byte1.Length; i++)
            {
                if (byte1[i] != byte2[i])
                    return false;
            }
            return true;
        }
    }
}