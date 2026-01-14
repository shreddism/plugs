using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Plugin260112
{
    [PluginName("Plugin260112")]
    public class Plugin260112 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Plugin260112() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        [Property("VT Limiter"), DefaultPropertyValue(2.5f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float vtlimiter { 
            set => _vtlimiter = (float)Math.Clamp(value, 2.5f, 3.0f);
            get => _vtlimiter;
        }
        public float _vtlimiter;

        [Property("Direction Antichatter Inner"), DefaultPropertyValue(1f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float dacInner { 
            set => _dacInner = value;
            get => _dacInner;
        }
        public float _dacInner;

        [Property("Direction Antichatter Outer"), DefaultPropertyValue(5f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float dacOuter { 
            set => _dacOuter = value;
            get => _dacOuter;
        }
        public float _dacOuter;

        public event Action<IDeviceReport> Emit;

        protected override void ConsumeState()
        {
            if (State is ITabletReport report)
            {
                reportTime = (float)reportStopwatch.Restart().TotalMilliseconds;
                if (reportTime < 25) {
                    reportMsAvg += ((reportTime - reportMsAvg) * 0.1f);
                    emergency = false;
                }
                else {
                    emergency = true;
                }
                consume = true;

                
                      
                StatUpdate(report);
                ConditionalUpdate();
                LineDrive();
                

                bottom = -1 * Math.Max(alpha0 - vtlimiter, 0);

                if (top > 0.75f || bottom > 0.75f) {
                    top = 0;
                    bottom = 0;
                }

                
               // testDir = stdir0;

              //  Plot();

         //       testOutput += testDir;
         //       if (emergency)
          //      testOutput = pos0;
            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
            if (State is ITabletReport report && PenIsInRange())
            {
                if (consume) {
                alpha1 = 0;
                // Console.WriteLine("-- Consume");
                // Console.WriteLine(vel0);
                if ((alpha0PreservationSociety > 1) && (top < 1)) {
                    top = alpha0PreservationSociety - 1;
                    bottom = 0;
                }
                else top = 0;

                consume = false;
                }

                float ohmygodbruh = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg) * (1000 / Frequency);

                alpha0 = ((1 - top) * ohmygodbruh) + 1.120f * top;

                alpha0PreservationSociety = alpha0;
            
                alpha0 += (vtlimiter - 1);

                alpha0 = Math.Clamp(alpha0, (vtlimiter - 1), pathpreservationsociety);

                testDir = Trajectory(stdir0, stdir1, stdir2, alpha0) / reportMsAvg;
                sdirt1 = Trajectory(a1stdir0, a1stdir1, a1stdir2, alpha0 + 0.5f) / reportMsAvg;
                testDir = Vector2.Lerp(testDir, sdirt1, pps3);

                testOutput += testDir;

                if (!emergency) {
                    testOutput = Vector2.Lerp(testOutput, pos0 + stdir0 + (stdir0 - stdir1), 0.025f);
                    testOutput = Vector2.Lerp(testOutput, pos0, MathF.Cbrt(FSmootherstep(accel0, 0, -100)));
                }

                if (!vec2IsFinite(testOutput)) {
                    testOutput = pos0;
                }

                report.Position = testOutput;
                OnEmit();
            }
        }

        void StatUpdate(ITabletReport report) {
            pos2 = pos1;
            pos1 = pos0;
            pos0 = report.Position;

            dir2 = dir1;
            dir1 = dir0;
            dir0 = (pos0 - pos1);

            vel1 = vel0;
            vel0 = dir0.Length();

            accel1 = accel0;
            accel0 = (vel0 - vel1);

            ddir1 = ddir0;
            ddir0 = (dir0 - dir1);

            pointaccel1 = pointaccel0;
            pointaccel0 = ddir0.Length();

            stdir2 = stdir1;
            stdir1 = stdir0;
            DAC();

            a1stdir2 = a1stdir0;
            a1stdir1 = a1stdir0;
            a1stdir0 = (stdir1 + stdir0) / 2;

            pathpreservationsociety = MathF.Min(MathF.Min(vel0, vel1), vel2);
            pathpreservationsociety = 2 + (vtlimiter - 2) * FSmoothstep(pathpreservationsociety, 0, 20);
            pps2Dir = (dir0 + dir1) - (dir2 + dir2);
            pps2 = 2 + (vtlimiter - 2) * 0.5f * FSmoothstep(pps2Dir.Length(), 0, 15) + (vtlimiter - 2) * 0.5f * FSmoothstep(pps2Dir.Length(), 15, 45);
            pathpreservationsociety = Math.Min(pathpreservationsociety, pps2);
            pps3 = FSmoothstep(dir3.Length() - dir0.Length(), -20, 0) - FSmoothstep(dir3.Length() - dir0.Length(), 0, 20);
            
        }

        void DAC() {
            float scale = FSmootherstep(Vector2.Distance(stdir0, dir0), Math.Max(0, FSmoothstep(vel0, 0, 25) * dacInner), 0.01f + (FSmoothstep(vel0, 0, 25) * dacOuter));
            stdir0 = Vector2.Lerp(stdir0, dir0, scale);
            if (vel0 >= 1 && vel1 >= 1 && vel0 < 100) {
                stdir0 = Vector2.Lerp(stdir0, stdir1.Length() * Vector2.Normalize(stdir0), FSmootherstep(vel0, 5, 25) * (1 - scale) * (FSmootherstep(stdir0.Length() - stdir1.Length(), -3, 0) - FSmoothstep(stdir0.Length() - stdir1.Length(), 0, 3)));
            }
        }

        void LineDrive() {
            if (clusterjumping && accel0 < 0 & namelesstime1 > 6) {
                linedrivetime = Math.Min(linedrivetime + 1, namelesstime1);
                float scale1 = MathF.Pow(Math.Max(0.1f, Vector2.Dot(Vector2.Normalize(stdir0 - clusterdir1), Vector2.Normalize(Vector2.Zero - clusterdir1))), 1);
                Vector2 dist = ctozero.SegmentDistanceToPoint(stdir0, Line.SelfSmoothstep((linedrivetime - 1) / namelesstime1), Line.SelfSmoothstep((linedrivetime + 1) / namelesstime1));
                if (!vec2IsFinite(dist)) {
                    dist = Vector2.Zero;
                }
                float scale2 = dist.Length() / scale1;
                float scale3 = Math.Max(vel0 / 10, 1) * FSmoothstep(scale2, 20, 0);
                stdir0 -= dist * scale3;
                 Console.WriteLine(dist * scale3);
               
            }
            else linedrivetime = 1;
        }



        void ConditionalUpdate() {
            if (pointaccel0 > 4 && !(accel0 > 0 && accel1 < 0) && !(accel0 < 0 && accel1 > 0)) {
                if (!clusterjumping) {
                    clusterpos1 = clusterpos0;
                    clusterdir1 = clusterdir0;
                    ctozero = new Line(clusterdir1, Vector2.Zero, namelesstime1);
                    clusterjumping = true;
                }
                namelesstime0++;
                
            }
            else {
                clusterpos0 = pos0;
                namelesstime1 = namelesstime0;
                clusterdir0 = stdir0;
                clusterjumping = false;
                namelesstime0 = 1;
            }

            if (Math.Abs(accel0) > 4 && !(accel0 > 0 && accel1 < 0) && !(accel0 < 0 && accel1 > 0)) {
                if (!magclusterjumping) {
                    magcluster1 = magcluster0;
                    magclusterjumping = true;
                }
            }
            else {
                magcluster0 = stdir0.Length();
                magclusterjumping = false;
            }
            
            if (accel1 > 0 && accel0 < 0) {
                arc = (dir0 - dir2) / 2;
            }
            
        }

        void Plot() {
            Console.Write("vx");
            Console.WriteLine(dir0.X);
            Console.Write("vy");
            Console.WriteLine(dir0.Y * -1);
            Console.Write("ax");
            Console.WriteLine(testDir.X);
            Console.Write("ay");
            Console.WriteLine(testDir.Y * -1);
            Console.Write("jx");
            Console.WriteLine(clusterdir1.X);
            Console.Write("jy");
            Console.WriteLine(clusterdir1.Y * -1);
            Console.WriteLine("xx");
            Console.WriteLine("dd");
        }

        public static float FSmoothstep(float x, float start, float end)
        {
            x = (float)Math.Clamp((x - start) / (end - start), 0.0, 1.0);

            return x * x * (3 - 2 * x);
        }

        public static float FSmootherstep(float x, float start, float end)
        {
            x = (float)Math.Clamp((x - start) / (end - start), 0.0, 1.0);

            return (float)(x * x * x * (x * (6.0 * x - 15.0) + 10.0));
        }

        public static float ClampedLerp(float start, float end, float scale)
        {
            scale = (float)Math.Clamp(scale, 0, 1);

            return start + scale * (end - start);
        }

        public static Vector2 Trajectory(Vector2 p0, Vector2 p1, Vector2 p2, float t) {
            Vector2 tMid = 0.5f * (p0 + p2);
            Vector2 tAccel = 2 * (tMid - p1);
            Vector2 tVel = (2 * p1) - p2 - tMid;
            return p2 + t * tVel + 0.5f * t * t * tAccel;
        }

        public class Line {
            public Vector2 Start;
            public Vector2 End;
            public float Time;

            public Line(Vector2 s, Vector2 e, float t) {
                Start = s;
                End = e;
                Time = t;
            }

            public static Vector2 Rotate(Vector2 p, float a) {
                Vector2 sp = p;
                Vector2 rp;
                rp.X = (MathF.Cos(a) * sp.X) - (MathF.Sin(a) * sp.Y);
                rp.Y = (MathF.Sin(a) * sp.X) + (MathF.Cos(a) * sp.Y);
                return rp;
            }

            public void Step(float t) {
                Vector2 ldir = ((Start - End) / Time) * t;
                Start += ldir;
                End += ldir;
            }

            public Vector2 Curve(Vector2 p1, float t) {
                Vector2 tMid = 0.5f * (End + Start);
                Vector2 tAccel = 2 * (tMid - p1);
                Vector2 tVel = (2 * p1) - End - tMid;
                return End + t * tVel + 0.5f * t * t * tAccel;
            }

            public static float SelfSmoothstep(float x) {
                x = Math.Clamp(x, 0, 1);
                return x * x * (3.0f - 2.0f * x);
            }

            public static float SelfSmootherstep(float x) {
                x = Math.Clamp(x, 0, 1);
                return x * x * x * (x * (6.0f * x - 15.0f) + 10.0f);
            }

            public float ASSSS(float x) => SelfSmoothstep(x + 1 / Time);
                
            public float ASSRSS(float x) => SelfSmootherstep(x + 1 / Time);

            public Vector2 DTP(Vector2 mp, Vector2 me) {
                float a = MathF.Atan2(me.Y, me.X);
                float ca = -1 * MathF.Atan2(me.Y, me.X);
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                if (rp.X < 0)
                return Rotate(rp, a);
                else if (rp.X > re.X)
                return Rotate(rp - re, a);
                else {
                    rp.X = 0;
                    return Rotate(rp, a);
                }
            }
            
            public Vector2 FullDistanceToPoint(Vector2 p) {
                Vector2 mp = p - Start;
                Vector2 me = End - Start;
                return DTP(mp, me);
            }

            public Vector2 SegmentDistanceToPoint(Vector2 p, float t1, float t2) {
                Vector2 ss = Vector2.Lerp(Start, End, t1);
                Vector2 se = Vector2.Lerp(Start, End, t2);
                Vector2 mp = p - ss;
                Vector2 me = se - ss;
                return DTP(mp, me);
            }

            public Vector2 DirtyCurveDistanceToPoint(Vector2 p, Vector2 c, float t1, float t2) {
                Vector2 ss = Curve(c, t1);
                Vector2 se = Curve(c, t2);
                Vector2 mp = p - ss;
                Vector2 me = se - ss;
                return DTP(mp, me);
            }

            
        }

        

        Vector2 pos0, pos1, pos2, dir0, dir1, dir2, dir3, ddir0, ddir1, planestart, planeend, peak;
        float vel0, vel1, vel2, accel0, accel1, pointaccel0, pointaccel1;
        float peakMag, planeMag;
        Vector2 clusterpos0, clusterpos1;
        Vector2 clusterdir0, clusterdir1;
        float magcluster0, magcluster1;
        Line plane, ctozero, turnmirror;
        bool clusterjumping, magclusterjumping;
        Vector2 stdir0, stdir1, stdir2;
        float stmag0, stmag1;
        float reportTime;
        float reportMsAvg = (1 / 305);
        Vector2 testOutput, testDir;
        bool emergency;
        int namelesstime0, namelesstime1;
        float linedrivetime;
        bool linedriving;
        Vector2 arc;
        float savetime;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        float alpha0, alpha1, alpha0PreservationSociety;
        float top, bottom;
        Vector2 a1stdir0, a1stdir1, a1stdir2;
        Vector2 sdirt1;
        Vector2 pps2Dir;
        float pathpreservationsociety, pps2, pps3;
        bool consume;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}