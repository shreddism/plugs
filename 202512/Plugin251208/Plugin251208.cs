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

            // This is bullshit.

            const int SIZE = 5;

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        protected override void ConsumeState()  // Report
        {
            if (State is ITabletReport report)
            {

                
                for (int i = (SIZE - 1); i > 0; i--) {
                    pos[i] = pos[i - 1];
                }
                pos[0] = report.Position;

                for (int i = (SIZE - 1); i > 0; i--) {
                    dir[i] = dir[i - 1];
                }

                
                dir[0] = pos[0] - pos[1];


                vel1 = vel0;
                vel0 = Vector2.Distance(pos[0], pos[1]);
                
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
                double j = -1 * Math.Atan2(dir[1].Y, dir[1].X);
                 //((Math.Cos(j) * dir0.X - Math.Sin(j) * dir0.Y), (Math.Sin(j) * dir0.X + Math.Cos(j) * dir0.Y));
                /* adjdir1.X = (float)(Math.Cos(j) * dir1.X - Math.Sin(j) * dir1.Y);
                adjdir1.Y = (float)(Math.Sin(j) * dir1.X + Math.Cos(j) * dir1.Y);
                adjdir0.X = (float)(Math.Cos(j) * dir0.X - Math.Sin(j) * dir0.Y);
                adjdir0.Y = (float)(Math.Sin(j) * dir0.X + Math.Cos(j) * dir0.Y);
                adjdir2.X = (float)(Math.Cos(j) * dir2.X - Math.Sin(j) * dir2.Y);
                adjdir2.Y = (float)(Math.Sin(j) * dir2.X + Math.Cos(j) * dir2.Y); */

                if (reportStopwatch.Restart().TotalMilliseconds > 10)
                for (int i = 0; i < SIZE; i++) {
                    dir[i] = Vector2.Zero;
                    
                }
                    
                bool panic = false;

                for (int i = 0; i < SIZE; i++) {
                adjdir[i].X = (float)(Math.Cos(j) * dir[i].X - Math.Sin(j) * dir[i].Y);
                adjdir[i].Y = (float)(Math.Sin(j) * dir[i].X + Math.Cos(j) * dir[i].Y);
                }
                float please = 0;
                for (int i = 2; i < SIZE; i++) {
                    please += adjdir[i].Y;
                    if (adjdir[i].Y > (Math.Abs(adjdir[i].X * 10)))
                       panic = true;
                }
                float help = (adjdir[0].Y - please) /  (float)(SIZE - 1);

                Console.WriteLine(adjdir[0]);

                /* Console.WriteLine("0: " + adjdir0);
                Console.WriteLine("1: " + adjdir1);
                Console.WriteLine("2: " + adjdir2); */
                
                if (!panic)
                adjdir[0].Y = help;

                strdir0.X = (float)(Math.Cos(-j) * adjdir[0].X - Math.Sin(-j) * adjdir[0].Y);
                strdir0.Y = (float)(Math.Sin(-j) * adjdir[0].X + Math.Cos(-j) * adjdir[0].Y);
                adjpos1 = adjpos0;

                if (reportStopwatch.Restart().TotalMilliseconds < 10)
                    adjpos0 = adjpos1 + strdir0;
                else {
                    adjpos0 = pos[0];
                    
                }
                
                strpos0 = adjpos0;
                double XD = Vector2.Dot(Vector2.Normalize(strdir0), Vector2.Normalize(pos[0] - adjpos0));

                bool hairpincheck = false;

                for (int i = 1; i < SIZE; i++) {
                    if (Vector2.Dot(Vector2.Normalize(dir[i]), Vector2.Normalize(dir[i - 1])) < 0.9)
                        hairpincheck = true;
                }
                
                if (double.IsFinite(XD)) {
                    if (!hairpincheck)
                        adjpos0 = Vector2.Lerp(adjpos0, pos[0], (float)Math.Max(0, XD));
                  //  Console.WriteLine(Math.Max(0.01f, Vector2.Distance(pos[0], adjpos0) / 100.0f));
                }
                else adjpos0 = Vector2.Lerp(adjpos0, pos[0], (float)Math.Max(0, 0.1f));

                if (!vec2IsFinite(adjpos0))
                adjpos0 = pos[0];

                
                
               // Console.WriteLine(adjdir[0]);
                


                


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

        Vector2 adjpos0, adjpos1, strdir0, strpos0, strpos1;
        double vel0, vel1, accel0, accel1, jerk0, jerk1, snap0, snap1, ra1accel0, ra1accel1, ra1ajerk0, ra1ajerk1, ra1asnap0;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 300);
        bool consume;
        uint press;
        Vector2[] pos = new Vector2[SIZE];
        Vector2[] dir = new Vector2[SIZE];
        Vector2[] adjdir = new Vector2[SIZE];
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}