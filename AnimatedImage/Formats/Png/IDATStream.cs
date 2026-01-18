#if NET6_0_OR_GREATER
using CompressionMode = System.IO.Compression.CompressionMode;
using ZlibStream = System.IO.Compression.ZLibStream;
#else
using CompressionMode =SharpCompress.Compressors.CompressionMode;
using ZlibStream = SharpCompress.Compressors.Deflate.ZlibStream;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnimatedImage.Formats.Png.Chunks;
using AnimatedImage.Formats.Png.Types;

namespace AnimatedImage.Formats.Png
{
    internal class IDATStream : IDisposable
    {
        private readonly int _dimension;
        private readonly ByteBuffer _buffer;
        private readonly byte[] _prevLine;

        public int Stride { get; }

        public IDATStream(
            ApngFile file,
            ApngFrame frame)
        {
            IHDRChunk header = file.IHDRChunk;
            PLTEChunk? palette = file.PLTEChunk;
            tRNSChunk? transparency = file.tRNSChunk;

            var mem = new MultiMemoryStream(frame.IDATChunks.Select(chunk => chunk.FrameData));
            _buffer = ByteBuffer.Create(mem, header.BitDepth);

            _dimension = header.ColorType switch
            {
                ColorType.Glayscale => _dimension = 1,
                ColorType.Color => _dimension = 3,
                ColorType.IndexedColor => _dimension = 1,
                ColorType.GlayscaleAlpha => _dimension = 2,
                ColorType.ColorAlpha => _dimension = 4,

                _ => throw new ArgumentException("unsupport color type")
            };

            int width = frame.fcTLChunk is null ? file.IHDRChunk.Width : (int)frame.fcTLChunk.Width;
            Stride = width * _dimension;

            _prevLine = new byte[Stride];
        }

        public void Reset()
        {
            for (var i = 0; i < _prevLine.Length; ++i)
                _prevLine[i] = 0;

            _buffer.Reset();
        }

        public void DecompressLine(byte[] bytes, int offset, int length)
        {
            if (offset < 0)
                throw new ArgumentException("offset is less than 0");

            if (offset + length > bytes.Length)
                throw new ArgumentException("offset+length is too long");

            if (length > _prevLine.Length || length < 0)
                throw new ArgumentException("bad length");

            if (length == 0)
                return;

            var filterByte = _buffer.ReadRawByte();
            if (filterByte == -1)
                throw new EndOfStreamException("Failed to read filter method");

            var len = _buffer.Read(bytes, offset, length);
            if (len < length)
                throw new EndOfStreamException("Failed to read compressed data");

            switch ((FilterMethod)filterByte)
            {
                case FilterMethod.None:
                    break;

                case FilterMethod.Sub:
                    for (var i = offset + _dimension; i < offset + length; ++i)
                        bytes[i] = (byte)(bytes[i] + bytes[i - _dimension]);
                    break;

                case FilterMethod.Up:
                    for (var i = 0; i < length; ++i)
                        bytes[i + offset] = (byte)(bytes[i + offset] + _prevLine[i]);
                    break;

                case FilterMethod.Average:
                    for (var i = 0; i < _dimension; ++i)
                    {
                        bytes[i + offset] = (byte)(bytes[i + offset] + (_prevLine[i] >> 1));
                    }
                    for (var i = _dimension; i < length; ++i)
                    {
                        var avg = (bytes[i + offset - _dimension] + _prevLine[i]) >> 1;
                        bytes[i + offset] = (byte)(bytes[i + offset] + avg);
                    }
                    break;

                case FilterMethod.Paeth:
                    for (var i = 0; i < _dimension; ++i)
                    {
                        bytes[i + offset] = (byte)(bytes[i + offset] + Paeth(0, _prevLine[i], 0));
                    }
                    for (var i = _dimension; i < length; ++i)
                    {
                        var val = Paeth(bytes[i + offset - _dimension], _prevLine[i], _prevLine[i - _dimension]);
                        bytes[i + offset] = (byte)(bytes[i + offset] + val);
                    }
                    break;

            }

            Array.Copy(bytes, offset, _prevLine, 0, length);

            static byte Paeth(byte a, byte b, byte c)
            {
                //
                // c | b
                // -----
                // a | ?
                //

                int pa = Math.Abs(b - c);
                int pb = Math.Abs(a - c);
                int pc = Math.Abs(a + b - 2 * c);

                return (pa <= pb && pa <= pc) ? a :
                       (pb <= pc) ? b :
                       c;
            }
        }

        public void Dispose() => _buffer.Dispose();

