using System;
using System.Reflection;
using HidSharp;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Numerics;
using OpenTabletDriver;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Devices;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace CustomFirmwareSettings
{
    [PluginName("CustomFirmwareSettings")]
    public class CustomFirmwareSettings : IPositionedPipelineElement<IDeviceReport>
    {
        public CustomFirmwareSettings() : base()
        {
        }

        [BooleanProperty("Filtering", ""), DefaultPropertyValue(false), ToolTip
        (
            "filt"
        )]
        public bool filtering { set; get; }

        [Property("freq"), DefaultPropertyValue(1000), ToolTip
        (
            "Possible range: 133 - 1000, default 1000"
        )]
        public int freq
        {
            set => _freq = Math.Clamp(value, 133, 1000);
            get => _freq;
        }
        public int _freq;

        [BooleanProperty("Pen Clicks/Buttons", ""), DefaultPropertyValue(false), ToolTip
        (
            "f"
        )]
        public bool buttons { set; get; }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        private FeatureReportAccess? _stream;

        public void Consume(IDeviceReport value) => Emit?.Invoke(value);
        
        int tabletVendorID;
        int tabletProductID;
        int tabletType;
        byte FeatureAddress1;
        byte FeatureAddress2;
        int FeatureReport1Length;
        int FeatureReport2Length;

        bool FeatureAddress1Exists;
        bool FeatureAddress2Exists;

        [OnDependencyLoad]
        public void initialize() {
            if (drv is Driver driver) {
                var tablet = driver.InputDevices.Where(dev => dev.Properties == Tablet.Properties).FirstOrDefault();
                var device = tablet?.InputDevices.Where(dev => dev.Configuration == Tablet.Properties).FirstOrDefault();
                if (device is InputDevice inputDevice) {
                    var id = inputDevice.Identifier;

                    tabletVendorID = id.VendorID;
                    tabletProductID = id.ProductID;

                    SetTargetBytes();

                    if (!init) {
                        try
                        {
                            _stream = OpenConfigInterface();
                            if (_stream is null)
                                return;

                            ApplySettings();

                            init = true;

                            return;
                        }
                        catch (Exception ex)
                        {
                            Log.Write("PTK470", $"initialize failed: {ex}", LogLevel.Error);
                            init = true;

                            return;
                        }
                    }
                }
            }
        }

        bool init;


        private FeatureReportAccess? OpenConfigInterface()
        {
            var candidates = DeviceList.Local.GetHidDevices(vendorID: tabletVendorID, productID: tabletProductID);

            foreach (var dev in candidates)
            {
                byte[] featureIds;
                bool address1 = false;
                bool address2 = false;
                try
                {
                    featureIds = dev.GetReportDescriptor().FeatureReports.Select(r => r.ReportID).ToArray();
                }
                catch
                {
                    continue; // some interfaces won't yield a descriptor at all - skip them
                }

                if (!featureIds.Contains(FeatureAddress1))
                    continue;
                else {
                    address1 = true;
                }

                if (!featureIds.Contains(FeatureAddress2))
                    continue;
                else {
                    address2 = true;
                }

                try
                {
                    var access = FeatureReportAccess.Open(dev);
                    Log.Write("PTK470", $"opened config interface at {dev.DevicePath}", LogLevel.Info);
                    FeatureAddress1Exists = address1;
                    FeatureAddress2Exists = address2;

                    if (address1) {
                        var report1 = dev.GetReportDescriptor().FeatureReports.First(r => r.ReportID == FeatureAddress1);
                        FeatureReport1Length = report1.Length;
                    }

                    if (address2) {
                        var report2 = dev.GetReportDescriptor().FeatureReports.First(r => r.ReportID == FeatureAddress2);
                        FeatureReport2Length = report2.Length;
                    }

                    return access;
                }
                catch (Exception ex)
                {
                    Log.Write("PTK470", $"found matching interface at {dev.DevicePath} but could not open it: {ex.Message}", LogLevel.Warning);
                }
            }
            Console.WriteLine(FeatureAddress1);
            Console.WriteLine(FeatureAddress2);
            return null;
        }

        private void ApplySettings() {
            if (tabletType == 1) {
                if (FeatureAddress1Exists) {
                    var buffer1 = ReadFeatureReport(FeatureAddress1, FeatureReport1Length);
                    if (filtering) {
                        buffer1[1] = 0xf8;
                    }
                    else {
                        buffer1[1] = 0xf0;
                    }
                    _stream.SetFeature(buffer1);
                    
                    var check = ReadFeatureReport(FeatureAddress1, FeatureReport1Length);

                    Console.WriteLine(check[1]);
                }
                if (FeatureAddress2Exists) {
                    var buffer2 = ReadFeatureReport(FeatureAddress2, FeatureReport2Length);
                }

            }
        }

        private byte[] ReadFeatureReport(byte FeatureReportAddress, int FeatureReportLength)
        {
            if (_stream is null)
                return null;

            try
            {
                var buffer = _stream.GetFeature(FeatureReportAddress, length: FeatureReportLength);

                return buffer;
            }
            catch (Exception ex)
            {
                Log.Write("PTK470", $"GetFeature failed: {ex}", LogLevel.Error);
                return null;
            }
        }

        private void ToggleFilterReportTest()
        {
            if (_stream is null)
                return;

            try
            {
                // 1. read current state
                var before = _stream.GetFeature(102, length: 5);
                uint flagsBefore = BitConverter.ToUInt32(before, 1);
                Log.Write("PTK470", $"before: {BitConverter.ToString(before)} (flags=0x{flagsBefore:X8})", LogLevel.Info);

                // 2. flip bit 0x08
                uint flagsAfter = flagsBefore ^ 0x08;
                var toWrite = new byte[5];
                toWrite[0] = 102;
                BitConverter.GetBytes(flagsAfter).CopyTo(toWrite, 1);

                // 3. write it back
                _stream.SetFeature(toWrite);
                Log.Write("PTK470", $"wrote: {BitConverter.ToString(toWrite)} (flags=0x{flagsAfter:X8})", LogLevel.Info);

                // 4. read back and verify the tablet actually applied it
                var after = _stream.GetFeature(102, length: 5);
                uint flagsConfirmed = BitConverter.ToUInt32(after, 1);

                if (flagsConfirmed == flagsAfter)
                    Log.Write("PTK470", $"readback OK: {BitConverter.ToString(after)}", LogLevel.Info);
                else
                    Log.Write("PTK470", $"readback MISMATCH: expected 0x{flagsAfter:X8}, got 0x{flagsConfirmed:X8}", LogLevel.Warning);
            }
            catch (Exception ex)
            {
                Log.Write("PTK470", $"toggle test failed: {ex}", LogLevel.Error);
            }
        }

        public void SetTargetBytes() {
            if (tabletVendorID == 1386) {
                if (tabletProductID == 1013 || tabletProductID == 1015 || tabletProductID == 1017) {
                    FeatureAddress1 = 102;
                    FeatureAddress2 = 96;
                    tabletType = 1;
                }
            }
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }


        [TabletReference]
        public TabletReference Tablet { get; set; }

        [Resolved]
        public IDriver drv { get; set; }
    }
}