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

        [BooleanProperty("Filtering", ""), DefaultPropertyValue(false)]
        public bool filtering { set; get; }

        [Property("Frequency"), DefaultPropertyValue(1000)]
        public int frequency
        {
            set => _frequency = Math.Clamp(value, 133, 1000);
            get => _frequency;
        }
        public int _frequency;

        [BooleanProperty("Pen Clicks/Buttons", ""), DefaultPropertyValue(true)]
        public bool penbuttons { set; get; }

        [BooleanProperty("Motion Sync", ""), DefaultPropertyValue(true)]
        public bool motionsync { set; get; }

        [BooleanProperty("Persistence", ""), DefaultPropertyValue(false)]
        public bool persistence { set; get; }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        public SettingsApplier settings;

        public void Consume(IDeviceReport value) => Emit?.Invoke(value);

        [OnDependencyLoad]
        public void initialize() {
            if (drv is Driver driver) {
                var tablet = driver.InputDevices.Where(dev => dev.Properties == Tablet.Properties).FirstOrDefault();
                var device = tablet?.InputDevices.Where(dev => dev.Configuration == Tablet.Properties).FirstOrDefault();
                if (device is InputDevice inputDevice) {
                    var id = inputDevice.Identifier;

                    settings = new SettingsApplier(id.VendorID, id.ProductID);

                    if (settings.tabletType == 0) {
                        Log.Write("cfw", "unsupported tablet", LogLevel.Error);
                        init = true;
                        return;
                    }

                    settings.write = new FWSettings(
                        filtering,
                        frequency,
                        penbuttons,
                        motionsync,
                        persistence
                    );

                    if (!init) {
                        try
                        {
                            settings.OpenConfigInterface();
                            if (settings.reportStream is null)
                                return;

                            settings.Apply();

                            init = true;

                            return;
                        }
                        catch (Exception ex)
                        {
                            Log.Write("cfw", $"initialize failed: {ex}", LogLevel.Error);
                            init = true;

                            return;
                        }
                    }
                }
            }
        }

        bool init;

        public void Dispose() {
            settings.reportStream?.Dispose();
            settings.reportStream = null;
        }

        [TabletReference]
        public TabletReference Tablet { get; set; }

        [Resolved]
        public IDriver drv { get; set; }
    }

    [PluginName("Custom Firmware Settings Binding")]
    public class CustomFirmwareBinding : IStateBinding 
    {
        [Property("Action"), DefaultPropertyValue("Toggle Pen Buttons"), PropertyValidated(nameof(actionModes)), ToolTip
        (
            "Changes what pressing and holding the binding will do."
        )]
        public string Action { get; set; } = string.Empty;

        public static IEnumerable<string> actionModes { get; set; } = new List<string> { "Toggle Pen Buttons", "Toggle Filtering" };

        public int actionMode;

        [OnDependencyLoad]
        public void initialize() {
            actionMode = Action switch {
                "Toggle Pen Buttons" => 1,
                "Toggle Filtering" => 2,
                _ => 0
            };

            if (drv is Driver driver) {
                var tablet = driver.InputDevices.Where(dev => dev.Properties == Tablet.Properties).FirstOrDefault();
                var device = tablet?.InputDevices.Where(dev => dev.Configuration == Tablet.Properties).FirstOrDefault();
                if (device is InputDevice inputDevice) {
                    var id = inputDevice.Identifier;

                    settings = new SettingsApplier(id.VendorID, id.ProductID);

                    if (settings.tabletType == 0) {
                        Log.Write("cfw", "unsupported tablet", LogLevel.Error);
                        return;
                    }

                    if (!init) {
                        try
                        {
                            settings.OpenConfigInterface();
                            if (settings.reportStream is null)
                                return;

                            init = true;

                            return;
                        }
                        catch (Exception ex)
                        {
                            Log.Write("cfw", $"initialize failed: {ex}", LogLevel.Error);

                            return;
                        }
                    }
                }
            }
        }

        public void Press(TabletReference tablet, IDeviceReport report) {
            if (init) {
                settings.Read(true);
                settings.write.Persistence = false;
                if (actionMode == 1) {
                    if ((settings.writemask & 0x04) == 0) {
                        Log.Write("cfw", "Cannot toggle pen buttons.", LogLevel.Info);
                        return;
                    }
                    settings.write.PenButtons = !settings.write.PenButtons;
                    settings.Apply();
                    settings.Read(false);
                    if (settings.read.PenButtons != settings.write.PenButtons) {
                        Log.Write("cfw", "Failed toggle.", LogLevel.Error);
                    }
                    else {
                        if (settings.read.PenButtons) {
                            Log.Write("cfw", "Enabled pen buttons.", LogLevel.Info);
                        }
                        else {
                            Log.Write("cfw", "Disabled pen buttons.", LogLevel.Info);
                        }
                    }
                }
                else if (actionMode == 2) {
                    if ((settings.writemask & 0x01) == 0) {
                        Log.Write("cfw", "Cannot toggle filtering.", LogLevel.Info);
                        if (settings.modReportPresent) {
                            Log.Write("cfw", "A modded firmware may have this force-disabled.", LogLevel.Info);
                        }
                    }
                    settings.write.Filtering = !settings.write.Filtering;
                    settings.Apply();
                    settings.Read(false);
                    if (settings.read.Filtering != settings.write.Filtering) {
                        Log.Write("cfw", "Failed toggle.", LogLevel.Error);
                    }
                    else {
                        if (settings.read.Filtering) {
                            Log.Write("cfw", "Enabled firmware filtering.", LogLevel.Info);
                        }
                        else {
                            Log.Write("cfw", "Disabled firmware filtering.", LogLevel.Info);
                        }
                    }
                }
            }
        }

        public void Release(TabletReference tablet, IDeviceReport report) {
            if (init) {
                return;
            }
        }

        bool init;

        public SettingsApplier settings;

        [TabletReference]
        public TabletReference Tablet { get; set; }

        [Resolved]
        public IDriver drv { get; set; }
    }
}