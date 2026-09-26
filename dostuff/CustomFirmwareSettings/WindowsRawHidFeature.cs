using System;
using System.IO;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace CustomFirmwareSettings
{
    /// <summary>
    /// GET_REPORT/SET_REPORT as raw USB control transfers, for the one
    /// interface that's been rebound from the HID class driver to WinUsb
    /// (via Zadig) because Windows' HID stack won't expose it to
    /// HidSharpCore at all - either because of the exact issue seen with
    /// this PTK-470, or in general whenever something else on the system
    /// (inbox digitizer driver, vendor software, etc.) claims it first.
    ///
    /// Only ever call this on Windows, for the specific VID/PID that was
    /// rebound - see FeatureReportAccess for the platform gate.
    /// </summary>
    internal static class WindowsRawHidFeature
    {
        private const byte ReportTypeFeature = 3;

        // GET_REPORT: device-to-host, class request, recipient = interface
        private const byte RequestTypeGet = 0xA1;
        private const byte RequestGetReport = 0x01;

        // SET_REPORT: host-to-device, class request, recipient = interface
        private const byte RequestTypeSet = 0x21;
        private const byte RequestSetReport = 0x09;

        /// <summary>
        /// After a Zadig per-interface rebind, the device generally
        /// enumerates as if it has a single interface 0 from LibUsbDotNet's
        /// point of view, regardless of the original composite interface
        /// number. Confirmed working for the PTK-470's config interface.
        ///
        /// IMPORTANT: the driver Zadig binds matters, not just the
        /// interface number. WinUSB specifically failed GET_REPORT/
        /// SET_REPORT on this device even with the correct interface
        /// and an explicit ClaimInterface - libusbK, bound to the same
        /// physical interface, works correctly with the plain
        /// ControlTransfer call below and no claim step. If this stops
        /// working after any Windows/driver update, check the Zadig
        /// driver assignment before re-deriving the interface number -
        /// that was the false lead last time, not this constant.
        /// </summary>
        private const byte WinUsbInterfaceNumber = 0;

        public static byte[] GetFeature(UsbDevice device, byte reportId, int length)
        {
            var buffer = new byte[length];

            var setup = new UsbSetupPacket(
                RequestTypeGet,
                RequestGetReport,
                (short)((ReportTypeFeature << 8) | reportId),
                WinUsbInterfaceNumber,
                (short)length);

            bool ok = device.ControlTransfer(ref setup, buffer, buffer.Length, out int transferred);
            if (!ok)
                throw new IOException($"GET_REPORT failed for report {reportId}");
            if (transferred != length)
                throw new IOException($"GET_REPORT for report {reportId} returned {transferred} bytes, expected {length}");

            return buffer;
        }

        /// <param name="data">full report bytes, data[0] must be the report ID</param>
        public static void SetFeature(UsbDevice device, byte[] data)
        {
            var setup = new UsbSetupPacket(
                RequestTypeSet,
                RequestSetReport,
                (short)((ReportTypeFeature << 8) | data[0]),
                WinUsbInterfaceNumber,
                (short)data.Length);

            bool ok = device.ControlTransfer(ref setup, data, data.Length, out int transferred);
            if (!ok)
                throw new IOException("SET_REPORT failed");
            if (transferred != data.Length)
                throw new IOException($"SET_REPORT sent {transferred} of {data.Length} bytes");
        }
    }
}