        internal abstract class ByteBuffer : IDisposable
        {
            private readonly MultiMemoryStream _memoryStream;
            private ZlibStream _stream;

            protected ByteBuffer(MultiMemoryStream stream)
            {
                _memoryStream = stream;
                _stream = new ZlibStream(_memoryStream, CompressionMode.Decompress);
            }

            public virtual void Reset()
            {
                _stream.Dispose();
                _memoryStream.Reset();
                _stream = new ZlibStream(_memoryStream, CompressionMode.Decompress);
            }

            public int ReadRawByte() => _stream.ReadByte();

            public int ReadRawBytes(byte[] array, int offset, int length)
            {
                return ReadFromZlibStream(_stream, array, offset, length);
            }

            private int ReadFromZlibStream(ZlibStream stream, byte[] array, int offset, int length)
            {
                int totalRead = 0;
                while (totalRead < length)
                {
                    int bytesRead = stream.Read(array, offset + totalRead, length - totalRead);
                    if (bytesRead == 0) break;
                    totalRead += bytesRead;
                }
                return totalRead;
            }

            public abstract int Read(byte[] array, int offset, int length);

            public static ByteBuffer Create(MultiMemoryStream stream, byte depth)
                => depth switch
                {
                    1 => new ByteBuffer1(stream),
                    2 => new ByteBuffer2(stream),
                    4 => new ByteBuffer4(stream),
                    8 => new ByteBuffer8(stream),
                    16 => new ByteBuffer16(stream),
                    _ => throw new ArgumentException()
                };

            public void Dispose()
            {
                _stream?.Dispose();
            }
        }

        internal class ByteBuffer1 : ByteBuffer
        {
            private int _subidx;
            private readonly byte[] _sub;

            public ByteBuffer1(MultiMemoryStream stream) : base(stream)
            {
                _sub = new byte[8];
                _subidx = _sub.Length;
            }

            public override void Reset()
            {
                base.Reset();
                _subidx = _sub.Length;
            }

            public override int Read(byte[] array, int offset, int length)
            {
                var capacity = _sub.Length - _subidx;

                if (length <= capacity)
                {
                    Array.Copy(_sub, _subidx, array, offset, length);
                    _subidx += length;
                    return length;
                }

                if (capacity != 0)
                {
                    Array.Copy(_sub, _subidx, array, offset, capacity);
                    _subidx = _sub.Length;
                    offset += capacity;
                    length -= capacity;
                }

                // Reads in 8-bit units and expand each 1 bit individually.
                // To conserve memory, the end of the destination array is used as the working area.
                var aryCopyLen = length >> 3;
                if (aryCopyLen > 0)
                {
                    int spareIdx = offset + length - aryCopyLen;
                    var readLen = ReadRawBytes(array, spareIdx, aryCopyLen);

                    for (var leave = readLen; leave > 0; --leave)
                    {
                        var val = array[spareIdx++];
                        array[offset++] = (byte)((val & 0b10000000) >> 7);
                        array[offset++] = (byte)((val & 0b01000000) >> 6);
                        array[offset++] = (byte)((val & 0b00100000) >> 5);
                        array[offset++] = (byte)((val & 0b00010000) >> 4);
                        array[offset++] = (byte)((val & 0b00001000) >> 3);
                        array[offset++] = (byte)((val & 0b00000100) >> 2);
                        array[offset++] = (byte)((val & 0b00000010) >> 1);
                        array[offset++] = (byte)((val & 0b00000001));
                    }

                    if (readLen != aryCopyLen)
                        return capacity + readLen;

                    capacity += readLen;
                    length -= readLen;
                }

                // 7bits or less remaining,
                // Read 8 bits into temporary array.
                if (length != 0)
                {
                    if (ReadRawBytes(_sub, 0, 1) == 0)
                        return capacity;

                    var val = _sub[0];
                    _sub[0] = (byte)((val & 0b10000000) >> 7);
                    _sub[1] = (byte)((val & 0b01000000) >> 6);
                    _sub[2] = (byte)((val & 0b00100000) >> 5);
                    _sub[3] = (byte)((val & 0b00010000) >> 4);
                    _sub[4] = (byte)((val & 0b00001000) >> 3);
                    _sub[5] = (byte)((val & 0b00000100) >> 2);
                    _sub[6] = (byte)((val & 0b00000010) >> 1);
                    _sub[7] = (byte)((val & 0b00000001));
                    _subidx = 0;

                    Array.Copy(_sub, _subidx, array, offset, length);
                    _subidx += length;
                }

                return capacity + length;
            }
        }

