using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Rcm.Common.IO;

namespace Rcm.Common.TestDoubles.IO
{
    public class FakeFileAccess : IFileAccess
    {
        private readonly IDictionary<string, FakeFile> _files;

        public FakeFileAccess(params (string name, byte[] data)[] files)
        {
            _files = files.ToDictionary(f => Path.GetFullPath(f.name), f => new FakeFile(f.data));
        }

        public bool Exists(string path) => _files.ContainsKey(Path.GetFullPath(path));

        public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            ValidateArguments(path, mode, access, share);

            path = Path.GetFullPath(path);

            if (!_files.TryGetValue(path, out var file))
            {
                if (mode == FileMode.Open || mode == FileMode.Truncate)
                {
                    throw new FileNotFoundException($"File not found.", path);
                }

                file = new FakeFile();
                _files.Add(path, file);
            }
            else if (mode == FileMode.CreateNew)
            {
                throw new IOException($"Specified file already exists. Path: {path}");
            }

            if (mode == FileMode.Create || mode == FileMode.Truncate)
            {
                file.SetSize(0);
            }

            return FakeFileStream.Open(file, access, share, mode == FileMode.Append);
        }

        private void ValidateArguments(string path, FileMode mode, FileAccess access, FileShare share)
        {
            ValidatePath(path);
            ValidateMode(mode);
            ValidateAccess(access);
            ValidateShare(share);
            ValidateModeAndAccessCombination(mode, access);
        }

        private static void ValidateModeAndAccessCombination(FileMode mode, FileAccess access)
        {
            if (access == FileAccess.Read && mode != FileMode.Open && mode != FileMode.OpenOrCreate)
            {
                throw new ArgumentException($"Cannot open file in mode {mode} with access {access}.");
            }
        }

        private static void ValidateShare(FileShare share)
        {
            if (share < 0 || share.HasFlag((FileShare)0x8) || share > (FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable))
            {
                throw new ArgumentOutOfRangeException(nameof(share), $"Invalid {nameof(FileShare)} value {share}.");
            }
        }

        private static void ValidateAccess(FileAccess access)
        {
            if (access <= 0 || access > FileAccess.ReadWrite)
            {
                throw new ArgumentOutOfRangeException(nameof(access), $"Invalid {nameof(FileAccess)} value {access}.");
            }
        }

