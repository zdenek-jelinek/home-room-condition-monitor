using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace Rcm.I2c
{
    public class I2cBus : IDisposable
    {
        private readonly ILogger<I2cBus> _logger;
        private readonly FileHandle _i2cBusHandle;

        private byte? _selectedDeviceAddress;
        private bool _disposed;

        private I2cBus(ILogger<I2cBus> logger, FileHandle i2cBusHandle)
        {
            _logger = logger;
            _i2cBusHandle = i2cBusHandle;
        }

        
        private const int OpenFlagsReadWrite = 2;
        private const int SelectI2cSlave = 0x703;

        [DllImport("libc.so.6", EntryPoint = "open", SetLastError = true)]
        private static extern FileHandle Open(string fileName, int flags);

        [DllImport("libc.so.6", EntryPoint = "close", SetLastError = true)]
        private static extern int Close(IntPtr handle);
 
        [DllImport("libc.so.6", EntryPoint = "ioctl", SetLastError = true)]
        private static extern int Ioctl(FileHandle handle, int request, int data);
 
        [DllImport("libc.so.6", EntryPoint = "read", SetLastError = true)]
        private static extern int Read(FileHandle handle, in byte data, int length);

        [DllImport("libc.so.6", EntryPoint = "write", SetLastError = true)]
        private static extern int Write(FileHandle handle, in byte data, int length);

        internal static I2cBus Open(ILogger<I2cBus> logger, string i2cBus)
        {
	        var i2cBusHandle = Open(i2cBus, OpenFlagsReadWrite);
            if (i2cBusHandle.IsInvalid)
            {
                throw new IOException($"Could not open I2C bus \"{i2cBus}\".", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            logger.LogDebug($"I2C bus \"{i2cBus}\" initialized.");

            return new I2cBus(logger, i2cBusHandle);
        }

        private void SelectDevice(byte address)
        {
            if (_selectedDeviceAddress == address)
            {
                return;
            }

            var selectionResult = Ioctl(_i2cBusHandle, SelectI2cSlave, address);
            if (selectionResult == -1)
            {
                throw new IOException($"Could not select I2C device at \"{address}\"", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            _selectedDeviceAddress = address;

            _logger.LogTrace($"Selected I2C device at {address:x}.");
        }

        private unsafe void Read(Span<byte> buffer)
        {
            int read;
            fixed (byte* ptr = buffer)
            {
                read = Read(_i2cBusHandle, Unsafe.AsRef<byte>(ptr), buffer.Length);
            }

            if (read == -1)
            {
                throw new IOException($"Could not read from I2C device at \"{_selectedDeviceAddress}\"", new Win32Exception(Marshal.GetLastWin32Error()));
            }
            else if (read != buffer.Length)
            {
                throw new IOException($"Failed to read expected data from I2C device at \"{_selectedDeviceAddress}\".\nExpected: {buffer.Length}\nRead: {read}");
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace($"Read {read} bytes from I2C bus.\n{PrintBuffer(buffer, read)}");
            }
        }

        public void Read(byte deviceAddress, Span<byte> buffer)
        {
            SelectDevice(deviceAddress);
            Read(buffer);
        }

        private unsafe void Write(ReadOnlySpan<byte> data)
        {
            int written;
            fixed (byte* ptr = data)
            {
                written = Write(_i2cBusHandle, Unsafe.AsRef<byte>(ptr), data.Length);
            }

            if (written == -1)
            {
                throw new IOException($"Could not write to I2C device at \"{_selectedDeviceAddress}\"", new Win32Exception(Marshal.GetLastWin32Error()));
            }
            else if (written != data.Length)
            {
                throw new IOException($"Failed to write all data to I2C device at \"{_selectedDeviceAddress}\".\nExpected: {data.Length}\nWritten: {written}");
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace($"Written {written} bytes to I2C bus.\n{PrintBuffer(data, written)}");
            }
        }

        public void Write(byte deviceAddress, ReadOnlySpan<byte> data)
        {
            SelectDevice(deviceAddress);
            Write(data);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _i2cBusHandle.Dispose();

            _logger.LogDebug($"I2C bus closed.");
        }

        private static string PrintBuffer(ReadOnlySpan<byte> buffer, int length)
        {
            var str = new StringBuilder(2 * length);

            foreach (var @byte in buffer.Slice(0, length))
            {
                str.AppendFormat("{0:X2}", @byte);
            }

            return str.ToString();
        }

        private class FileHandle : SafeHandleMinusOneIsInvalid
        {
            public FileHandle() : base(true)
            {
            }

            public FileHandle(IntPtr handle, bool isOwned) : base(isOwned)
            {
                SetHandle(handle);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            protected override bool ReleaseHandle()
            {
                return I2cBus.Close(handle) != -1;
            }
        }
    }
}
