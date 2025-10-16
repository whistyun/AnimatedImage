// Copyright (c) 2013 Amemiya
// Licensed under the MIT License.
// Ported from: https://github.com/xupefei/APNG.NET

using System;
using System.IO;

namespace AnimatedImage.Formats.Png
{
    internal static class StreamExtensions
    {
        #region Peek

        public static byte[] PeekBytes(this Stream ms, int position, int count)
        {
            long prevPosition = ms.Position;

            ms.Position = position;
            byte[] buffer = ms.ReadBytes(count);
            ms.Position = prevPosition;

            return buffer;
        }

        public static char PeekChar(this Stream ms)
        {
            return ms.PeekChar((int)ms.Position);
        }

        public static char PeekChar(this Stream ms, int position)
        {
            return BitConverter.ToChar(ms.PeekBytes(position, 2), 0);
        }

        public static short PeekInt16(this Stream ms)
        {
            return ms.PeekInt16((int)ms.Position);
        }

        public static short PeekInt16(this Stream ms, int position)
        {
            return BitConverter.ToInt16(ms.PeekBytes(position, 2), 0);
        }

        public static int PeekInt32(this Stream ms)
        {
            return ms.PeekInt32((int)ms.Position);
        }

        public static int PeekInt32(this Stream ms, int position)
        {
            return BitConverter.ToInt32(ms.PeekBytes(position, 4), 0);
        }

        public static long PeekInt64(this Stream ms)
        {
            return ms.PeekInt64((int)ms.Position);
        }

        public static long PeekInt64(this Stream ms, int position)
        {
            return BitConverter.ToInt64(ms.PeekBytes(position, 8), 0);
        }

        public static ushort PeekUInt16(this Stream ms)
        {
            return ms.PeekUInt16((int)ms.Position);
        }

        public static ushort PeekUInt16(this Stream ms, int position)
        {
            return BitConverter.ToUInt16(ms.PeekBytes(position, 2), 0);
        }

        public static uint PeekUInt32(this Stream ms)
        {
            return ms.PeekUInt32((int)ms.Position);
        }

        public static uint PeekUInt32(this Stream ms, int position)
        {
            return BitConverter.ToUInt32(ms.PeekBytes(position, 4), 0);
        }

        public static ulong PeekUInt64(this Stream ms)
        {
            return ms.PeekUInt64((int)ms.Position);
        }

        public static ulong PeekUInt64(this Stream ms, int position)
        {
            return BitConverter.ToUInt64(ms.PeekBytes(position, 8), 0);
        }

        #endregion Peek

        #region Read

        public static byte[] ReadBytes(this Stream ms, int count)
        {
            var buffer = new byte[count];

            if (ms.Read(buffer, 0, count) != count)
                throw new PngDecoderException("End reached.");

            return buffer;
        }

        #endregion Read

        #region Write

        public static void WriteByte(this Stream ms, int position, byte value)
        {
            long prevPosition = ms.Position;

            ms.Position = position;
            ms.WriteByte(value);
            ms.Position = prevPosition;
        }

        public static void WriteBytes(this Stream ms, byte[] value)
        {
            ms.Write(value, 0, value.Length);
        }

        public static void WriteBytes(this Stream ms, int position, byte[] value)
        {
            long prevPosition = ms.Position;

            ms.Position = position;
            ms.Write(value, 0, value.Length);
            ms.Position = prevPosition;
        }

        public static void WriteInt16(this Stream ms, short value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 2);
        }

        public static void WriteInt16(this Stream ms, int position, short value)
        {
            ms.WriteBytes(position, BitConverter.GetBytes(value));
        }

        public static void WriteInt32(this Stream ms, int value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public static void WriteInt32(this Stream ms, int position, int value)
        {
            ms.WriteBytes(position, BitConverter.GetBytes(value));
        }

        public static void WriteInt64(this Stream ms, long value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 8);
        }

        public static void WriteInt64(this Stream ms, int position, long value)
        {
            ms.WriteBytes(position, BitConverter.GetBytes(value));
        }

        public static void WriteUInt16(this Stream ms, ushort value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 2);
        }

        public static void WriteUInt16(this Stream ms, int position, ushort value)
        {
            ms.WriteBytes(position, BitConverter.GetBytes(value));
        }

        public static void WriteUInt32(this Stream ms, uint value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public static void WriteUInt32(this Stream ms, int position, uint value)
        {
            ms.WriteBytes(position, BitConverter.GetBytes(value));
        }

        public static void WriteUInt64(this Stream ms, ulong value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 8);
        }

        public static void WriteUInt64(this Stream ms, int position, ulong value)
        {
            ms.WriteBytes(position, BitConverter.GetBytes(value));
        }

        #endregion Write
    }
}