        private static void ValidateMode(FileMode mode)
        {
            if (!IsValidFileMode(mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode), $"Invalid {nameof(FileMode)} value {mode}.");
            }
        }

        private static bool IsValidFileMode(FileMode mode)
        {
            return mode == FileMode.Append
                || mode == FileMode.Create
                || mode == FileMode.CreateNew
                || mode == FileMode.Open
                || mode == FileMode.OpenOrCreate
                || mode == FileMode.Truncate;
        }

        private static void ValidatePath(string path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (String.IsNullOrWhiteSpace(path) || Path.GetInvalidPathChars().Any(path.Contains))
            {
                throw new ArgumentException($"Path is not valid: \"{path}\"", nameof(path));
            }
        }

        private class FakeFile
        {
            private byte[] _data;

            // 0-31: readers count
            // 32-62: writers count
            // 63: locked exclusively
            private long _locks;

            public int Length => _data.Length;

            public FakeFile() : this(Array.Empty<byte>())
            {
            }

            public FakeFile(byte[] data)
            {
                _data = data;
            }

            public int Read(int position, byte[] buffer, int bufferOffset, int requestedLength)
            {
                var remainingLength = _data.Length - position;
                var lengthToCopy = requestedLength < remainingLength ? requestedLength : remainingLength;

                Array.Copy(_data, position, buffer, bufferOffset, lengthToCopy);

                return lengthToCopy;
            }

            public void Write(int position, byte[] buffer, int offset, int count)
            {
                if (_data.Length - position < count)
                {
                    Array.Resize(ref _data, position + count);
                }

                Array.Copy(buffer, offset, _data, position, count);
            }

            public void SetSize(int size)
            {
                Array.Resize(ref _data, size);
            }

            public void Acquire(FileAccess access, FileShare share)
            {
                EnsureSupportedShare(share);
                AcquireLocks(access, share);
            }

            private static void EnsureSupportedShare(FileShare share)
            {
                if (share.HasFlag(FileShare.Delete))
                {
                    throw new NotSupportedException($"Fake file access does not support {nameof(FileShare)}.{nameof(FileShare.Delete)}");
                }

                if (share.HasFlag(FileShare.Inheritable))
                {
                    throw new NotSupportedException($"Fake file access does not support {nameof(FileShare)}.{nameof(FileShare.Inheritable)}");
                }
            }

            private void AcquireLocks(FileAccess access, FileShare share)
            {
                long initialLocks, replacementLocks;
                do
                {
                    initialLocks = _locks;

                    ValidateAccess(initialLocks, access);

                    replacementLocks = GetAcquiredLocks(initialLocks, share);
                }
                while (initialLocks != Interlocked.CompareExchange(ref _locks, replacementLocks, initialLocks));
            }

            private static void ValidateAccess(long locks, FileAccess access)
            {
                if (HasExclusigeLock(locks)
                    || (access.HasFlag(FileAccess.Read) && HasReadLocks(locks))
                    || (access.HasFlag(FileAccess.Write) && HasWriteLocks(locks)))
                {
                    throw new IOException($"Unable to acquire file lock for access {access}.");
                }
            }

            private static bool HasExclusigeLock(long locks) => locks < 0L;
            private static bool HasReadLocks(long locks) => (locks & 0xFFFF_FFFF) != 0L;
            private static bool HasWriteLocks(long locks) => (locks & 0x7FFF_FFFF_0000_0000) != 0L;

            private static long GetAcquiredLocks(long currentLocks, FileShare share)
            {
                if (share == FileShare.None)
                {
                    return unchecked((long)0x8000_0000_0000_0000UL);
                }

                var readLocks = currentLocks & 0xFFFF_FFFF;
                if (IsReadLock(share))
                {
                    readLocks += 1;
                }

                var writeLocks = (currentLocks >> 32) & 0x7FFF_FFFF;
                if (IsWriteLock(share))
                {
                    writeLocks += 1;
                }

                ValidateReadLocks(readLocks);
                ValidateWriteLocks(writeLocks);

                return (writeLocks << 32) | readLocks;
            }

            public void Release(FileShare share)
            {
                long initialLocks, replacementLocks;
                do
                {
                    initialLocks = _locks;
                    replacementLocks = GetReleasedLocks(initialLocks, share);
                }
                while (initialLocks != Interlocked.CompareExchange(ref _locks, replacementLocks, initialLocks));
            }

            private static long GetReleasedLocks(long currentLocks, FileShare share)
            {
                if (share == FileShare.None)
                {
                    return 0L;
                }

                var readLocks = currentLocks & 0xFFFFFFFF;
                if (IsReadLock(share))
                {
                    readLocks -= 1;
                }

                var writeLocks = (currentLocks >> 32) & 0x7FFFFFFF;
                if (IsWriteLock(share))
                {
                    writeLocks -= 1;
                }

                ValidateReadLocks(readLocks);
                ValidateWriteLocks(writeLocks);

                return (writeLocks << 32) | readLocks;
            }

            private static void ValidateReadLocks(long readLocks)
            {
                if (readLocks > UInt32.MaxValue)
                {
                    throw new InvalidOperationException($"Attempted to open more than {UInt32.MaxValue} read locks.");
                }
                else if (readLocks < 0)
                {
                    throw new InvalidOperationException($"Attempted to close more read locks than opened.");
                }
            }

            private static void ValidateWriteLocks(long writeLocks)
            {
                if (writeLocks > Int32.MaxValue)
                {
                    throw new InvalidOperationException($"Attempted to open more than {Int32.MaxValue} write locks.");
                }
                else if (writeLocks < 0)
                {
                    throw new InvalidOperationException($"Attempted to close more write locks than opened.");
                }
            }

            private static bool IsReadLock(FileShare share) => (share & FileShare.Read) == 0;
            private static bool IsWriteLock(FileShare share) => (share & FileShare.Write) == 0;
        }

        private class FakeFileStream : Stream
        {
            private readonly FakeFile _file;
            private readonly FileAccess _access;
            private readonly FileShare _share;

            private int _position;
            private bool _disposed;

            public override bool CanRead => _access.HasFlag(FileAccess.Read);
            public override bool CanSeek => true;
            public override bool CanWrite => _access.HasFlag(FileAccess.Write);
            public override long Length => _file.Length;

            public static FakeFileStream Open(FakeFile file, FileAccess access, FileShare share, bool append)
            {
                file.Acquire(access, share);

                return new FakeFileStream(file, access, share, append);
            }

            private FakeFileStream(FakeFile file, FileAccess access, FileShare share, bool append)
            {
                _file = file;
                _share = share;
                _access = access;

                if (append)
                {
                    _position = _file.Length;
                }
            }

            public override long Position
            {
                get => _position;
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException($"Position {value} is before the start of stream.");
                    }

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
                if (!CanRead)
                {
                    throw new NotSupportedException("The stream was not opened as readable.");
                }

                var readLength = _file.Read(_position, buffer, offset, count);

                Position += readLength;

                return readLength;
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
                    Position = _file.Length + offset;
                }

                return Position;
            }

            public override void SetLength(long value)
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        $"Stream length must be non-negative, got {value}");
                }

                if (value > Int32.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        $"Specified stream length {value} is greater than the maximum of {Int32.MaxValue}");
                }

                _file.SetSize((int)value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (!CanWrite)
                {
                    throw new NotSupportedException("The stream was not opened as readable.");
                }

                _file.Write(_position, buffer, offset, count);
                Position += count;
            }

            protected override void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _file?.Release(_share);
            }

            ~FakeFileStream()
            {
                Dispose(false);
            }
        }
    }
}
