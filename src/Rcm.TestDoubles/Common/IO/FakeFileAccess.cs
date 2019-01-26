using System;
using System.Collections.Generic;
using System.IO;
using Rcm.Common.IO;

namespace Rcm.TestDoubles.IO
{
    public class FakeFileAccess : IFileAccess
    {
        private class FakeFileStream : Stream
        {
            private int _position;
            private byte[] _data;

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => true;
            public override long Length => _data.Length;

            public FakeFileStream()
            {
                _data = new byte[0];
            }

            public override long Position
            {
                get => _position;
                set
                {
                    if (value >= Int32.MaxValue)
                    {
                        throw new ArgumentOutOfRangeException(
                            $"Position {value} is above maximum stream length of {Int32.MaxValue}");
                    }

                    if (value > Length)
                    {
                        throw new ArgumentOutOfRangeException(
                            $"Position {value} is outside of stream length of {Length}");
                    }

                    _position = (int)value;
                }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var remainingLength = _data.Length - _position;
                var lengthToCopy = count < remainingLength ? count : remainingLength;

                Array.Copy(_data, _position, buffer, offset, lengthToCopy);

                Position += lengthToCopy;

                return lengthToCopy;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (origin == SeekOrigin.Begin)
                {
                    Position = offset;
                }
                else if (origin == SeekOrigin.Current)
                {
                    Position += offset;
                }
                else
                {
                    Position = _data.Length + offset;
                }

                return Position;
            }

            public override void SetLength(long value)
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        $"Stream length must be greater than zero, got {value}");
                }

                if (value > Int32.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        $"Specified stream length {value} is greater than the maximum of {Int32.MaxValue}");
                }

                Array.Resize(ref _data, (int)value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (_data.Length - _position < count)
                {
                    Array.Resize(ref _data, _position + count);
                }

                Array.Copy(buffer, offset, _data, _position, count);
                Position += count;
            }
        }

        private readonly IDictionary<string, FakeFileStream> _files = new Dictionary<string, FakeFileStream>();

        public bool Exists(string path) =>
            _files.ContainsKey(path);

        public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            if (!_files.TryGetValue(path, out var stream))
            {
                stream = new FakeFileStream();
                _files.Add(path, stream);
            }
            else
            {
                stream.Seek(0L, SeekOrigin.Begin);
            }

            return stream;
        }
    }
}
