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

namespace CustomFirmwareSettings {
    public struct FWSettings 
    {
        public bool Filtering;
        public uint Frequency;
        public bool PenButtons;
        public bool MotionSync;
        public bool Persistence;

        public FWSettings(bool filtering, uint frequency, bool penbuttons, bool motionsync, bool persistence) {
            Filtering = filtering;
            Frequency = frequency;
            PenButtons = penbuttons;
            MotionSync = motionsync;
            Persistence = persistence;
        }
    }

    public class SettingsApplier
    {

        public int tabletType;
        int tabletVendorID;
        int tabletProductID;
        public FeatureReportAccess? reportStream;

        public FWSettings read, write;

        public SettingsApplier(int VID, int PID) {
            tabletVendorID = VID;
            tabletProductID = PID;
            if (tabletVendorID == 1386) {
                if (tabletProductID == 1013 || tabletProductID == 1015 || tabletProductID == 1017) {
                    tabletType = 1;
                }
                if (tabletProductID == 782 || tabletProductID == 803 || tabletProductID == 890 || tabletProductID == 891) {
                    tabletType = 2;
                    Console.WriteLine("tablet type 2");
                }
                if (tabletProductID == 884 || tabletProductID == 886) {
                    tabletType = 3;
                }
            }
        }

        public void OpenConfigInterface() {
            if (tabletType == 1) {
                reportStream = FeatureReportAccess.Open(tabletVendorID, tabletProductID, 102);
            }
            if (tabletType == 2) {
                reportStream = FeatureReportAccess.Open(tabletVendorID, tabletProductID, 33);
                if (reportStream == null) {
                    Console.WriteLine("report stream null");
                }
                else {
                    Console.WriteLine("report stream opened successfully");
                }
            }
            if (tabletType == 3) {
                reportStream = FeatureReportAccess.Open(tabletVendorID, tabletProductID, 102);
            }
        }

        public void Read(bool copy) {
            if (tabletType == 1) {
                if (reportStream.GetFeature(102, length: 5, out var ptkx70read1)) { 
                    read.Filtering = (ptkx70read1[1] == 0xf8);
                }

                if (reportStream.GetFeature(96, length: 64, out var ptkx70read2)) {
                    if (ptkx70read2[1] == 84 && ptkx70read2[2] == 86) {
                        read.Frequency = ((uint)ptkx70read2[4] | ((uint)(ptkx70read2[5]) << 8));
                        read.PenButtons = (ptkx70read2[6] > 0);
                        read.MotionSync = (((ptkx70read2[7] & 0x04) > 0) && (ptkx70read2[8] > 0));
                        read.Persistence = ((ptkx70read2[7] & 0x10) > 0);
                    }
                }
            }
            if (tabletType == 2) {
                if (reportStream.GetFeature(33, length: 1, out var ctlx72x80read1)) {
                    read.Filtering = ((ctlx72x80read1[0] & 0x01) == 0);
                }
                else read.Filtering = false;

                if (reportStream.GetFeature(36, length: 32, out var ctlx72x80read2)) {
                    if (ctlx72x80read2[1] == 84 && ctlx72x80read2[2] == 86) {
                        read.Frequency = ((uint)ctlx72x80read2[4] | ((uint)(ctlx72x80read2[5]) << 8));
                        read.PenButtons = (ctlx72x80read2[6] > 0);
                        read.MotionSync = (((ctlx72x80read2[7] & 0x04) > 0) && (ctlx72x80read2[8] > 0));
                        read.Persistence = ((ctlx72x80read2[7] & 0x10) > 0);
                    }
                }
            }
            if (tabletType == 3) {
                if (reportStream.GetFeature(102, length: 5, out var ptkx70read1)) { 
                    read.Filtering = (ptkx70read1[1] == 0xf8);
                }

                if (reportStream.GetFeature(96, length: 64, out var ptkx70read2)) {
                    if (ptkx70read2[1] == 84 && ptkx70read2[2] == 86) {
                        read.Frequency = ((uint)ptkx70read2[4] | ((uint)(ptkx70read2[5]) << 8));
                        read.PenButtons = (ptkx70read2[6] > 0);
                        read.MotionSync = (((ptkx70read2[7] & 0x04) > 0) && (ptkx70read2[8] > 0));
                        read.Persistence = ((ptkx70read2[7] & 0x10) > 0);
                    }
                }
            }
            if (copy) {
                write = read;
            }
        }

