using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Rcm.Common.IO;
using Rcm.Common.TestDoubles.IO;

namespace Rcm.Common.TestDoubles.Tests.IO;

[TestFixture]
public class FakeFileAccessUnitTests
{
    private static string RelativePath { get; } = $".{Path.DirectorySeparatorChar}Files";
    private static string AbsolutePath { get; } = Path.GetFullPath(RelativePath);

    public class OpenTests
    {
        [Test]
        public void DataWrittenToFileCanBeRetrievedWithSubsequentRead()
        {
            // given
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess();

            // when
            fileAccess.WriteAllText(path, contents);
            var readContents = fileAccess.ReadAllText(path);

            // then
            Assert.AreEqual(contents, readContents);
        }

        [Test]
        public void EmptyFileCanBeEnlargedUsingSetLength()
        {
            // given
            var path = "file.txt";
            var newLength = 10L;

            var fileAccess = new FakeFileAccess((path, Array.Empty<byte>()));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            file.SetLength(newLength);

            // then
            Assert.AreEqual(newLength, file.Length);
        }

        [Test]
        public void FileCanBeShrunkUsingSetLength()
        {
            // given
            var path = "file.txt";
            var newLength = 0L;

            var fileAccess = new FakeFileAccess((path, Encoding.UTF8.GetBytes("Hello world!")));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            file.SetLength(newLength);

            // then
            Assert.AreEqual(newLength, file.Length);
        }

        [Test]
        public void ThrowsArgumentOutOfRangeExceptionForNegativeLength()
        {
            // given
            var path = "file.txt";

            var fileAccess = new FakeFileAccess();

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            void SetNegativeLength() => file.SetLength(-1L);

            // then
            Assert.Catch<ArgumentOutOfRangeException>(SetNegativeLength);
        }

        [Test]
        public void WritingPastFileSizeEnlargesTheFile()
        {
            // given
            var path = "file.txt";
            var contentsToWrite = "Hello world!";

            var fileAccess = new FakeFileAccess((path, Array.Empty<byte>()));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            file.Write(Encoding.UTF8.GetBytes(contentsToWrite));

            // then
            Assert.AreEqual(contentsToWrite.Length, file.Length);
        }

        [Test]
        public void ReadingPastFileSizeReadsOnlyUntilFileEndAndReturnsCountOfBytesActuallyRead()
        {
            // given
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess((path, Encoding.UTF8.GetBytes(contents)));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            var buffer = new byte[2 * contents.Length];
            var readCount = file.Read(buffer.AsSpan());

            // then
            Assert.AreEqual(contents.Length, readCount);
            Assert.AreEqual(contents, Encoding.UTF8.GetString(buffer.AsSpan(0, readCount)));
        }

        [Test]
        [TestCase(FileAccess.Read)]
        [TestCase(FileAccess.ReadWrite)]
        public void FileOpenedWithReadAccessCanBeRead(FileAccess readFileAcces)
        {
            // given
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess((path, Encoding.UTF8.GetBytes(contents)));

            // when
            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, readFileAcces, FileShare.None);

            string ReadFromFile()
            {
                var buffer = new byte[contents.Length];
                file.Read(buffer.AsSpan());
                return Encoding.UTF8.GetString(buffer);
            }

            // then
            Assert.IsTrue(file.CanRead);
            Assert.AreEqual(contents, ReadFromFile());
        }

        [Test]
        [TestCase(FileAccess.Write)]
        [TestCase(FileAccess.ReadWrite)]
        public void FileOpenedWithWriteAccessCanBeWritten(FileAccess writeFileAcces)
        {
            // given
            var path = "file.txt";

            var fileAccess = new FakeFileAccess((path, Array.Empty<byte>()));

            // when
            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, writeFileAcces, FileShare.None);

            void WriteIntoFile() => file.Write(Encoding.UTF8.GetBytes("Hello world!"));

