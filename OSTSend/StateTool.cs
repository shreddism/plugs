using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;      

/// Calculates some colors then sends them out, hopefully to be eaten up by OTDStateTool.
/// As you can see, the format seen on each report is (example)
/// 
/// vr1
/// vg2
/// vb3
/// ar1
/// ag2
/// ab3
/// jr1                                         
/// jg2
/// jb3
/// sr1
/// sg2
/// sb3
/// xx
/// 
/// and so on, except when pen isn't on tablet, which is when ii is called.


namespace OTDStateTool
{
    [PluginName("OTDStateTool")]
    public class ColorTool : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public ColorTool() : base()
        {
        }

        Anticlutter fn = new Anticlutter();     // Look 👀 at the anticlutter file 👀

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        [BooleanProperty("Wire (Hover Over This)", ""), DefaultPropertyValue(true), ToolTip
        (
            "Set to true. Make frequency refresh rate of screen."
        )]
        public bool Wire { get; set; }

        protected override void ConsumeState()
        {
            if (State is ITabletReport report)
            {
                raw3Pos = raw2Pos;
                raw2Pos = raw1Pos;
                raw1Pos = report.Position;
                lastVelocity = velocity;
                lastAccel = accel;
                lastJerk = jerk;
                velocity = Math.Sqrt(Math.Pow(raw1Pos.X - raw2Pos.X, 2) + Math.Pow(raw1Pos.Y - raw2Pos.Y, 2));
                accel = velocity - lastVelocity;
                jerk = accel - lastAccel;
                snap = jerk - lastJerk;




            
                ar = (int)Double.Round(Math.Clamp(Math.Abs(accel), 0, 255), 1);
                jg = (int)Double.Round(Math.Clamp(Math.Abs(jerk), 0, 255), 1);
                sb = (int)Double.Round(Math.Clamp(Math.Sqrt(Math.Abs(jerk * accel)), 0, 255), 1);
                vr = (int)Double.Round(Math.Clamp(127 + 127 * Math.Pow(Vector2.Dot(Vector2.Normalize(raw1Pos - raw2Pos), Vector2.Normalize(raw2Pos - raw3Pos)), 9), 0, 255), 1);
    
                

                Console.Write("vr");
                Console.WriteLine(vr);
                Console.Write("vg");
                Console.WriteLine(vg);
                Console.Write("vb");
                Console.WriteLine(vb);
                Console.Write("ar");
                Console.WriteLine(ar);
                Console.Write("ag");
                Console.WriteLine(ag);
                Console.Write("ab");
                Console.WriteLine(ab);
                Console.Write("jr");
                Console.WriteLine(jr);
                Console.Write("jg");
                Console.WriteLine(jg);
                Console.Write("jb");
                Console.WriteLine(jb);
                Console.Write("sr");
                Console.WriteLine(sr);
                Console.Write("sg");
                Console.WriteLine(sg);
                Console.Write("sb");
                Console.WriteLine(sb);
                Console.WriteLine("xx");

                fn.reset4(ar, jg, sb, vr);

                
                
                
                if (Wire)
                    UpdateState();

            }
            else OnEmit();
        }

        protected override void UpdateState()
        {
            if (State is ITabletReport report && PenIsInRange())
            {
                report.Position = raw1Pos;
                State = report;
                OnEmit();
            }
            else Console.WriteLine("ii");
        }

            

        public int vr, vg, vb, ar, ag, ab, jr, jg, jb, sr, sg, sb;
        public double velocity, accel, jerk, snap, lastVelocity, lastAccel, lastJerk, count;
        public Vector2 raw1Pos, raw2Pos, raw3Pos;

    }
}