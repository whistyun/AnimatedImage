using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimatedImage.Formats.Png
{
    /// <summary>
    /// The class for simplifying the reading of Chunk data.
    /// This provides digits that are converted from BigEndian(PNG) to LittleEndian(.NET).
    /// </summary>
    internal class ChunkStream
    {
        private Stream _stream;
        private string _chunkType = string.Empty;
        private uint _leave;
        private bool _isCrcRead = true;

        public ChunkStream(Stream stream)
        {
            _stream = stream;
        }

        public string ChunkType => _chunkType;

        public uint Length { get; private set; }

        public string? ReadChunkType()
        {
            if (_stream.Length == _stream.Position)
                return null;

            if (_leave != 0)
                throw new PngDecoderException($"'{_chunkType}' reading is not completed (chunkdata).");

            if (!_isCrcRead)
                throw new PngDecoderException($"'{_chunkType}' reading is not completed (crc).");

            _leave = Length = (uint)PrivateReadInt32();
            _chunkType = Encoding.ASCII.GetString(_stream.ReadBytes(4));

            return _chunkType;
        }

        public uint ReadCrc()
        {
            if (_leave != 0)
                ThrowEndReached();

            return (uint)PrivateReadInt32();
        }

        public uint ReadUInt32()
            => (uint)ReadInt32();

        public int ReadInt32()
        {
            Check(4);
            return PrivateReadInt32();
        }

        private int PrivateReadInt32()
        {
            var a = (byte)_stream.ReadByte();
            var b = (byte)_stream.ReadByte();
            var c = (byte)_stream.ReadByte();
            var digit = _stream.ReadByte();
            var d = (byte)digit;

            if (digit < 0) ThrowEndReached();

            return (a << 24) | (b << 16) | (c << 8) | d;
        }

        public ushort ReadUInt16()
        {
            Check(2);

            var a = (byte)_stream.ReadByte();
            var digit = _stream.ReadByte();
            var b = (byte)digit;

            if (digit < 0) ThrowEndReached();

            return (ushort)((a << 8) | b);
        }

        public byte ReadByte()
        {
            Check(1);

            var digit = _stream.ReadByte();
            if (digit < 0) ThrowEndReached();

            return (byte)digit;
        }

        public byte[] ReadBytes(int count)
        {
            Check((uint)count);

            var buffer = new byte[count];

            if (_stream.Read(buffer, 0, count) != count)
                ThrowEndReached();

            return buffer;
        }

        private void Check(uint len)
        {
            if (len > _leave)
                ThrowEndReached();

            _leave -= len;
        }

        private void ThrowEndReached()
        {
            throw new PngDecoderException("End reached");
        }
    }
}
