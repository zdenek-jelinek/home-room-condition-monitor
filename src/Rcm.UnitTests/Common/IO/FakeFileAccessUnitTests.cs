using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Rcm.Common.IO;
using Rcm.Testing.Common.IO;

namespace Rcm.UnitTests.Common.IO;

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
            // Given
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess();

            // When
            fileAccess.WriteAllText(path, contents);
            var readContents = fileAccess.ReadAllText(path);

            // Then
            Assert.AreEqual(contents, readContents);
        }

        [Test]
        public void EmptyFileCanBeEnlargedUsingSetLength()
        {
            // Given
            var path = "file.txt";
            var newLength = 10L;

            var fileAccess = new FakeFileAccess([(path, [])]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            file.SetLength(newLength);

            // Then
            Assert.AreEqual(newLength, file.Length);
        }

        [Test]
        public void FileCanBeShrunkUsingSetLength()
        {
            // Given
            var path = "file.txt";
            var newLength = 0L;

            var fileAccess = new FakeFileAccess([(path, [.. "Hello world!"u8])]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            file.SetLength(newLength);

            // Then
            Assert.AreEqual(newLength, file.Length);
        }

        [Test]
        public void ThrowsArgumentOutOfRangeExceptionForNegativeLength()
        {
            // Given
            var path = "file.txt";

            var fileAccess = new FakeFileAccess();

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            void SetNegativeLength() => file.SetLength(-1L);

            // Then
            Assert.Catch<ArgumentOutOfRangeException>(SetNegativeLength);
        }

        [Test]
        public void WritingPastFileSizeEnlargesTheFile()
        {
            // Given
            var path = "file.txt";
            var contentsToWrite = "Hello world!"u8;

            var fileAccess = new FakeFileAccess([(path, [])]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            file.Write(contentsToWrite);

            // Then
            Assert.AreEqual(contentsToWrite.Length, file.Length);
        }

        [Test]
        public void ReadingPastFileSizeReadsOnlyUntilFileEndAndReturnsCountOfBytesActuallyRead()
        {
            // Given
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess([(path, Encoding.UTF8.GetBytes(contents))]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            var buffer = new byte[2 * contents.Length];
            var readCount = file.Read(buffer.AsSpan());

            // Then
            Assert.AreEqual(contents.Length, readCount);
            Assert.AreEqual(contents, Encoding.UTF8.GetString(buffer.AsSpan(0, readCount)));
        }

        [Test]
        [TestCase(FileAccess.Read)]
        [TestCase(FileAccess.ReadWrite)]
        public void FileOpenedWithReadAccessCanBeRead(FileAccess readFileAccess)
        {
            // Given
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess([(path, Encoding.UTF8.GetBytes(contents))]);

            // When
            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, readFileAccess, FileShare.None);

            string ReadFromFile()
            {
                var buffer = new byte[contents.Length];
                file.ReadExactly(buffer.AsSpan());
                return Encoding.UTF8.GetString(buffer);
            }

            // Then
            Assert.IsTrue(file.CanRead);
            Assert.AreEqual(contents, ReadFromFile());
        }

        [Test]
        [TestCase(FileAccess.Write)]
        [TestCase(FileAccess.ReadWrite)]
        public void FileOpenedWithWriteAccessCanBeWritten(FileAccess writeFileAccess)
        {
            // Given
            var path = "file.txt";

            var fileAccess = new FakeFileAccess([(path, [])]);

            // When
            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, writeFileAccess, FileShare.None);

            void WriteIntoFile() => file.Write("Hello world!"u8);

            // Then
            Assert.IsTrue(file.CanWrite);
            Assert.DoesNotThrow(WriteIntoFile);
        }

        [Test]
        public void FileSupportsSettingPositionWithinItsSize()
        {
            // Given
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess([(path, Encoding.UTF8.GetBytes(contents))]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            var newPositon = contents.Length / 2;

            // When
            file.Position = newPositon;

            // Then
            Assert.AreEqual(newPositon, file.Position);
        }

        [Test]
        public void SettingFilePositionBeforeTheStartOfFileThrowsArgumentOutOfRangeException()
        {
            // Given
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess([(path, Encoding.UTF8.GetBytes(contents))]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            void SetPositionBeyondFileEnd() => file.Position = -1;

            // Then
            Assert.Catch<ArgumentOutOfRangeException>(SetPositionBeyondFileEnd);
        }

        [Test]
        public void SettingFilePositionBeyondTheEndOfFileThrowsArgumentOutOfRangeException()
        {
            var contents = "Hello world!";
            var path = "file.txt";

            var fileAccess = new FakeFileAccess([(path, Encoding.UTF8.GetBytes(contents))]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            void SetPositionBeyondFileEnd() => file.Position = contents.Length + 1;

            // Then
            Assert.Catch<ArgumentOutOfRangeException>(SetPositionBeyondFileEnd);
        }

        [Test]
        public void FileSupportsSeekingFromStart()
        {
            // Given
            var seekOffset = 5L;
            var contents = "Hello world!";

            var path = "file.txt";

            var fileAccess = new FakeFileAccess([(path, Encoding.UTF8.GetBytes(contents))]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            file.Seek(seekOffset, SeekOrigin.Begin);

            // Then
            Assert.AreEqual(seekOffset, file.Position);
        }

        [Test]
        public void FileSupportsSeekingFromCurrentPosition()
        {
            // Given
            var seekOffset = -5L;
            var written = "Hello world!"u8;

            var path = "file.txt";

            var fileAccess = new FakeFileAccess([(path, [])]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            file.Write(written);

            file.Seek(seekOffset, SeekOrigin.Current);

            // Then
            Assert.AreEqual(written.Length + seekOffset, file.Position);
        }

        [Test]
        public void FileSupportsSeekingFromEnd()
        {
            // Given
            var seekOffset = -5L;

            var path = "file.txt";
            var contents = "Hello world!"u8;

            var fileAccess = new FakeFileAccess([(path, [ ..contents])]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            file.Seek(seekOffset, SeekOrigin.End);

            // Then
            Assert.AreEqual(contents.Length + seekOffset, file.Position);
        }

        [Test]
        [Theory]
        public void SeekingBeyondFileEndThrowsArgumentOutOfRangeException(SeekOrigin origin)
        {
            // Given
            var path = "empty.txt";

            var fileAccess = new FakeFileAccess([(path, [])]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            void SeekBeyondFileEnd() => file.Seek(42L, origin);

            // Then
            Assert.Catch<ArgumentOutOfRangeException>(SeekBeyondFileEnd);
        }

        [Test]
        [Theory]
        public void SeekingBeforeFileStartThrowsArgumentOutOfRangeException(SeekOrigin origin)
        {
            // Given
            var path = "empty.txt";

            var fileAccess = new FakeFileAccess([(path, [])]);

            using var file = fileAccess.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // When
            void SeekBeforeFileStart() => file.Seek(-42L, origin);

            // Then
            Assert.Catch<ArgumentOutOfRangeException>(SeekBeforeFileStart);
        }

        [Test]
        public void OpeningFileWithAppendModeOpensItAtItsEnd()
        {
            // Given
            var dummyPath = "dummy.txt";
            var content = "Hello world!";

            var fileAccess = new FakeFileAccess([(dummyPath, Encoding.UTF8.GetBytes(content))]);

            // When
            var file = fileAccess.Open(dummyPath, FileMode.Append, FileAccess.ReadWrite, FileShare.None);

            var positionAfterOpen = file.Position;

            file.Close();

            // Then
            Assert.AreEqual(content.Length, positionAfterOpen);
            Assert.AreEqual(content, fileAccess.ReadAllText(dummyPath));
        }

        [Test]
        [TestCase(FileMode.Create)]
        [TestCase(FileMode.Truncate)]
        public void OpeningFileWithCreateOrTruncateErasesItsContent(FileMode createOrTruncateFileMode)
        {
            // Given
            var dummyPath = "dummy.txt";

            var fileAccess = new FakeFileAccess([(dummyPath, [.. "Hello world!"u8])]);

            // When
            var file = fileAccess.Open(dummyPath, createOrTruncateFileMode, FileAccess.ReadWrite, FileShare.None);

            var positionAfterOpen = file.Position;

            file.Close();

            // Then
            Assert.AreEqual(0, positionAfterOpen);
            Assert.IsEmpty(fileAccess.ReadAllText(dummyPath));
        }

        [Test]
        public void DisposedFileCanBeReopenedWithConflictingAccess()
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // When
            var file = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            file.Dispose();

            void ReopenFile()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            // Then
            Assert.DoesNotThrow(ReopenFile);
        }

        [Test]
        public void NonDisposedFileCanBeReopenedWithConflictingAccessAfterItIsFinalized()
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // When
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

            // Then
            Assert.DoesNotThrow(ReopenFile);
        }

        [Test]
        public void DisposingFileMultipleTimesDoesNotThrow()
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // When
            var file = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            void DisposeMultipleTimes()
            {
                file.Dispose();
                file.Dispose();
            }

            // Then
            Assert.DoesNotThrow(DisposeMultipleTimes);
        }

        [Test]
        public void FileOpenedWithReadAccessDoesNotSupportWriting()
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // When
            using var readOnlyFile = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);

            void WriteToReadOnlyFile()
            {
                readOnlyFile.Write("Hello World!"u8);
            }

            // Then
            Assert.IsFalse(readOnlyFile.CanWrite, nameof(Stream.CanWrite));
            Assert.Catch<NotSupportedException>(WriteToReadOnlyFile);
        }

        [Test]
        public void FileOpenedWithWriteAccessDoesNotSupportReading()
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // When
            using var writeOnlyFile = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

            void ReadFromWriteOnlyFile()
            {
                var buffer = new byte[8];
                writeOnlyFile.ReadExactly(buffer.AsSpan());
            }

            // Then
            Assert.IsFalse(writeOnlyFile.CanRead, nameof(Stream.CanRead));
            Assert.Catch<NotSupportedException>(ReadFromWriteOnlyFile);
        }

        [Test]
        public void OpeningPreexistingFileWithCreateNewModeThrowsIOException()
        {
            // Given
            var preexistingFile = "dummy.txt";
            var fileAccess = new FakeFileAccess([(preexistingFile, [])]);

            // When
            void OpenPreexistingFileWithCreateNewMode()
            {
                fileAccess.Open(preexistingFile, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }

            // Then
            Assert.Catch<IOException>(OpenPreexistingFileWithCreateNewMode);
        }

        [Test]
        [TestCase(FileMode.Open)]
        [TestCase(FileMode.Truncate)]
        public void OpeningNonExtantFileWithOpenOrTruncateModeThrowsFileNotFoundException(FileMode openOrTruncateMode)
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // When
            void OpenNonExtantFileWithOpenOrTruncateMode()
            {
                fileAccess.Open(dummyPath, openOrTruncateMode, FileAccess.Write, FileShare.None);
            }

            // Then
            Assert.Catch<FileNotFoundException>(OpenNonExtantFileWithOpenOrTruncateMode);
        }

        [Test]
        [TestCase(FileShare.Inheritable)]
        [TestCase(FileShare.Delete)]
        public void FileSharesInheritableAndDeleteAreNotSupported(FileShare unsupportedFileShare)
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // When
            void OpenFileWithUnsupportedSharing()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, unsupportedFileShare);
            }

            // Then
            Assert.Catch<NotSupportedException>(OpenFileWithUnsupportedSharing);
        }

        [Test]
        [TestCase(FileAccess.Write)]
        [TestCase(FileAccess.ReadWrite)]
        public void ThrowsIOExceptionWhenAttemptingToOpenFileForWritingWithNoWriteSharing(FileAccess writeFileAccess)
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            using var file = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);

            // When
            void OpenFilePreviouslyOpenedWithoutWriteSharing()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, writeFileAccess, FileShare.ReadWrite);
            }

            // Then
            Assert.Catch<IOException>(OpenFilePreviouslyOpenedWithoutWriteSharing);
        }

        [Test]
        [TestCase(FileAccess.Read)]
        [TestCase(FileAccess.ReadWrite)]
        public void ThrowsIOExceptionWhenAttemptingToOpenFileForReadingPreviouslyOpenedWithNoReadSharing(
            FileAccess readFileAccess)
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            using var file = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Write);

            // When
            void OpenFilePreviouslyOpenedWithoutReadSharing()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, readFileAccess, FileShare.ReadWrite);
            }

            // Then
            Assert.Catch<IOException>(OpenFilePreviouslyOpenedWithoutReadSharing);
        }

        [Test]
        [TestCase(FileAccess.Read)]
        [TestCase(FileAccess.Write)]
        [TestCase(FileAccess.ReadWrite)]
        public void ThrowsIOExceptionWhenAttemptingToOpenFilePreviouslyOpenedWithNoSharing(FileAccess secondAccess)
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            using var file = fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);

            // When
            void OpenFilePreviouslyOpenedWithNoSharing()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, secondAccess, FileShare.ReadWrite);
            }

            // Then
            Assert.Catch<IOException>(OpenFilePreviouslyOpenedWithNoSharing);
        }

        [Test]
        [TestCase(FileMode.Create)]
        [TestCase(FileMode.CreateNew)]
        [TestCase(FileMode.Append)]
        [TestCase(FileMode.Truncate)]
        public void ThrowsArgumentExceptionForReadFileAccessWithFileModeOtherThanOpenOrOpenOrCreate(FileMode nonReadFileMode)
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // When
            void OpenFileWithReadAccessAndNonReadMode()
            {
                fileAccess.Open(dummyPath, nonReadFileMode, FileAccess.Read, FileShare.ReadWrite);
            }

            // Then
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
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // When
            void OpenFileWithInvalidShare()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, FileAccess.Read, invalidFileShare);
            }

            // Then
            Assert.Catch<ArgumentException>(OpenFileWithInvalidShare);
        }

        [Test]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(FileAccess.ReadWrite + 1)]
        public void ThrowsArgumentOutOfRangeForInvalidFileAccess(FileAccess invalidFileAccess)
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // When
            void OpenFileWithInvalidAccess()
            {
                fileAccess.Open(dummyPath, FileMode.OpenOrCreate, invalidFileAccess, FileShare.None);
            }

            // Then
            Assert.Catch<ArgumentException>(OpenFileWithInvalidAccess);
        }

        [Test]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(FileMode.Append + 1)]
        public void ThrowsArgumentOutOfRangeForInvalidFileMode(FileMode invalidFileMode)
        {
            // Given
            var dummyPath = "dummy.txt";
            var fileAccess = new FakeFileAccess();

            // When
            void OpenFileWithInvalidMode()
            {
                fileAccess.Open(dummyPath, invalidFileMode, FileAccess.ReadWrite, FileShare.None);
            }

            // Then
            Assert.Catch<ArgumentException>(OpenFileWithInvalidMode);
        }

        // Convert to int to work around NUnit recognizing test cases via ToString()
        // which conflicts with some of the invalid characters being non-printable
        private static IEnumerable<int> InvalidPathCharacters => Path.GetInvalidPathChars().Select(c => (int)c);

        [Test]
        [TestCaseSource(nameof(InvalidPathCharacters))]
        public void ThrowsArgumentExceptionForPathThatContainsInvalidCharacters(int invalidPathCharacter)
        {
            // Given
            var pathIncludingInvalidCharacters = $"abc{(char)invalidPathCharacter}def{Path.DirectorySeparatorChar}file.ext";
            var fileAccess = new FakeFileAccess();

            // When
            void OpenInvalidPathFile()
            {
                fileAccess.Open(pathIncludingInvalidCharacters, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            // Then
            Assert.Catch<ArgumentException>(OpenInvalidPathFile);
        }

        [Test]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t")]
        public void ThrowsArgumentExceptionForPathThatIsEmptyOrWhitespace(string emptyOrWhitespacePath)
        {
            // Given
            var fileAccess = new FakeFileAccess();

            // When
            void OpenEmptyOrWhitespacePathFile()
            {
                fileAccess.Open(emptyOrWhitespacePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            // Then
            Assert.Catch<ArgumentException>(OpenEmptyOrWhitespacePathFile);
        }

        [Test]
        public void ThrowsArgumentNullExceptionForNullPath()
        {
            // Given
            var nullPath = (string?)null;
            var fileAccess = new FakeFileAccess();

            // When
            void OpenNullPathFile()
            {
                fileAccess.Open(nullPath!, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            // Then
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
            // Given
            var (createdPath, checkedPath) = paths;

            var fileAccess = new FakeFileAccess();

            CreateEmptyFile(fileAccess, createdPath);

            // When
            var exists = fileAccess.Exists(checkedPath);

            // Then
            Assert.IsTrue(exists);
        }

        [Test]
        [TestCaseSource(nameof(Paths))]
        public void ConstructorProvidedFileExists((string, string) paths)
        {
            // Given
            var (createdPath, checkedPath) = paths;

            var fileAccess = new FakeFileAccess([(createdPath, [])]);

            // When
            var exists = fileAccess.Exists(checkedPath);

            // Then
            Assert.IsTrue(exists);
        }

        [Test]
        public void FileThatWasNotCreatedDoesNotExist()
        {
            // Given
            var emptyFileAccess = new FakeFileAccess();

            // When
            var exists = emptyFileAccess.Exists("dummy.file.path");

            // Then
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