        internal class ByteBuffer2 : ByteBuffer
        {
            private int _subidx;
            private readonly byte[] _sub;

            public ByteBuffer2(MultiMemoryStream stream) : base(stream)
            {
                _sub = new byte[4];
                _subidx = _sub.Length;
            }

            public override void Reset()
            {
                base.Reset();
                _subidx = _sub.Length;
            }

            public override int Read(byte[] array, int offset, int length)
            {
                var capacity = _sub.Length - _subidx;

                if (length <= capacity)
                {
                    Array.Copy(_sub, _subidx, array, offset, length);
                    _subidx += length;
                    return length;
                }

                if (capacity != 0)
                {
                    Array.Copy(_sub, _subidx, array, offset, capacity);
                    _subidx = _sub.Length;
                    offset += capacity;
                    length -= capacity;
                }

                // Reads in 8-bit units and expand each 2 bits individually.
                // To conserve memory, the end of the destination array is used as the working area.
                var aryCopyLen = length >> 2;
                if (aryCopyLen > 0)
                {
                    int spareIdx = offset + length - aryCopyLen;
                    var readLen = ReadRawBytes(array, spareIdx, aryCopyLen);

                    for (var leave = readLen; leave > 0; --leave)
                    {
                        var val = array[spareIdx++];
                        array[offset++] = (byte)((val & 0b11000000) >> 6);
                        array[offset++] = (byte)((val & 0b00110000) >> 4);
                        array[offset++] = (byte)((val & 0b00001100) >> 2);
                        array[offset++] = (byte)((val & 0b00000011));
                    }

                    if (readLen != aryCopyLen)
                        return capacity + readLen;

                    capacity += readLen;
                    length -= readLen;
                }

                // 6bits or less remaining,
                // Read 8 bits into temporary array.
                if (length != 0)
                {
                    if (ReadRawBytes(_sub, 0, 1) == 0)
                        return capacity;

                    var val = _sub[0];
                    _sub[0] = (byte)((val & 0b11000000) >> 6);
                    _sub[1] = (byte)((val & 0b00110000) >> 4);
                    _sub[2] = (byte)((val & 0b00001100) >> 2);
                    _sub[3] = (byte)((val & 0b00000011));
                    _subidx = 0;

                    Array.Copy(_sub, _subidx, array, offset, length);
                    _subidx += length;
                }

                return capacity + length;
            }
        }

        internal class ByteBuffer4 : ByteBuffer
        {
            private int _subidx;
            private readonly byte[] _sub;

            public ByteBuffer4(MultiMemoryStream stream) : base(stream)
            {
                _sub = new byte[2];
                _subidx = _sub.Length;
            }

            public override void Reset()
            {
                base.Reset();
                _subidx = _sub.Length;
            }

            public override int Read(byte[] array, int offset, int length)
            {
                var capacity = _sub.Length - _subidx;

                if (length <= capacity)
                {
                    Array.Copy(_sub, _subidx, array, offset, length);
                    _subidx += length;
                    return length;
                }

                if (capacity != 0)
                {
                    Array.Copy(_sub, _subidx, array, offset, capacity);
                    _subidx = _sub.Length;
                    offset += capacity;
                    length -= capacity;
                }

                // Reads in 8-bit units and expand each 4 bits individually.
                // To conserve memory, the end of the destination array is used as the working area.
                var aryCopyLen = length >> 1;
                if (aryCopyLen > 0)
                {
                    int spareIdx = offset + length - aryCopyLen;
                    var readLen = ReadRawBytes(array, spareIdx, aryCopyLen);

                    for (var leave = readLen; leave > 0; --leave)
                    {
                        var val = array[spareIdx++];
                        array[offset++] = (byte)((val & 0b11110000) >> 4);
                        array[offset++] = (byte)((val & 0b00001111));
                    }

                    if (readLen != aryCopyLen)
                        return capacity + readLen;

                    capacity += readLen;
                    length -= readLen;
                }

                // 4bits or less remaining,
                // Read 8 bits into temporary array.
                if (length != 0)
                {
                    if (ReadRawBytes(_sub, 0, 1) == 0)
                        return capacity;

                    var val = _sub[0];
                    _sub[0] = (byte)((val & 0b11110000) >> 4);
                    _sub[1] = (byte)((val & 0b00001111));
                    _subidx = 0;

                    Array.Copy(_sub, _subidx, array, offset, length);
                    _subidx += length;
                }
                return capacity + length;
            }
        }

        internal class ByteBuffer8 : ByteBuffer
        {
            public ByteBuffer8(MultiMemoryStream stream) : base(stream) { }