        public void Apply() {
            if (tabletType == 1) {
                if (reportStream.GetFeature(102, length: 5, out var ptkx70read1)) { 
                    if (write.Filtering) {
                        ptkx70read1[1] = 0xf8;
                    }
                    else {
                        ptkx70read1[1] = 0xf0;
                    }
                    reportStream.SetFeature(ptkx70read1);
                }
                
                if (reportStream.GetFeature(96, length: 64, out var ptkx70read2)) {
                    if (ptkx70read2[1] == 84 && ptkx70read2[2] == 86) {
                        byte[] ptkx70tvwrite = new byte[64];
                        ptkx70tvwrite[0] = 96;
                        ptkx70tvwrite[1] = 84;
                        ptkx70tvwrite[2] = 86;
                        ptkx70tvwrite[3] = 1;
                        ptkx70tvwrite[4] = (byte)((write.Frequency) & 0xff);
                        ptkx70tvwrite[5] = (byte)((write.Frequency >> 8) & 0xff);

                        if (write.PenButtons)
                            ptkx70tvwrite[6] = 1;
                        else
                            ptkx70tvwrite[6] = 0;

                        if (((ptkx70read2[7] & 0x04) > 0) && write.MotionSync) 
                            ptkx70tvwrite[7] = 1;
                        else
                            ptkx70tvwrite[7] = 0;

                        if (((ptkx70read2[7] & 0x08) > 0) && write.Persistence)
                            ptkx70tvwrite[8] = 1;
                        else 
                            ptkx70tvwrite[8] = 0;

                        reportStream.SetFeature(ptkx70tvwrite);
                    }
                }
            }

            if (tabletType == 2) {
                if (reportStream.GetFeature(33, length: 1, out var ctlx72x80read1)) {
                    if ((((ctlx72x80read1[0] & 0x01) == 0) && !write.Filtering) ||
                        (((ctlx72x80read1[0] & 0x01) != 0) && write.Filtering)) {
                        ctlx72x80read1[0] = (byte)(ctlx72x80read1[0] ^ 0x01);
                    }

                    Console.WriteLine("read1 success");

                    reportStream.SetFeature(ctlx72x80read1);
                }
                else {
                    Console.WriteLine("read1 failure (expected, is fine)");
                }

                if (reportStream.GetFeature(36, length: 32, out var ctlx72x80read2)) {
                    if (ctlx72x80read2[1] == 84 && ctlx72x80read2[2] == 86) {
                        Console.WriteLine("read2 success");
                        byte[] ctlx72x80tvwrite = new byte[32];
                        ctlx72x80tvwrite[0] = 36;
                        ctlx72x80tvwrite[1] = 84;
                        ctlx72x80tvwrite[2] = 86;
                        ctlx72x80tvwrite[3] = 1;
                        ctlx72x80tvwrite[4] = (byte)((write.Frequency) & 0xff);
                        ctlx72x80tvwrite[5] = (byte)((write.Frequency >> 8) & 0xff);

                        if (write.PenButtons)
                            ctlx72x80tvwrite[6] = 1;
                        else
                            ctlx72x80tvwrite[6] = 0;

                        if (((ctlx72x80read2[7] & 0x04) > 0) && write.MotionSync) 
                            ctlx72x80tvwrite[7] = 1;
                        else
                            ctlx72x80tvwrite[7] = 0;

                        reportStream.SetFeature(ctlx72x80tvwrite);
                    }
                    else {
                        Console.WriteLine("read2 weird failure");
                    }
                }
                else {
                    Console.WriteLine("read2 failure");
                }
            }

            if (tabletType == 3) {
                if (reportStream.GetFeature(102, length: 5, out var ctlx100read1)) { 
                    if (write.Filtering) {
                        ctlx100read1[1] = 0xf8;
                    }
                    else {
                        ctlx100read1[1] = 0xf0;
                    }
                    reportStream.SetFeature(ctlx100read1);
                }
                
                if (reportStream.GetFeature(96, length: 64, out var ctlx100read2)) {
                    if (ctlx100read2[1] == 84 && ctlx100read2[2] == 86) {
                        byte[] ctlx100tvwrite = new byte[64];
                        ctlx100tvwrite[0] = 96;
                        ctlx100tvwrite[3] = 84;
                        ctlx100tvwrite[4] = 86;
                        ctlx100tvwrite[5] = 1;
                        ctlx100tvwrite[6] = (byte)((write.Frequency) & 0xff);
                        ctlx100tvwrite[7] = (byte)((write.Frequency >> 8) & 0xff);

                        if (write.PenButtons)
                            ctlx100tvwrite[8] = 1;
                        else
                            ctlx100tvwrite[8] = 0;

                        if (((ctlx100read2[6] & 0x04) > 0) && write.MotionSync) 
                            ctlx100tvwrite[9] = 1;
                        else
                            ctlx100tvwrite[9] = 0;

                        if (((ctlx100read2[7] & 0x08) > 0) && write.Persistence)
                            ctlx100tvwrite[10] = 1;
                        else 
                            ctlx100tvwrite[10] = 0;

                        reportStream.SetFeature(ctlx100tvwrite);
                    }
                }
            }
        }
    }
}