            // then
            Assert.IsTrue(file.CanWrite);
            Assert.DoesNotThrow(WriteIntoFile);
        }

        [Test]
        public void FileSupportsSettingPositionWithinItsSize()
        {
            // given
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess((path, Encoding.UTF8.GetBytes(contents)));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            var newPositon = contents.Length / 2;

            // when
            file.Position = newPositon;

            // then
            Assert.AreEqual(newPositon, file.Position);
        }

        [Test]
        public void SettingFilePositionBeforeTheStartOfFileThrowsArgumentOutOfRangeException()
        {
            // given
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess((path, Encoding.UTF8.GetBytes(contents)));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            void SetPositionBeyondFileEnd() => file.Position = -1;

            // then
            Assert.Catch<ArgumentOutOfRangeException>(SetPositionBeyondFileEnd);
        }

        [Test]
        public void SettingFilePositionBeyondTheEndOfFileThrowsArgumentOutOfRangeException()
        {
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess((path, Encoding.UTF8.GetBytes(contents)));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            void SetPositionBeyondFileEnd() => file.Position = contents.Length + 1;

            // then
            Assert.Catch<ArgumentOutOfRangeException>(SetPositionBeyondFileEnd);
        }

        [Test]
        public void FileSupportsSeekingFromStart()
        {
            // given
            var seekOffset = 5L;
            var contents = "Hello world!";

            var path = "file.txt";

            var fileAccess = new FakeFileAccess((path, Encoding.UTF8.GetBytes(contents)));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            file.Seek(seekOffset, SeekOrigin.Begin);

            // then
            Assert.AreEqual(seekOffset, file.Position);
        }

        [Test]
        public void FileSupportsSeekingFromCurrentPosition()
        {
            // given
            var seekOffset = -5L;
            var written = "Hello world!";

            var path = "file.txt";

            var fileAccess = new FakeFileAccess((path, Array.Empty<byte>()));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            file.Write(Encoding.UTF8.GetBytes(written).AsSpan());

            file.Seek(seekOffset, SeekOrigin.Current);

            // then
            Assert.AreEqual(written.Length + seekOffset, file.Position);
        }

        [Test]
        public void FileSupportsSeekingFromEnd()
        {
            // given
            var seekOffset = -5L;

            var path = "file.txt";
            var contents = "Hello world!";

            var fileAccess = new FakeFileAccess((path, Encoding.UTF8.GetBytes(contents)));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            file.Seek(seekOffset, SeekOrigin.End);

            // then
            Assert.AreEqual(contents.Length + seekOffset, file.Position);
        }

        [Test]
        [Theory]
        public void SeekingBeyondFileEndThrowsArgumentOutOfRangeException(SeekOrigin origin)
        {
            // given
            var path = "empty.txt";

            var fileAccess = new FakeFileAccess((path, Array.Empty<byte>()));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            void SeekBeyondFileEnd() => file.Seek(42L, origin);

            // then
            Assert.Catch<ArgumentOutOfRangeException>(SeekBeyondFileEnd);
        }

        [Test]
        [Theory]
        public void SeekingBeforeFileStartThrowsArgumentOutOfRangeException(SeekOrigin origin)
        {
            // given
            var path = "empty.txt";

            var fileAccess = new FakeFileAccess((path, Array.Empty<byte>()));

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // when
            void SeekBeforeFileStart() => file.Seek(-42L, origin);

            // then
            Assert.Catch<ArgumentOutOfRangeException>(SeekBeforeFileStart);
        }

        [Test]
        public void OpeningFileWithAppendModeOpensItAtItsEnd()
        {
            // given
            var dummyPath = "dummy.txt";
            var content = "Hello world!";

            var fileAccess = new FakeFileAccess((dummyPath, Encoding.UTF8.GetBytes(content)));

            // when
            var file = fileAccess.Open(dummyPath, FileMode.Append, FileAccess.ReadWrite, FileShare.None);

            var positionAfterOpen = file.Position;

            file.Close();

            // then
            Assert.AreEqual(content.Length, positionAfterOpen);
            Assert.AreEqual(content, fileAccess.ReadAllText(dummyPath));
        }

        [Test]
        [TestCase(FileMode.Create)]
        [TestCase(FileMode.Truncate)]
        public void OpeningFileWithCreateOrTruncateErasesItsContent(FileMode createOrTruncateFileMode)
        {
            // given
            var dummyPath = "dummy.txt";

            var fileAccess = new FakeFileAccess((dummyPath, Encoding.UTF8.GetBytes("Hello world!")));

            // when
            var file = fileAccess.Open(dummyPath, createOrTruncateFileMode, FileAccess.ReadWrite, FileShare.None);

            var positionAfterOpen = file.Position;

            file.Close();

            // then
            Assert.AreEqual(0, positionAfterOpen);
            Assert.IsEmpty(fileAccess.ReadAllText(dummyPath));
        }

        [Test]
        public void DisposedFileCanBeReopenedWithConflictingAccess()
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // when
            var file = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            file.Dispose();

            void ReopenFile()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            // then
            Assert.DoesNotThrow(ReopenFile);
        }

        [Test]
        public void NonDisposedFileCanBeReopenedWithConflictingAccessAfterItIsFinalized()
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // when
            void OpenFileWithoutDisposing()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            OpenFileWithoutDisposing();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            void ReopenFile()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            // then
            Assert.DoesNotThrow(ReopenFile);
        }

        [Test]
        public void DisposingFileMultipleTimesDoesNotThrow()
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // when
            var file = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            void DisposeMultipleTimes()
            {
                file.Dispose();
                file.Dispose();
            }

            // then
            Assert.DoesNotThrow(DisposeMultipleTimes);
        }

        [Test]
        public void FileOpenedWithReadAccessDoesNotSupportWriting()
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // when
            using var readOnlyFile = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);

            void WriteToReadOnlyFile()
            {
                readOnlyFile.Write(Array.Empty<byte>(), 0, 0);
            }

            // then
            Assert.IsFalse(readOnlyFile.CanWrite, nameof(Stream.CanWrite));
            Assert.Catch<NotSupportedException>(WriteToReadOnlyFile);
        }

        [Test]
        public void FileOpenedWithWriteAccessDoesNotSupportReading()
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // when
            using var writeOnlyFile = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

            void ReadFromWriteOnlyFile()
            {
                writeOnlyFile.Read(Array.Empty<byte>(), 0, 0);
            }

            // then
            Assert.IsFalse(writeOnlyFile.CanRead, nameof(Stream.CanRead));
            Assert.Catch<NotSupportedException>(ReadFromWriteOnlyFile);
        }

        [Test]
        public void OpeningPreexistingFileWithCreateNewModeThrowsIOException()
        {
            // given
            var preexistingFile = "dummy.txt";
            var fileAccess = new FakeFileAccess((preexistingFile, Array.Empty<byte>()));

            // when
            void OpenPreexistingFileWithCreateNewMode()
            {
                fileAccess.Open(preexistingFile, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }

            // then
            Assert.Catch<IOException>(OpenPreexistingFileWithCreateNewMode);
        }

        [Test]
        [TestCase(FileMode.Open)]
        [TestCase(FileMode.Truncate)]
        public void OpeningNonExtantFileWithOpenOrTruncateModeThrowsFileNotFoundException(FileMode openOrTruncateMode)
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // when
            void OpenNonExtantFileWithOpenOrTruncateMode()
            {
                fileAccess.Open(dummyPath, openOrTruncateMode, FileAccess.Write, FileShare.None);
            }

            // then
            Assert.Catch<FileNotFoundException>(OpenNonExtantFileWithOpenOrTruncateMode);
        }

        [Test]
        [TestCase(FileShare.Inheritable)]
        [TestCase(FileShare.Delete)]
        public void FileSharesInheritableAndDeleteAreNotSupported(FileShare unsupportedFileShare)
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // when
            void OpenFileWithUsupportedSharing()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, unsupportedFileShare);
            }

            // then
            Assert.Catch<NotSupportedException>(OpenFileWithUsupportedSharing);
        }

        [Test]
        [TestCase(FileAccess.Write)]
        [TestCase(FileAccess.ReadWrite)]
        public void ThrowsIOExceptionWhenAttemptingToOpenFileForWritingWithNoWriteSharing(FileAccess writeFileAccess)
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            using var file = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);

            // when
            void OpenFilePreviouslyOpenedWithoutWriteSharing()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, writeFileAccess, FileShare.ReadWrite);
            }

            // then
            Assert.Catch<IOException>(OpenFilePreviouslyOpenedWithoutWriteSharing);
        }

        [Test]
        [TestCase(FileAccess.Read)]
        [TestCase(FileAccess.ReadWrite)]
        public void ThrowsIOExceptionWhenAttemptingToOpenFileForReadingPreviouslyOpenedWithNoReadSharing(
            FileAccess readFileAccess)
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            using var file = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Write);

            // when
            void OpenFilePreviouslyOpenedWithoutReadSharing()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, readFileAccess, FileShare.ReadWrite);
            }

            // then
            Assert.Catch<IOException>(OpenFilePreviouslyOpenedWithoutReadSharing);
        }

        [Test]
        [TestCase(FileAccess.Read)]
        [TestCase(FileAccess.Write)]
        [TestCase(FileAccess.ReadWrite)]
        public void ThrowsIOExceptionWhenAttemptingToOpenFilePreviouslyOpenedWithNoSharing(FileAccess secondAccess)
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            using var file = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);

            // when
            void OpenFilePreviouslyOpenedWithNoSharing()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, secondAccess, FileShare.ReadWrite);
            }

            // then
            Assert.Catch<IOException>(OpenFilePreviouslyOpenedWithNoSharing);
        }

        [Test]
        [TestCase(FileMode.Create)]
        [TestCase(FileMode.CreateNew)]
        [TestCase(FileMode.Append)]
        [TestCase(FileMode.Truncate)]
        public void ThrowsArgumentExceptionForReadFileAccessWithFileModeOtherThanOpenOrOpenOrCreate(FileMode nonReadFileMode)
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // when
            void OpenFileWithReadAccessAndNonReadMode()
            {
                fileAccess.Open(dummyPath, nonReadFileMode, FileAccess.Read, FileShare.ReadWrite);
            }

            // then
            Assert.Catch<ArgumentException>(OpenFileWithReadAccessAndNonReadMode);
        }

        [Test]
        [TestCase(-1)]
        [TestCase(0x8)]
        [TestCase((FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable) + 1)]
        [TestCase(FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable | (FileShare)0x8)]
        [TestCase(FileShare.ReadWrite | FileShare.Delete | (FileShare)0x8)]
        [TestCase(FileShare.ReadWrite | (FileShare)0x8)]
        public void ThrowsArgumentOutOfRangeForInvalidFileShare(FileShare invalidFileShare)
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // when
            void OpenFileWithInvalidShare()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, invalidFileShare);
            }

            // then
            Assert.Catch<ArgumentException>(OpenFileWithInvalidShare);
        }

        [Test]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(FileAccess.ReadWrite + 1)]
        public void ThrowsArgumentOutOfRangeForInvalidFileAccess(FileAccess invalidFileAccess)
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // when
            void OpenFileWithInvalidAccess()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, invalidFileAccess, FileShare.None);
            }

            // then
            Assert.Catch<ArgumentException>(OpenFileWithInvalidAccess);
        }

        [Test]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(FileMode.Append + 1)]
        public void ThrowsArgumentOutOfRangeForInvalidFileMode(FileMode invalidFileMode)
        {
            // given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // when
            void OpenFileWithInvalidMode()
            {
                fileAccess.Open(dummyPath, invalidFileMode, FileAccess.ReadWrite, FileShare.None);
            }

            // then
            Assert.Catch<ArgumentException>(OpenFileWithInvalidMode);
        }

        // Convert to int to work around NUnit recognizing test cases via ToString()
        // which conflicts with some of the invalid characters being non-printable
        private static IEnumerable<int> InvalidPathCharacters
        {
            get => Path.GetInvalidPathChars().Select(c => (int)c);
        }

        [Test]
        [TestCaseSource(nameof(InvalidPathCharacters))]
        public void ThrowsArgumentExceptionForPathThatContainsInvalidCharacters(int invalidPathCharacter)
        {
            // given
            var pathIncludingInvalidCharacters = $"abc{(char)invalidPathCharacter}def{Path.DirectorySeparatorChar}file.ext";
            var fileAccess = new FakeFileAccess();

            // when
            void OpenInvalidPathFile()
            {
                fileAccess.Open(pathIncludingInvalidCharacters, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            // then
            Assert.Catch<ArgumentException>(OpenInvalidPathFile);
        }

        [Test]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t")]
        public void ThrowsArgumentExceptionForPathThatIsEmptyOrWhitespace(string emptyOrWhitespacePath)
        {
            // given
            var fileAccess = new FakeFileAccess();

            // when
            void OpenEmptyOrWhitespacePathFile()
            {
                fileAccess.Open(emptyOrWhitespacePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            // then
            Assert.Catch<ArgumentException>(OpenEmptyOrWhitespacePathFile);
        }

        [Test]
        public void ThrowsArgumentNullExceptionForNullPath()
        {
            // given
            var nullPath = (string?)null;
            var fileAccess = new FakeFileAccess();

            // when
            void OpenNullPathFile()
            {
                fileAccess.Open(nullPath!, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            // then
            Assert.Catch<ArgumentNullException>(OpenNullPathFile);
        }
    }

    public class ExistsTests
    {
        private static string FileName { get; } = "dummy.txt";

        private static IEnumerable<(string creationPath, string checkedPath)> Paths
        {
            get
            {
                var relativePath = $"{RelativePath}{Path.DirectorySeparatorChar}{FileName}";
                var absolutePath = $"{AbsolutePath}{Path.DirectorySeparatorChar}{FileName}";

                yield return (absolutePath, absolutePath);
                yield return (relativePath, absolutePath);
                yield return (absolutePath, relativePath);
                yield return (relativePath, relativePath);
            }
        }

        [Test]
        [TestCaseSource(nameof(Paths))]
        public void PreviouslyCreatedFileExists((string, string) paths)
        {
            // given
            var (createdPath, checkedPath) = paths;

            var fileAccess = new FakeFileAccess();

            CreateEmptyFile(fileAccess, createdPath);

            // when
            var exists = fileAccess.Exists(checkedPath);

            // then
            Assert.IsTrue(exists);
        }

        [Test]
        [TestCaseSource(nameof(Paths))]
        public void ConstructorProvidedFileExists((string, string) paths)
        {
            // given
            var (createdPath, checkedPath) = paths;

            var fileAccess = new FakeFileAccess((createdPath, Array.Empty<byte>()));

            // when
            var exists = fileAccess.Exists(checkedPath);

            // then
            Assert.IsTrue(exists);
        }

        [Test]
        public void FileThatWasNotCreatedDoesNotExist()
        {
            // given
            var emptyFileAccess = new FakeFileAccess();

            // when
            var exists = emptyFileAccess.Exists("dummy.file.path");

            // then
            Assert.IsFalse(exists);
        }

        private static void CreateEmptyFile(FakeFileAccess fileAccess, string path)
        {
            fileAccess
                .Open(path, FileMode.Create, FileAccess.Write, FileShare.Read)
                .Close();
        }
    }
}