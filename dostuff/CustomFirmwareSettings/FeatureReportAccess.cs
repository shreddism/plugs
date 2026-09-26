using System;
using System.IO;
using System.Runtime.InteropServices;
using HidSharp;

namespace CustomFirmwareSettings
{
    /// <summary>
    /// Unified feature-report access for a single HID interface.
    ///
    /// On Windows/macOS this is a thin pass-through to HidSharpCore's
    /// normal HidStream.GetFeature/SetFeature.
    ///
    /// On Linux it bypasses HidSharpCore for these two calls specifically
    /// and issues the ioctl directly (see LinuxRawHidFeature), because
    /// HidSharpCore's own Linux GetFeature/SetFeature currently fail here.
    /// Everything else (device enumeration, report descriptor parsing)
    /// still goes through HidSharpCore as normal - only the two calls
    /// that were actually broken are swapped out.
    ///
    /// Usage is identical regardless of platform:
    ///
    ///   using var access = FeatureReportAccess.Open(device);
    ///   var bytes = access.GetFeature(reportId: 102, length: 5);
    ///   access.SetFeature(myReportBytes);
    /// </summary>
    public sealed class FeatureReportAccess : IDisposable
    {
        private readonly HidStream? _hidSharpStream; // Windows/macOS
        private readonly string? _linuxDevicePath;    // Linux

        private FeatureReportAccess(HidStream? hidSharpStream, string? linuxDevicePath)
        {
            _hidSharpStream = hidSharpStream;
            _linuxDevicePath = linuxDevicePath;
        }

        /// <summary>
        /// Opens feature-report access for a device already resolved via
        /// HidSharpCore's normal enumeration (i.e. the interface whose
        /// descriptor you already confirmed carries the report you want).
        /// </summary>
        public static FeatureReportAccess Open(HidDevice device)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // HidSharpCore's DevicePath on Linux is a sysfs path
                // ending in ".../hidraw/hidrawN" - the character device
                // itself lives at /dev/hidrawN.
                var hidrawName = Path.GetFileName(device.DevicePath);
                var devPath = $"/dev/{hidrawName}";

                if (!File.Exists(devPath))
                    throw new FileNotFoundException($"expected hidraw node not found: {devPath}", devPath);

                return new FeatureReportAccess(null, devPath);
            }

            var config = new OpenConfiguration();
            config.SetOption(OpenOption.Exclusive, false);

            if (!device.TryOpen(config, out var stream))
                throw new IOException($"could not open {device.DevicePath}");

            return new FeatureReportAccess(stream, null);
        }

        /// <param name="length">total report length including the report-ID byte
        /// (get this from the device's report descriptor, don't hardcode it)</param>
        public byte[] GetFeature(byte reportId, int length)
        {
            if (_linuxDevicePath is not null)
                return LinuxRawHidFeature.GetFeature(_linuxDevicePath, reportId, length);

            var buffer = new byte[length];
            buffer[0] = reportId;
            _hidSharpStream!.GetFeature(buffer);
            return buffer;
        }

        /// <param name="data">full report bytes, data[0] must be the report ID</param>
        public void SetFeature(byte[] data)
        {
            if (_linuxDevicePath is not null)
            {
                LinuxRawHidFeature.SetFeature(_linuxDevicePath, data);
                return;
            }

            _hidSharpStream!.SetFeature(data);
        }

        public void Dispose() => _hidSharpStream?.Dispose();
    }
}
