using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HidSharp;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace CustomFirmwareSettings
{
    /// <summary>
    /// Unified feature-report access for one report family on this
    /// device, picking the right backend per platform:
    ///
    ///   Linux:   raw HIDIOCGFEATURE/HIDIOCSFEATURE ioctl, bypassing
    ///            HidSharpCore (see LinuxRawHidFeature) - its own
    ///            GetFeature/SetFeature throw here on this device.
    ///
    ///   Windows: raw GET_REPORT/SET_REPORT control transfers via
    ///            LibUsbDotNet (see WindowsRawHidFeature), against the
    ///            interface that's been manually rebound from the HID
    ///            class driver to libusbK using Zadig (NOT WinUSB - that
    ///            specific generic driver failed GET_REPORT/SET_REPORT
    ///            on this device even with a correct interface number
    ///            and explicit ClaimInterface; libusbK on the same
    ///            interface works with a plain ControlTransfer call and
    ///            no claim step). Windows won't expose this interface to
    ///            HidSharpCore (or any other HID-class API) at all
    ///            otherwise.
    ///
    ///   Other (macOS): HidSharpCore's normal HidStream.GetFeature/
    ///            SetFeature, unchanged - no evidence of a problem here,
    ///            revisit if that turns out not to hold.
    ///
    /// Usage is identical regardless of platform:
    ///
    ///   using var access = FeatureReportAccess.Open(vendorId, productId, requiredReportId: 102);
    ///   var bytes = access.GetFeature(reportId: 102, length: 5);
    ///   access.SetFeature(myReportBytes);
    /// </summary>
    public sealed class FeatureReportAccess : IDisposable
    {
        private readonly HidStream? _hidSharpStream;   // macOS (and any other non-Linux/Windows platform)
        private readonly string? _linuxDevicePath;       // Linux
        private readonly UsbDevice? _winUsbDevice;        // Windows

        private FeatureReportAccess(HidStream? hidSharpStream, string? linuxDevicePath, UsbDevice? winUsbDevice)
        {
            _hidSharpStream = hidSharpStream;
            _linuxDevicePath = linuxDevicePath;
            _winUsbDevice = winUsbDevice;
        }

        /// <param name="requiredReportId">
        /// Used on Linux/macOS to pick the correct HID interface out of
        /// however many share this VID/PID, by checking which one's
        /// descriptor actually declares this report. Not needed on
        /// Windows - after the Zadig rebind there's inherently only one
        /// WinUsb-visible interface for this VID/PID, so it's opened
        /// directly with no descriptor search.
        /// </param>
        public static FeatureReportAccess Open(int vendorId, int productId, byte requiredReportId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return OpenWindows(vendorId, productId);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OpenLinux(vendorId, productId, requiredReportId);

            return OpenHidSharp(vendorId, productId, requiredReportId);
        }

        private static FeatureReportAccess OpenWindows(int vendorId, int productId)
        {
            var device = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(vendorId, productId));
            if (device is null)
                throw new IOException(
                    $"no WinUsb device found for VID_{vendorId:X4}&PID_{productId:X4} - " +
                    "confirm the config interface was rebound to WinUsb via Zadig, and " +
                    "that no other software (Wacom driver/service, Windows' own inbox " +
                    "digitizer driver) has re-claimed it since");

            return new FeatureReportAccess(null, null, device);
        }

        private static FeatureReportAccess OpenLinux(int vendorId, int productId, byte requiredReportId)
        {
            foreach (var dev in DeviceList.Local.GetHidDevices(vendorID: vendorId, productID: productId))
            {
                byte[] featureIds;
                try
                {
                    featureIds = dev.GetReportDescriptor().FeatureReports.Select(r => r.ReportID).ToArray();
                }
                catch
                {
                    continue;
                }

                if (!featureIds.Contains(requiredReportId))
                    continue;

                var hidrawName = Path.GetFileName(dev.DevicePath);
                var devPath = $"/dev/{hidrawName}";

                if (!File.Exists(devPath))
                    throw new FileNotFoundException($"expected hidraw node not found: {devPath}", devPath);

                return new FeatureReportAccess(null, devPath, null);
            }

            throw new IOException($"no interface exposing report {requiredReportId} was found for VID_{vendorId:X4}&PID_{productId:X4}");
        }

        private static FeatureReportAccess OpenHidSharp(int vendorId, int productId, byte requiredReportId)
        {
            foreach (var dev in DeviceList.Local.GetHidDevices(vendorID: vendorId, productID: productId))
            {
                byte[] featureIds;
                try
                {
                    featureIds = dev.GetReportDescriptor().FeatureReports.Select(r => r.ReportID).ToArray();
                }
                catch
                {
                    continue;
                }

                if (!featureIds.Contains(requiredReportId))
                    continue;

                var config = new OpenConfiguration();
                config.SetOption(OpenOption.Exclusive, false);

                if (dev.TryOpen(config, out var stream))
                    return new FeatureReportAccess(stream, null, null);
            }

            throw new IOException($"no interface exposing report {requiredReportId} was found for VID_{vendorId:X4}&PID_{productId:X4}");
        }

        /// <param name="length">total report length including the report-ID byte</param>
        public bool GetFeature(byte reportId, int length, out byte[]? buffer)
        {
            try
            {
                buffer = _linuxDevicePath is not null
                    ? LinuxRawHidFeature.GetFeature(_linuxDevicePath, reportId, length)
                    : _winUsbDevice is not null
                        ? WindowsRawHidFeature.GetFeature(_winUsbDevice, reportId, length)
                        : GetFeatureViaHidSharp(reportId, length);
                return true;
            }
            catch (Exception ex)
            {
                buffer = null;
                return false;
            }
        }

        private byte[] GetFeatureViaHidSharp(byte reportId, int length)
        {
            var buffer = new byte[length];
            buffer[0] = reportId;
            _hidSharpStream!.GetFeature(buffer);
            return buffer;
        }

        public bool SetFeature(byte[] data)
        {
            try
            {
                if (_linuxDevicePath is not null)
                    LinuxRawHidFeature.SetFeature(_linuxDevicePath, data);
                else if (_winUsbDevice is not null)
                    WindowsRawHidFeature.SetFeature(_winUsbDevice, data);
                else
                    _hidSharpStream!.SetFeature(data);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }



        public void Dispose()
        {
            _hidSharpStream?.Dispose();
            _winUsbDevice?.Close();
        }
    }
}