            public override int Read(byte[] array, int offset, int length)
                => ReadRawBytes(array, offset, length);
        }

        internal class ByteBuffer16 : ByteBuffer
        {
            private byte[] _buffer = new byte[0];

            public ByteBuffer16(MultiMemoryStream stream) : base(stream) { }

            public override int Read(byte[] array, int offset, int length)
            {
                if (_buffer.Length < length * 2)
                {
                    _buffer = new byte[length * 2];
                }

                // TODO support 16bit color
                // this code may mistake a non-transparency color as a trasparency color.
                // for example If 0xFF00 is declared as transparency color,
                // 0xFF01 - 0xFFFF are treated as transparency color.

                var readLen = ReadRawBytes(_buffer, 0, length * 2);

                for (var i = 0; i < readLen / 2; ++i)
                {
                    array[offset + i] = _buffer[i * 2 + 1];
                }

                return readLen / 2;
            }
        }

        internal class MultiMemoryStream : Stream
        {
            private long _position;

            private byte[][] _arrays;
            private int _arraysIdx;

            private byte[] _current;
            private int _currentIdx;

            private long _rangeStart;
            private long _rangeEnd;

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override long Length { get; }

            public override long Position
            {
                get => _position;
                set
                {
                    var pos = Math.Max(0, value);

                    if (pos >= Length)
                    {
                        _position = Length;
                        _current = _arrays.Last();
                        _currentIdx = _current.Length;
                        _rangeStart = Length - _current.Length;
                        _rangeEnd = Length;
                    }
                    else if (pos < _rangeStart)
                    {
                        do
                        {
                            _current = _arrays[--_arraysIdx];
                            _rangeStart -= _current.Length;
                            _rangeEnd = _rangeStart + _current.Length;
                        } while (pos < _rangeStart);

                        _currentIdx = (int)(pos - _rangeStart);
                        _position = pos;
                    }
                    else if (_rangeEnd <= pos)
                    {
                        do
                        {
                            _current = _arrays[++_arraysIdx];
                            _rangeEnd += _current.Length;
                            _rangeStart = _rangeEnd - _current.Length;
                        } while (_rangeEnd <= pos);

                        _currentIdx = (int)(pos - _rangeStart);
                        _position = pos;
                    }
                    else
                    {
                        // _rangeStart <= pos && pos < _rangeEnd
                        _currentIdx += (int)(pos - _position);
                        _position = pos;
                    }
                }
            }

            public void Reset() => Position = 0;

            public MultiMemoryStream(IEnumerable<byte[]> arrays)
            {
                _arrays = arrays.ToArray();
                _current = _arrays[0];

                _position = 0;
                _arraysIdx = 0;
                _rangeStart = 0;
                _currentIdx = 0;
                _rangeEnd = _arrays[0].Length;

                Length = _arrays.Sum(a => a.Length);
            }

            public override int ReadByte()
            {
                if (Position < Length)
                {
                    var rtn = _current[_currentIdx];
                    _currentIdx += 1;
                    _position += 1;

                    if (_currentIdx >= _current.Length
                        && _arraysIdx < _arrays.Length - 1)
                    {
                        _current = _arrays[++_arraysIdx];
                        _currentIdx = 0;

                        _rangeStart = Position;
                        _rangeEnd = _rangeStart + _current.Length;
                    }

                    return rtn;
                }
                return -1;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer));
                if (offset < 0 || count < 0 || offset + count > buffer.Length)
                    throw new ArgumentOutOfRangeException();

                if (count == 0)
                    return 0;

                int remaining = count;
                int written = 0;

                while (remaining > 0 && Position < Length)
                {
                    if (_currentIdx >= _current.Length)
                    {
                        if (_arraysIdx < _arrays.Length - 1)
                        {
                            _current = _arrays[++_arraysIdx];
                            _currentIdx = 0;

                            _rangeStart = Position;
                            _rangeEnd = _rangeStart + _current.Length;
                        }
                        else
                        {
                            break;
                        }
                    }

                    int avail = _current.Length - _currentIdx;
                    int toCopy = avail <= remaining ? avail : remaining;

                    Array.Copy(_current, _currentIdx, buffer, offset + written, toCopy);

                    _currentIdx += toCopy;
                    _position += toCopy;
                    written += toCopy;
                    remaining -= toCopy;
                }

                return written;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        Position = offset;
                        break;

                    case SeekOrigin.Current:
                        Position += offset;
                        break;

                    case SeekOrigin.End:
                        Position = Length + offset;
                        break;
                }

                return Position;
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override void Flush()
            {
            }
        }
    }
}
