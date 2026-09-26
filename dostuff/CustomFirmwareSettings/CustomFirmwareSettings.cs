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
            set => _freq = Math.Clamp(value, 100, 1500);
            get => _freq;
        }
        public int _freq;

        [BooleanProperty("Pen Clicks/Buttons", ""), DefaultPropertyValue(false), ToolTip
        (
            "f"
        )]
        public bool buttons { set; get; }

        [BooleanProperty("motionsync", ""), DefaultPropertyValue(false), ToolTip
        (
            "m"
        )]
        public bool motionsync { set; get; }

        [BooleanProperty("Persistence", ""), DefaultPropertyValue(false), ToolTip
        (
            "m"
        )]
        public bool persistence { set; get; }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        private FeatureReportAccess? _stream;

        public void Consume(IDeviceReport value) => Emit?.Invoke(value);
        
        int tabletVendorID;
        int tabletProductID;
        int tabletType;

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
            if (tabletType == 1) {
                _stream = FeatureReportAccess.Open(tabletVendorID, tabletProductID, 102);
            }
            return _stream;
        }

        private void ApplySettings() {
            if (tabletType == 1) {
                if (_stream.GetFeature(102, length: 5, out var buffer1)) { 
                    if (filtering) {
                        buffer1[1] = 0xf8;
                    }
                    else {
                        buffer1[1] = 0xf0;
                    }
                    _stream.SetFeature(buffer1);
                }
                
                if (_stream.GetFeature(96, length: 64, out var buffer2)) {
                    if (buffer2[1] == 84 && buffer2[2] == 86) {
                        if (buffer2[63] != 66) {
                            Console.WriteLine("!!");
                        }

                        byte[] ptkx70tvwrite = new byte[64];
                        ptkx70tvwrite[0] = 96;
                        ptkx70tvwrite[1] = 84;
                        ptkx70tvwrite[2] = 86;
                        ptkx70tvwrite[3] = 1;
                        ptkx70tvwrite[4] = (byte)((freq) & 0xff);
                        ptkx70tvwrite[5] = (byte)((freq >> 8) & 0xff);

                        if (buttons)
                            ptkx70tvwrite[6] = 1;
                        else
                            ptkx70tvwrite[6] = 0;

                        if (((buffer2[7] & 0x04) > 0) && motionsync) 
                            ptkx70tvwrite[7] = 1;
                        else
                            ptkx70tvwrite[7] = 0;

                        if (((buffer2[7] & 0x08) > 0) && persistence)
                            ptkx70tvwrite[8] = 1;
                        else 
                            ptkx70tvwrite[8] = 0;

                    _stream.SetFeature(ptkx70tvwrite);

                    }
                }
            }
        }

        public void SetTargetBytes() {
            if (tabletVendorID == 1386) {
                if (tabletProductID == 1013 || tabletProductID == 1015 || tabletProductID == 1017) {
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