using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Plugin260122
{
    [PluginName("Plugin260122")]
    public class Plugin260122 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Plugin260122() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        [Property("VT Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public bool vtToggle { 
            set => _vtToggle = value;
            get => _vtToggle;
        }
        public bool _vtToggle;

        [Property("VT Limiter"), DefaultPropertyValue(3.0f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float vtlimiter { 
            set => _vtlimiter = (float)Math.Clamp(value, 2.0f, 3.0f);
            get => _vtlimiter;
        }
        public float _vtlimiter;

        [Property("DAC Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public bool dacToggle { 
            set => _dacToggle = value;
            get => _dacToggle;
        }
        public bool _dacToggle;

        [Property("DAC Inner"), DefaultPropertyValue(0f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float dacInner { 
            set => _dacInner = Math.Clamp(value, 0, _dacOuter);
            get => _dacInner;
        }
        public float _dacInner;

        [Property("DAC Outer"), DefaultPropertyValue(1f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float dacOuter { 
            set => _dacOuter = MathF.Max(value, 0.1f);
            get => _dacOuter;
        }
        public float _dacOuter;

        [Property("LD Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public bool ldToggle { 
            set => _ldToggle = value;
            get => _ldToggle;
        }
        public bool _ldToggle;

        [Property("LD Outer"), DefaultPropertyValue(50f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float ldOuter { 
            set => _ldOuter = MathF.Max(value, 0.1f);
            get => _ldOuter;
        }
        public float _ldOuter;

        [Property("Stock Weight"), DefaultPropertyValue(0.99f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float stockWeight { 
            set => _stockWeight = (float)Math.Clamp(value, 0.0f, 1.0f);
            get => _stockWeight;
        }
        public float _stockWeight;

        [Property("Dumb Weight (don't touch)"), DefaultPropertyValue(0.05f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float dumbWeight { 
            set => _dumbWeight = (float)Math.Clamp(value, 0.0f, 0.9f);
            get => _dumbWeight;
        }
        public float _dumbWeight;

        [Property("wire"), DefaultPropertyValue(true), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public bool wire { set; get; }

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
                
                bottom = -1 * Math.Max(alpha0 - vtlimiter, 0);

                if (top > 0.75f || bottom > 0.75f) {
                    top = 0;
                    bottom = 0;
                }

                if (!vtToggle | wire) {
                    UpdateState();
                }

            }
            else {
                OnEmit();
            } 
        }

        const int TEST_SIZE = 64;
        Random aaar = new Random();

        protected override void UpdateState()   // Interpolation
        {
            if (State is ITabletReport report && PenIsInRange())
            {
               // perfStopwatch.Restart();

                updateTime = (float)updateStopwatch.Restart().TotalMilliseconds;

                if (vtToggle) {
                if (consume) {
                alpha1 = 0;

                if ((alpha0PreservationSociety > 1) && (top < 1)) {
                    top = alpha0PreservationSociety - 1;
                    bottom = 0;
                }
                else top = 0;
                }
                
                float ohmygodbruh = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg) * (1000 / Frequency);

                alpha0 = ((1 - top) * ohmygodbruh) + 1.120f * top;

                alpha0PreservationSociety = alpha0;
            
                alpha0 += (vtlimiter - 1);

                alpha0 = Math.Clamp(alpha0, (vtlimiter - 1), pathpreservationsociety);

                trDir = Trajectory(stdir0, stdir1, stdir2, alpha0) / (reportMsAvg / (1000 / Frequency));
                sdirt1 = Trajectory(a1stdir0, a1stdir1, a1stdir2, alpha0 + 0.5f) / (reportMsAvg / (1000 / Frequency));
                trDir = Vector2.Lerp(trDir, sdirt1, pps3);
                if (wire) {
                    trDir *= updateTime / (1000 / Frequency);
                }
            //    Console.WriteLine(trDir.Length());
                LineDrive();
                }
                else {
                    if (consume) {
                        ldDir = stdir0;
                    }
                    else { 
                        ldDir = Vector2.Zero;
                    }
                }

                ldOutput += ldDir;

                if (!emergency && !liftorpress && vec2IsFinite(ldOutput + aemaOutput)) {
                    ldOutput = Vector2.Lerp(ldOutput, pos0 + trDir + (trDir - (stdir1 / reportMsAvg) * updateTime / (1000 / Frequency)), dumbWeight);
                 //  ldOutput = Vector2.Lerp(ldOutput, pos0, dumbWeight * FSmoothstep(accel0, 0, -200f));
                }
                else {
                    ldOutput = pos0;
                    aemaOutput = pos0;
                } 

                AEMA();

                report.Position = aemaOutput;

              //  Console.WriteLine(report.Position - pos0);
              //Console.WriteLine(pointaccel0);
                consume = false;
            
                OnEmit();
            }
        }

        void StatUpdate(ITabletReport report) {
            pos2 = pos1;
            pos1 = pos0;
            pos0 = report.Position;

            pressure1 = pressure0;
            pressure0 = report.Pressure;

            if ((pressure0 > 0 && pressure1 == 0) || (pressure0 == 0 && pressure1 > 0))
            liftorpress = true;
            else liftorpress = false;

            dir3 = dir2;
            dir2 = dir1;
            dir1 = dir0;
            dir0 = (pos0 - pos1);

            

            vel2 = vel1;
            vel1 = vel0;
            vel0 = dir0.Length();

            accel1 = accel0;
            accel0 = (vel0 - vel1);

            jerk0 = accel0 - accel1;

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

            pathpreservationsociety = MathF.Min(MathF.Min(stdir0.Length(), stdir1.Length()), stdir2.Length());
            pathpreservationsociety = 2 + (vtlimiter - 2) * FSmoothstep(pathpreservationsociety, 0, 20);
            pps2Dir = (stdir0 + stdir1) - (stdir2 + stdir2);
            pps2 = 2 + (vtlimiter - 2) * FSmoothstep(pps2Dir.Length(), 0, 15);
            pathpreservationsociety = Math.Min(pathpreservationsociety, pps2);
            pps3 = FSmoothstep(stdir3.Length() - stdir0.Length(), -15, 0) - FSmoothstep(stdir3.Length() - stdir0.Length(), 0, 15);

            if (pointaccel0 > 400)
                pathpreservationsociety = 2;
            
        }

        void DAC() {
            if (dacToggle) {
                float vscale = FSmoothstep(vel0, 3, 25);
                float scale = FSmootherstep(MathF.Max(pointaccel0, Vector2.Distance(stdir0, dir0)), Math.Max(0, vscale * dacInner), 0.01f + (vscale * dacOuter));
                stdir0 = Vector2.Lerp(stdir0, dir0, scale);
                if (vel0 >= 1 && vel1 >= 1 && vel0 < 100) {
                    float ascale = MathF.Max(Math.Abs(accel0), Math.Abs(stdir0.Length() - stdir1.Length()));
                    stdir0 = Vector2.Lerp(stdir0, stdir1.Length() * Vector2.Normalize(stdir0), vscale * (1 - scale) * (FSmoothstep(ascale, 0, dacOuter)));
                }
            }
            else {
                stdir0 = dir0;
            }
        }

        void LineDrive() {
            if (ldToggle) {
            if (clusterjumping && accel0 < 0 && namelesstime1 > 6 && peakAccel1 > 25) {
                linedrivetime = Math.Min(linedrivetime + 1, namelesstime1);
                float scale1 = MathF.Pow(Math.Max(0.01f, Vector2.Dot(Vector2.Normalize(trDir - clusterdir1), Vector2.Normalize(Vector2.Zero - clusterdir1))), 10);
                float time1 = Line.SelfSmoothstep((linedrivetime + (vtlimiter - 3)) / namelesstime1);
                float time2 = Line.SelfSmoothstep((linedrivetime + (vtlimiter - 2)) / namelesstime1);
                Vector2 dist = ctozero.SegmentDistanceToPoint(trDir, time1, time2);
                if (!vec2IsFinite(dist)) {
                    dist = Vector2.Zero;
                }
                float scale2 = (dist.Length() / scale1) / (1 / (1 + MathF.Log(ctozero.SegmentPerpendicularDistanceL(trDir, time1, time2) + 1)));
                float scale3 = Math.Min(vel0 / 10, 1) * FSmoothstep(scale2, ldOuter, 0);
                ldDir = trDir - dist * scale3;
                sense = dist;
                
            }
            else {
                linedrivetime = 1;
                sense = Vector2.Zero;
                ldDir = trDir;
            }
            }
            else ldDir = trDir;
        }

        void AEMA() {
            float weight = stockWeight;
            float mod1 = (1f - stockWeight) * (FSmoothstep(vel0, 25, 75) - FSmoothstep(vel0, 175, 250)) * FSmoothstep(MathF.Abs(accel0), 50, 10);
            float dist = Vector2.Distance(aemaOutput, ldOutput);
            float mod2 = mod1 * FSmoothstep(dist, 0, 75);
            float mod3 = (1f - stockWeight) * FSmoothstep(dist, 0, 100) * FSmoothstep(accel0 - jerk0, -10, -30);
            float mod4 = stockWeight * FSmoothstep(dist, 200, 0) * FSmoothstep(accel0 + jerk0, 10, 30);
            if (weight != 1)
            weight += MathF.Max(mod2, mod3) - mod4;

            aemaOutput = Vector2.Lerp(aemaOutput, ldOutput, weight);
         //   Console.WriteLine(weight);
        }

        void ConditionalUpdate() {
            if (!(accel0 > 0 && accel1 < 0) && !(accel0 < 0 && accel1 > 0)) {
                if (!clusterjumping) {
                    clusterpos1 = clusterpos0;
                    clusterdir1 = clusterdir0;
                    peakAccel0 = accel0;
                    ctozero = new Line(clusterdir1, Vector2.Zero, namelesstime1);
                    clusterjumping = true;
                }
                namelesstime0++;      
                if (peakAccel0 < accel0) {
                    peakAccel0 = accel0;
                }
            }
            else {
                clusterpos0 = pos0;
                namelesstime1 = namelesstime0;
                clusterdir0 = stdir0;
                peakAccel1 = peakAccel0;
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
            Console.WriteLine(dir0.X / reportMsAvg);
            Console.Write("vy");
            Console.WriteLine((dir0.Y * -1) / reportMsAvg);
            Console.Write("jx");
            Console.WriteLine(arc.X);
            Console.Write("jy");
            Console.WriteLine(arc.Y * -1);
            Console.Write("sx");
            Console.WriteLine(sense.X);
            Console.Write("sy");
            Console.WriteLine(sense.Y * -1);
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
            return start + (float)Math.Clamp(scale, 0, 1) * (end - start);
        }

        public static Vector2 Trajectory(Vector2 p0, Vector2 p1, Vector2 p2, float t) {
            Vector2 tMid = 0.5f * (p0 + p2);
            return p2 + t * ((2 * p1) - p2 - tMid) + 0.5f * t * t * (2 * (tMid - p1));
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
                float cosine = MathF.Cos(a);
                float sine = MathF.Sin(a);
                return new Vector2((cosine * p.X) - (sine * p.Y), (sine * p.X) + (cosine * p.Y));
            }

            public void Step(float t) {
                Vector2 ldir = ((Start - End) / Time) * t;
                Start += ldir;
                End += ldir;
            }

            public Vector2 Curve(Vector2 p1, float t) {
                Vector2 tMid = 0.5f * (End + Start);
                return End + t * ((2 * p1) - End - tMid) + 0.5f * t * t * (2 * (tMid - p1));
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

            public static Vector2 DTP(Vector2 mp, Vector2 me) {
                float a = MathF.Atan2(me.Y, me.X);
                float ca = -a;
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                if (rp.X < 0f) return mp;
                else if (rp.X > re.X) return Rotate(rp - re, a);
                else return Rotate(new Vector2(0f, rp.Y), a);
            }

            public static float DTPL(Vector2 mp, Vector2 me) {
                float ca = -MathF.Atan2(me.Y, me.X);
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                if (rp.X < 0f) return rp.Length();
                else if (rp.X > re.X) return Vector2.Distance(rp, re);
                else return rp.Y;
            }

            public static Vector2 PD(Vector2 mp, Vector2 me) {
                float a = MathF.Atan2(me.Y, me.X);
                float ca = -a;
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                if (rp.X < 0f) return Rotate(new Vector2(rp.X, 0f), a);
                else if (rp.X > re.X) return Rotate(new Vector2(rp.X - re.X, 0f), a);
                else return Vector2.Zero;
            }

            public static float PDL(Vector2 mp, Vector2 me) {
                float ca = -MathF.Atan2(me.Y, me.X);
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                if (rp.X < 0f) return -rp.X;
                else if (rp.X > re.X) return rp.X - re.X;
                else return 0f;
            }

            public Vector2 FullDistanceToPoint(Vector2 p) {
                return DTP(p - Start, End - Start);
            }

            public Vector2 SegmentDistanceToPoint(Vector2 p, float t1, float t2) {
                Vector2 ss = Vector2.Lerp(Start, End, t1);
                Vector2 se = Vector2.Lerp(Start, End, t2);
                return DTP(p - ss, se - ss);
            } 

            public Vector2 DirtyCurveDistanceToPoint(Vector2 p, Vector2 c, float t1, float t2) {
                Vector2 ss = Curve(c, t1 * 2);
                Vector2 se = Curve(c, t2 * 2);
                return DTP(p - ss, se - ss);
            } 

            public float FullDistanceToPointL(Vector2 p) {
                return DTPL(p - Start, End - Start);
            }

            public float SegmentDistanceToPointL(Vector2 p, float t1, float t2) {
                Vector2 ss = Vector2.Lerp(Start, End, t1);
                Vector2 se = Vector2.Lerp(Start, End, t2);
                return DTPL(p - ss, se - ss);
            } 

            public float DirtyCurveDistanceToPointL(Vector2 p, Vector2 c, float t1, float t2) {
                Vector2 ss = Curve(c, t1 * 2);
                Vector2 se = Curve(c, t2 * 2);
                return DTPL(p - ss, se - ss);
            } 

            public Vector2 FullPerpendicularDistance(Vector2 p) {
                return PD(p - Start, End - Start);
            }

            public Vector2 SegmentPerpendicularDistance(Vector2 p, float t1, float t2) {
                Vector2 ss = Vector2.Lerp(Start, End, t1);
                Vector2 se = Vector2.Lerp(Start, End, t2);
                return PD(p - ss, se - ss);
            } 

            public Vector2 DirtyCurvePerpendicularDistance(Vector2 p, Vector2 c, float t1, float t2) {
                Vector2 ss = Curve(c, t1 * 2);
                Vector2 se = Curve(c, t2 * 2);
                return PD(p - ss, se - ss);
            } 

            public float FullPerpendicularDistanceL(Vector2 p) {
                return PDL(p - Start, End - Start);
            }

            public float SegmentPerpendicularDistanceL(Vector2 p, float t1, float t2) {
                Vector2 ss = Vector2.Lerp(Start, End, t1);
                Vector2 se = Vector2.Lerp(Start, End, t2);
                return PDL(p - ss, se - ss);
            } 

            public float DirtyCurvePerpendicularDistanceL(Vector2 p, Vector2 c, float t1, float t2) {
                Vector2 ss = Curve(c, t1 * 2);
                Vector2 se = Curve(c, t2 * 2);
                return PDL(p - ss, se - ss);
            } 

            public static Vector2 PathDiff(Vector2 s, Vector2 e, Vector2 p) {
                Vector2 mp = p - s;
                Vector2 me = e - s;
                float ca = -MathF.Atan2(me.Y, me.X);
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                return rp - re;
            }
        }

        

        Vector2 pos0, pos1, pos2, dir0, dir1, dir2, dir3, ddir0, ddir1, planestart, planeend, peak;
        float vel0, vel1, vel2, accel0, accel1, jerk0, pointaccel0, pointaccel1;
        float peakMag, planeMag;
        Vector2 clusterpos0, clusterpos1;
        Vector2 clusterdir0, clusterdir1;
        float magcluster0, magcluster1;
        Line plane, ctozero, turnmirror;
        bool clusterjumping, magclusterjumping;
        Vector2 stdir0, stdir1, stdir2, stdir3;
        float stmag0, stmag1;
        float reportTime;
        float reportMsAvg = (1 / 303);
        Vector2 trueOutput, trueDir;
        Vector2 ldDir, ldOutput;
        Vector2 aemaOutput;
        bool emergency;
        int namelesstime0, namelesstime1;
        float linedrivetime;
        bool linedriving;
        Vector2 arc;
        float savetime;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private HPETDeltaStopwatch updateStopwatch = new HPETDeltaStopwatch();
        float updateTime;
        float alpha0, alpha1, alpha0PreservationSociety;
        float top, bottom;
        Vector2 a1stdir0, a1stdir1, a1stdir2;
        Vector2 sdirt1;
        Vector2 pps2Dir;
        float pathpreservationsociety, pps2, pps3;
        bool consume;
        Vector2 sense;
        float peakAccel0, peakAccel1;
        Vector2 trDir;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
        private HPETDeltaStopwatch perfStopwatch = new HPETDeltaStopwatch();
        double updatePerfTimeAvg;
        double updates = 0;
        uint pressure0, pressure1;
        bool liftorpress;
    }
}