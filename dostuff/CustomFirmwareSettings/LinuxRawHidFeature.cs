using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CustomFirmwareSettings
{
    /// <summary>
    /// Direct HIDIOCGFEATURE/HIDIOCSFEATURE ioctl calls against a hidraw
    /// device node, bypassing HidSharpCore entirely.
    ///
    /// Exists because HidSharpCore's LinuxHidStream.GetFeature/SetFeature
    /// throw a bare IOException on at least some devices/kernels, even
    /// though the exact same ioctl against the same node succeeds when
    /// issued directly (verified against a PTK-470's config interface).
    /// Only ever call this on Linux - see FeatureReportAccess for the
    /// platform gate.
    /// </summary>
    internal static class LinuxRawHidFeature
    {
        [DllImport("libc", SetLastError = true)]
        private static extern int ioctl(int fd, uint request, IntPtr argp);

        // ioctl request encoding: _IOC(dir, type, nr, size)
        // dir = READ|WRITE (3), type = 'H', nr = 0x06 (SFEATURE) / 0x07 (GFEATURE)
        private static uint HIDIOCGFEATURE(int len) =>
            (uint)((3 << 30) | ((byte)'H' << 8) | 0x07 | (len << 16));

        private static uint HIDIOCSFEATURE(int len) =>
            (uint)((3 << 30) | ((byte)'H' << 8) | 0x06 | (len << 16));

        /// <param name="devicePath">e.g. "/dev/hidraw12"</param>
        /// <param name="length">total report length including the report-ID byte</param>
        public static byte[] GetFeature(string devicePath, byte reportId, int length)
        {
            using var fs = new FileStream(devicePath, FileMode.Open, FileAccess.ReadWrite);
            int fd = (int)fs.SafeFileHandle.DangerousGetHandle();

            var buffer = new byte[length];
            buffer[0] = reportId;

            var native = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.Copy(buffer, 0, native, length);

                int result = ioctl(fd, HIDIOCGFEATURE(length), native);
                if (result < 0)
                    throw new IOException($"HIDIOCGFEATURE failed on {devicePath}, errno={Marshal.GetLastWin32Error()}");

                Marshal.Copy(native, buffer, 0, length);
                return buffer;
            }
            finally
            {
                Marshal.FreeHGlobal(native);
            }
        }

        /// <param name="devicePath">e.g. "/dev/hidraw12"</param>
        /// <param name="data">full report bytes, data[0] must be the report ID</param>
        public static void SetFeature(string devicePath, byte[] data)
        {
            using var fs = new FileStream(devicePath, FileMode.Open, FileAccess.ReadWrite);
            int fd = (int)fs.SafeFileHandle.DangerousGetHandle();

            var native = Marshal.AllocHGlobal(data.Length);
            try
            {
                Marshal.Copy(data, 0, native, data.Length);

                int result = ioctl(fd, HIDIOCSFEATURE(data.Length), native);
                if (result < 0)
                    throw new IOException($"HIDIOCSFEATURE failed on {devicePath}, errno={Marshal.GetLastWin32Error()}");
            }
            finally
            {
                Marshal.FreeHGlobal(native);
            }
        }
    }
}
