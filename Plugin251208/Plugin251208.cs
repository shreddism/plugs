using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace Plugin251208
{
    [PluginName("Plugin251208")]
    public class Plugin251208 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Plugin251208() : base()
        {
        }

            // This is bullshit. Very fun to mess with imo

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        protected override void ConsumeState()  // Report
        {
            if (State is ITabletReport report)
            {
                pos3 = pos2;
                pos2 = pos1;
                pos1 = pos0;
                pos0 = report.Position;

                dir2 = dir1;
                dir1 = dir0;
                dir0 = pos0 - pos1;


                vel1 = vel0;
                vel0 = Vector2.Distance(pos0, pos1);
                
                accel1 = accel0;
                accel0 = vel0 - vel1;
                
                ra1accel1 = ra1accel0;
                ra1accel0 = accel0 + accel1;

                jerk1 = jerk0;
                jerk0 = accel0 - accel1;

                ra1ajerk1 = ra1ajerk0;
                ra1ajerk0 = ra1accel0 - ra1accel1;

                ra1asnap0 = ra1ajerk0 - ra1ajerk1;
                press = report.Pressure;
                consume = true;
                double j = -1 * Math.Atan2(dir1.Y, dir1.X);
                 //((Math.Cos(j) * dir0.X - Math.Sin(j) * dir0.Y), (Math.Sin(j) * dir0.X + Math.Cos(j) * dir0.Y));
                adjdir1.X = (float)(Math.Cos(j) * dir1.X - Math.Sin(j) * dir1.Y);
                adjdir1.Y = (float)(Math.Sin(j) * dir1.X + Math.Cos(j) * dir1.Y);
                adjdir0.X = (float)(Math.Cos(j) * dir0.X - Math.Sin(j) * dir0.Y);
                adjdir0.Y = (float)(Math.Sin(j) * dir0.X + Math.Cos(j) * dir0.Y);
                adjdir2.X = (float)(Math.Cos(j) * dir2.X - Math.Sin(j) * dir2.Y);
                adjdir2.Y = (float)(Math.Sin(j) * dir2.X + Math.Cos(j) * dir2.Y);
                float help = (adjdir0.Y - adjdir2.Y) / 2.0f;

                /* Console.WriteLine("0: " + adjdir0);
                Console.WriteLine("1: " + adjdir1);
                Console.WriteLine("2: " + adjdir2); */

                adjdir0.Y = (float)(Math.Sign(help) * Math.Sqrt(Math.Abs(help)));
                strdir0.X = (float)(Math.Cos(-j) * adjdir0.X - Math.Sin(-j) * adjdir0.Y);
                strdir0.Y = (float)(Math.Sin(-j) * adjdir0.X + Math.Cos(-j) * adjdir0.Y);
                adjpos1 = adjpos0;

                if (reportStopwatch.Restart().TotalMilliseconds < 10)
                    adjpos0 = adjpos1 + strdir0;
                else adjpos0 = pos0;
                
                strpos0 = adjpos0;
                double XD = Vector2.Dot(Vector2.Normalize(strdir0), Vector2.Normalize(pos0 - adjpos0));
                
                if (double.IsFinite(XD)) {
                   adjpos0 = Vector2.Lerp(adjpos0, pos0, (float)Math.Max(0, XD * Math.Min(1, Math.Max(0.05f, Vector2.Distance(pos0, adjpos0) / 100.0f))));
                    Console.WriteLine(Math.Max(0.05f, Vector2.Distance(pos0, adjpos0) / 250.0f));
                }
                else adjpos0 = Vector2.Lerp(adjpos0, pos0, (float)Math.Max(0, 0.1f));

                if (!vec2IsFinite(adjpos0))
                adjpos0 = pos0;

                
                
                Console.WriteLine(adjpos0 - pos0);
                


                


                /* Console.Write("v");
                Console.WriteLine(vel0);
                Console.Write("a");
                Console.WriteLine(accel0);
                Console.Write("j");
                Console.WriteLine(10 * Math.Atan2(adjdir0.Y, adjdir0.X));
                Console.Write("s");
                Console.WriteLine(10 * Math.Atan2(adjdir2.Y, adjdir2.X));
                Console.WriteLine("x");
                Console.WriteLine("d"); */


                UpdateState();
                
                

                






            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
           float alpha = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg);

            if (State is ITabletReport report && PenIsInRange())
            {
                alpha = (float)Math.Clamp(alpha, 0, 1);
                
                report.Position = adjpos0;
                OnEmit();

                
            }
        }

        Vector2 pos0, pos1, pos2, pos3, dir0, dir1, dir2, bgdir, adjdir0, adjdir1, adjdir2, adjpos0, adjpos1, strdir0, strpos0, strpos1;
        double vel0, vel1, accel0, accel1, jerk0, jerk1, snap0, snap1, ra1accel0, ra1accel1, ra1ajerk0, ra1ajerk1, ra1asnap0;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 300);
        bool consume;
        uint press;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}