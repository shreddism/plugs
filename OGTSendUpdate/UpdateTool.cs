using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;      

/// This should probably always come before render tool in OTD filter order, but I'm unsure on whether it matters.
/// Calculates some tablet metrics then sends them out, hopefully to be eaten up by OTDGraphTool.
/// As you can see, the format seen on each report is (example)
/// 
/// v12.34567890
/// a12.34567890
/// j-12.34567890
/// s-12.34567890
/// x
/// v6.7
/// a-6.7
/// j-6.7
/// s6.7
/// x
/// 
/// and so on. The graph tool takes numbers after v, a, j, s as data points for their respective v, a, j, s arrays.
/// After this, the update sign x is seen, which makes the graph tool update the arrays with new data.
/// t for "test" just tells the graph tool to assume it got new data. Lines are the result.

namespace OGTUpdateTool
{
    [PluginName("OGTUpdateTool")]
    public class UpdateTool : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public UpdateTool() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        [BooleanProperty("Wire (Hover Over This)", ""), DefaultPropertyValue(true), ToolTip
        (
            "Set this to true and frequency to 0 in most cases. Otherwise, prepare for a stress test if stress test is enabled."
        )]
        public bool Wire { get; set; }

        [BooleanProperty("Stress Test (Hover Over This)", ""), DefaultPropertyValue(false), ToolTip
        (
            "Keep this at false unless you are testing if your CPU will be able to handle graphing a theoretical (frequency)Hz tablet."
        )]
        public bool Stress { get; set; }

        protected override void ConsumeState()
        {
            if (State is ITabletReport report)
            {
                raw2Pos = raw1Pos;
                raw1Pos = report.Position;
                lastVelocity = velocity;
                lastAccel = accel;
                lastJerk = jerk;
                velocity = Math.Sqrt(Math.Pow(raw1Pos.X - raw2Pos.X, 2) + Math.Pow(raw1Pos.Y - raw2Pos.Y, 2));
                accel = velocity - lastVelocity;
                jerk = accel - lastAccel;
                snap = jerk - lastJerk;
                lastChange = change;
                lastRa1Index = ra1Index;
                lastIndex = index;
                index = (jerk + lastJerk + accel + lastAccel) / (Math.Log((Math.Pow(lastVelocity, 1.1) + Math.E) / Math.E + 1) + 1);
                ra1Index = lastIndex + index;
                change = ra1Index - lastRa1Index;

                Console.Write("v");
                Console.WriteLine(velocity);
                Console.Write("a");
                Console.WriteLine(accel);
                Console.Write("j");
                Console.WriteLine(index);
                Console.Write("s");
                Console.WriteLine(change);
                Console.WriteLine("x");
                
                if (Wire)
                    UpdateState();

            }
            else OnEmit();
        }

        protected override void UpdateState()
        {
            if (State is ITabletReport report && PenIsInRange())
            {
                if (Stress)
                    Console.WriteLine("t");
                    
                    report.Position = raw1Pos;

                State = report;
                OnEmit();
            }
        }

        public double velocity, accel, jerk, snap, lastVelocity, lastAccel, lastJerk, count, lastIndex, index, lastChange, change, ra1Index, lastRa1Index;
        public Vector2 raw1Pos, raw2Pos;

    }
}