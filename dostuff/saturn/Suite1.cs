using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Saturn
{
    [PluginName("Saturn - Suite 1")]
    public class Suite1 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Suite1() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        [Property("Velocity Trajectory Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "You should almost definitely have this enabled.\n" +
            "If not, use the other standalone filters. Your life may be much easier."
        )]
        public bool vtToggle { set; get; }

        [Property("Velocity Trajectory Limiter"), DefaultPropertyValue(3.0f), ToolTip
        (
            "2 = zero prediction, only interpolation, 3 = only prediction under sufficiently accelerating scenarios.\n" +
            "If on a Wacom Pro of 200hz or 300hz, put this to 3.\n" +
            "From testing my PTK-470, the input is so noiseless that prediction can be made with imperceptible error.\n" +
            "This could be because of its resolution, which is quadruple normal. (Wacom Pro 200hz tablets have the same resolution)\n" +
            "If not, try lower, and if you don't like the feel of it, try the standalone filters.\n" 
        )]
        public float vtlimiter { 
            set => _vtlimiter = Math.Clamp(value, 2.0f, 3.0f);
            get => _vtlimiter;
        }
        public float _vtlimiter;

        [Property("Directional Antichatter Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Antichatter, but applied to direction per report's point instead.\n" +
            "Improves frame pacing heavily in small area scenarios given proper settings.\n" +
            "Your tablet should hopefully have even internal report intervals (Wacom)"
        )]
        public bool dacToggle { set; get; }

        [Property("Directional Antichatter Inner"), DefaultPropertyValue(0f), ToolTip
        (
            "Similar method to Radial Follow. The unit of this is tablet raw data unit per report.\n" +
            "If on a large-small area on a Wacom Pro, try 0-1 respectively.\n" +
            "It's really your preference, though this should not go high.\n" +
            "Internal thresholds are used to prevent this messing things up horribly."
        )]
        public float dacInner { 
            set => _dacInner = Math.Clamp(value, 0, _dacOuter);
            get => _dacInner;
        }
        public float _dacInner;

        [Property("Directional Antichatter Outer"), DefaultPropertyValue(1f), ToolTip
        (
            "Similar method to Radial Follow. The unit of this is tablet raw data unit per report.\n" +
            "If on a large-small area on a Wacom Pro, try 1-3 respectively.\n" +
            "It's really your preference, though this should not go high.\n" +
            "Internal thresholds are used to prevent this from messing things up horribly.\n" 

        )]
        public float dacOuter { 
            set => _dacOuter = Math.Max(value, 0.1f);
            get => _dacOuter;
        }
        public float _dacOuter;

        [Property("Line Drive Toggle"), DefaultPropertyValue(false), ToolTip
        (
            "Experimental feature that may have little effect at all.\n" +
            "Attempts to reduce decel noise on a jump by taking the acceleration time of a jump\n" +
            "and creating a theoretical noiseless path of velocity in deceleration to zero,\n" +
            "which has a 'gravitational pull' on interpolated velocity.\n" +
            "I seriously cannot explain it better. Feel free to leave disabled."
        )]
        public bool ldToggle { set; get; }

        [Property("LD Outer"), DefaultPropertyValue(50f), ToolTip
        (
            "Range of 'gravitational pull.' Only applies if the above toggle is checked."
        )]
        public float ldOuter { 
            set => _ldOuter = Math.Max(value, 0.1f);
            get => _ldOuter;
        }
        public float _ldOuter;

        [Property("Adaptive EMA Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Devocub/Hawku Antichatter/Smoothing uses EMA at 1000hz. The 'Latency' label in milliseconds\n" +
            "is probably just a remnant of ancient times.\n" +
            "EMA means 'Exponential Moving Average' where the formula is outputpos = ((1 - weight) * outputpos) + (weight * inputpos).\n" +
            "The issue that Devocub/Hawku has is that at a high enough weight (low latency), it ends up\n" +
            "skipping a large distance to get mostly to raw in the first 2 or so refreshes after a report,\n" +
            "and staying between where it landed itself and raw position for the next (whatever amount) of updates before the next report.\n" +
            "This is because it simply runs EMA at a disjointed frequency from its information update without adjustment,\n" +
            "which is not how EMA works at all, and if it were adjusted to shift its behavior to even spacing when the weight approaches 1,\n" +
            "it would eventually converge to a filter that lerps between last position and current raw position using an expected \n" +
            "amount of time to the next report.\n\n" +
            "This does not have this issue, though, because we are applying EMA to output, and even if wired, time is adjusted for.\n" +
            "For context, Temporal Resampler has the opportunity to also run EMA at 1000hz to a decent degree of success, \n" +
            "especially because it is mostly velocity-congruent despite not even focusing on that because of how good it is, but it ends up favoring\n" +
            "simply running its input point for trajectory through it at report rate, which may not be preferable.\n" +
            "If you think my method of interpolation sucks and you want to try Temporal Resampler with this smoothing on top,\n" +
            "make sure that this filter is applied after Temporal Resampler by checking order in daemon,\n" +
            "then make sure to disable every other checkbox that is not this one, even wire.\n" +
            "Generally, unforeseen consequences will occur if this is put after another asynchronous filter without\n" +
            "knowing exactly what you are doing, and this is no exception.\n"

        )]
        public bool aemaToggle { set; get; }

        [Property("Stock Adaptive EMA Weight"), DefaultPropertyValue(1.0f), ToolTip
        (
            "EMA weight, but it can change based on the movement situation/amount of lag there is, based on internal thresholds.\n" +
            "This should hold for any reasonable area."
        )]
        public float stockWeight { 
            set => _stockWeight = Math.Clamp(value, 0.0f, 1.0f);
            get => _stockWeight;
        }
        public float _stockWeight;

        [Property("Dumb Weight (don't touch)"), DefaultPropertyValue(0.05f), ToolTip
        (
            "This value has been internally clamped between 0.01f and 0.1f\n" +
            "to prevent people who don't know any better from doing anything bad.\n" +
            "If you were to set this to 0, for example, you would get tablet drift.\n" +
            "Any higher than 0.1 and it would start bugging out."
        )]
        public float dumbWeight { 
            set => _dumbWeight = Math.Clamp(value, 0, 1);
            get => _dumbWeight;
        }
        public float _dumbWeight;

        [Property("Ring Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Oh YHeah"
        )]
        public bool ringToggle { set; get; }

        [Property("stockR"), DefaultPropertyValue(5f), ToolTip
        (
            "Ta"
        )]
        public float rInner { 
            set => _rInner = Math.Clamp(value, 1f, 100f);
            get => _rInner;
        }
        public float _rInner;

        [Property("wire"), DefaultPropertyValue(true), ToolTip
        (
            "You should definitely leave this enabled unless your specific situation requires otherwise.\n" +
            "Equivalent to 'extraFrames' from Temporal Resampler."
        )]
        public bool wire { set; get; }

        [Property("msOverride"), DefaultPropertyValue(0f), ToolTip
        (
            "Ta"
        )]
        public float msOverride { 
            set => _msOverride = Math.Clamp(value, 0f, 100f);
            get => _msOverride;
        }
        public float _msOverride;

        public event Action<IDeviceReport> Emit;

        protected override void ConsumeState()
        {
            if (State is ITabletReport report)
            {
                reportTime = (float)reportStopwatch.Restart().TotalMilliseconds;
                if (reportTime < 25) {
                    if (msOverride == 0)
                    reportMsAvg += ((reportTime - reportMsAvg) * 0.1f);
                    else reportMsAvg = msOverride;
                    if (emergency > 0)
                    emergency--;
                }
                else {
                    emergency = 10;
                }
                consume = true;
                moveOk = false;

                      
                StatUpdate(report);
                ConditionalUpdate();
                
                bottom = -1 * Math.Max(alpha0 - vtlimiter, 0);

                if (top > 1f || bottom > 1f) {
                    top = 0;
                    bottom = 0;
                }
                
                

                if (!vtToggle | wire) {
                    UpdateState();
                }
               // Console.WriteLine(jerk[0]);
            }
            else {
                OnEmit();
            } 
        }

        protected override void UpdateState()
        {
            if (State is ITabletReport report && PenIsInRange())
            {
                perfStopwatch.Restart();
                updateTime = (float)updateStopwatch.Restart().TotalMilliseconds;

                if (vtToggle) {
                    if (consume) {
                        alpha1 = 0;

                        if ((alpha0PreservationSociety > 1) && (top < 1)) {
                            top = 0.9f * (alpha0PreservationSociety - 1);
                            bottom = 0;
                        }
                        else top = 0;
                    }
                    
                    ohmygodbruh = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg) * (expect);

                    alpha0 = ((1 - top) * ohmygodbruh) + top;

                    alpha0PreservationSociety = alpha0 + (expect / reportMsAvg);
                
                    alpha0 += (vtlimiter - 1);

                    alpha0 = Math.Clamp(alpha0, (vtlimiter - 1), pathpreservationsociety);

                    trDir = Trajectory(stdir[0], stdir[1], stdir[2], alpha0);
                    sdirt1 = Trajectory(a1stdir[0], a1stdir[1], a1stdir[2], alpha0 + 0.5f);
                    trDir = Vector2.Lerp(trDir, sdirt1, pps4);
                    LineDrive();
                    ldDir = WireAdjust(ldDir / (reportMsAvg / (expect)), expect, updateTime, wire);
                }
                else {
                    if (consume) {
                        ldDir = stdir[0];
                    }
                    else { 
                        ldDir = Vector2.Zero;
                    }
                }

                ldOutput += ldDir;

                RF();

                


                    if (moveOk && emergency == 0) {
                    Vector2 pointaaaa = pos[0] + (trDir - (trDir - (stdir[1] / reportMsAvg))) * Math.Max(0, alpha0 - (vtlimiter - 1)) * (reportMsAvg / expect);
                   // Console.WriteLine(Vector2.Distance(ldOutput, pointaaaa));
                    ldOutput = Vector2.Lerp(ldOutput, pointaaaa, WireAdjust(dumbWeight, expect, updateTime, wire));
                    ldOutput = Vector2.Lerp(ldOutput, pos[0], dumbWeight * FSmoothstep(accel[0], -10f, -200f));
                    }
              
                AEMA();

                report.Position = aemaOutput;
                dirOfOutput = (report.Position - lastOutputPos) / updateTime;
                report.Pressure = pressure[0];

                if (!vec2IsFinite(report.Position + ringOutput + iRingPos0 + ldOutput)) {
                    report.Position = pos[0];
                    aemaOutput = pos[0];
                    ldOutput = pos[0];
                    ringOutput = pos[0];
                    iRingPos0 = pos[0];
                    emergency = 10;
                    OnEmit();
                    return;
                }

                if (emergency > 0) {
                    report.Position = pos[0];
                    ldOutput = pos[0];
                    aemaOutput = pos[0];
                    ringOutput = pos[0];
                    iRingPos0 = pos[0];
                    OnEmit();
                    return;
                }

                lastOutputPos = report.Position;

    

                

                consume = false;

               

            //    Console.WriteLine(report.Position - pos[0]);

                Plot();
                
                OnEmit();
            }
        }

        void StatUpdate(ITabletReport report) {
            InsertAtFirst(pos, report.Position);
            InsertAtFirst(pressure, report.Pressure);
            InsertAtFirst(dir, pos[0] - pos[1]);
            InsertAtFirst(vel, dir[0].Length());
            InsertAtFirst(ddir, dir[0] - dir[1]);
            InsertAtFirst(accel, vel[0] - vel[1]);
            InsertAtFirst(pointaccel, ddir[0].Length());
            DAC();

            if ((pressure[0] > 0 && pressure[1] == 0) || (pressure[0] == 0 && pressure[1] > 0))
            liftorpress = true;
            else liftorpress = false;

            if (dir[0] == pos[0]) {
                emergency = 5;
            }

            pathpreservationsociety = Math.Min(Math.Min(stdir[0].Length(), stdir[1].Length()), stdir[2].Length());
            pathpreservationsociety = FSmoothstep(pathpreservationsociety, 0, 20);
            pps2Dir = (stdir[0] + stdir[1]) - (stdir[2] + stdir[2]);
            pps2 = FSmoothstep(pps2Dir.Length(), 0, 15);
            pps3 = FSmoothstep(Vector2.Distance(stdir[0], stdir[1]), dacInner, dacOuter);
            pathpreservationsociety = 2 + (vtlimiter - 2) * Math.Min(Math.Min(pathpreservationsociety, pps2), pps3);
            pps4 = FSmoothstep(stdir[3].Length() - stdir[0].Length(), -15, 0) - FSmoothstep(stdir[3].Length() - stdir[0].Length(), 0, 15);

            if (pressure[0] == 0)
                pathpreservationsociety = Math.Min(pathpreservationsociety, 3 - FSmoothstep(Vector2.Distance(ddir[0], ddir[1]), 30, 69));
            
        }

        void DAC() {
            if (dacToggle) {
                
                float vscale = FSmoothstep(vel[0], 3, 25);
                float scale = FSmootherstep(Math.Max(pointaccel[0], Vector2.Distance(stdir[0], dir[0])), Math.Max(0, vscale * dacInner), 0.01f + (vscale * dacOuter));
                Vector2 stabilized = Vector2.Lerp(stdir[0], dir[0], scale);
                if (vel[0] >= 1 && vel[1] >= 1 && vel[0] < 100 && stabilized.Length() > 1) {
                    float ascale = Math.Max(Math.Abs(accel[0]), Math.Abs(stabilized.Length() - stdir[0].Length()));
                    stabilized = Vector2.Lerp(stabilized, stdir[0].Length() * Vector2.Normalize(stabilized), vscale * (1 - scale) * (FSmoothstep(ascale, 0, dacOuter)));
                }
            InsertAtFirst(stdir, stabilized);
            InsertAtFirst(a1stdir, (stdir[1] + stdir[0]) / 2);
            }
            else {
                InsertAtFirst(stdir, dir[0]);
                InsertAtFirst(a1stdir, (stdir[1] + stdir[0]) / 2);
            }
        }

        void LineDrive() {
            if (ldToggle) {
            if (clusterjumping && accel[0] < 0 && namelesstime1 > 6 && peakAccel1 > 25) {
                linedrivetime = Math.Min(linedrivetime + 1, namelesstime1);
                float scale1 = MathF.Pow(Math.Max(0.01f, DotNorm(trDir - clusterdir1, Vector2.Zero - clusterdir1)), 9);
                float time1 = Line.SelfSmoothstep((linedrivetime + (vtlimiter - 3)) / namelesstime1);
                float time2 = Line.SelfSmoothstep((linedrivetime + (vtlimiter - 2)) / namelesstime1);
                Vector2 dist = ctozero.SegmentDistanceToPoint(trDir, time1, time2);
                if (!vec2IsFinite(dist)) {
                    dist = Vector2.Zero;
                }
                float scale2 = (dist.Length() / scale1) / (1 / (1 + MathF.Log(ctozero.SegmentPerpendicularDistanceL(trDir, time1, time2) + 1)));
                float scale3 = Math.Min(vel[0] / 10, 1) * FSmoothstep(scale2, ldOuter, 0);
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

        void RF() {
            if (ringToggle) {
                float mult = 20 + Math.Max(100 * DotNorm(ddir[0], dir[0]) * FSmootherstep((pointaccel[0] + Math.Max(jerk[0], 0)) / (MathF.Log(MathF.E * vel[0] + 1) + 1), 5, 10), -20);
                ringInputPos1 = ringInputPos0;
                ringInputPos0 = ldOutput;
                ringInputDir = ringInputPos0 - ringInputPos1;
                Vector2 dist = ldOutput - iRingPos0;
                iRingPos1 = iRingPos0;
                iRingPos0 += Math.Max(0, dist.Length() - (rInner)) * Default(Vector2.Normalize(dist), Vector2.Zero);
                ringDir = iRingPos0 - iRingPos1;
                ringOutput += ringDir;

                if (ringDir.Length() > 0 || dist.Length() > rInner || accel[0] < -10 || vel[0] > 10 * rInner) {
                ringOutput = capDist(ringOutput, Vector2.Lerp(ringOutput, ldOutput, 1f), 5f);
                ringOutput = Vector2.Lerp(ringOutput, ldOutput, FSmoothstep(accel[0], -10, -200));
                moveOk = true;
                }
            }
            else {
                moveOk = true;
                ringOutput = ldOutput;
            }

        }

        void AEMA() {
            float weight = 1;
            if (aemaToggle) {
                stockWeight = weight;
                float mod1 = (1f - stockWeight) * (FSmoothstep(vel[0], 25, 75) - FSmoothstep(vel[0], 175, 250)) * FSmoothstep(MathF.Abs(accel[0]), 50, 10);
                float dist = Vector2.Distance(aemaOutput, ringOutput);
                float mod2 = mod1 * FSmoothstep(dist, 0, 75);
                float mod3 = (1f - stockWeight) * FSmoothstep(dist, 0, 100) * FSmoothstep(accel[0] - jerk[0], -10, -30);
                float mod4 = stockWeight * FSmoothstep(dist, 200, 0) * FSmoothstep(accel[0] + jerk[0], 10, 30);
                weight += Math.Max(mod2, mod3) - mod4;
                weight = WireAdjust(weight, expect, updateTime, wire);
            }
            aemaOutput = Vector2.Lerp(aemaOutput, ringOutput, weight);
        }

        float WireAdjust(float a, float be, float br, bool w) => w ? a * (br / be) : a;

        Vector2 WireAdjust(Vector2 a, float be, float br, bool w) => w ? a * (br / be) : a;

        void ConditionalUpdate() {
            if (!(accel[0] > 0 && accel[1] < 0) && !(accel[0] < 0 && accel[1] > 0)) {
                if (!clusterjumping) {
                    clusterpos1 = clusterpos0;
                    clusterdir1 = clusterdir0;
                    peakAccel0 = accel[0];
                    ctozero = new Line(clusterdir1, Vector2.Zero, namelesstime1);
                    clusterjumping = true;
                }
                namelesstime0++;      
                if (peakAccel0 < accel[0]) {
                    peakAccel0 = accel[0];
                }
            }
            else {
                clusterpos0 = pos[0];
                namelesstime1 = namelesstime0;
                clusterdir0 = stdir[0];
                peakAccel1 = peakAccel0;
                clusterjumping = false;
                namelesstime0 = 1;
            }

            if (Math.Abs(accel[0]) > 4 && !(accel[0] > 0 && accel[1] < 0) && !(accel[0] < 0 && accel[1] > 0)) {
                if (!magclusterjumping) {
                    magcluster1 = magcluster0;
                    magclusterjumping = true;
                }
            }
            else {
                magcluster0 = stdir[0].Length();
                magclusterjumping = false;
            }
            
            if (accel[1] > 0 && accel[0] < 0) {
                arc = (dir[0] - dir[2]) / 2;
            }
            
        }

        void Plot() {
        
            Console.Write("vx");
            Console.WriteLine((dirOfOutput.X));
            Console.Write("vy");
            Console.WriteLine((dirOfOutput.Y) * -1);
            /*Console.Write("jx");
            Console.WriteLine(arc.X);
            Console.Write("jy");
            Console.WriteLine(arc.Y * -1);
            Console.Write("sx");
            Console.WriteLine(sense.X);
            Console.Write("sy");
            Console.WriteLine(sense.Y * -1); */
            Console.WriteLine("xx");
            Console.WriteLine("dd");
            
        }

        float DotNorm(Vector2 a, Vector2 b) {
            if (a != Vector2.Zero && b != Vector2.Zero)
                return Vector2.Dot(Vector2.Normalize(a), Vector2.Normalize(b));
            else return 1;
        }

        float Default(float a, float b) => float.IsFinite(a) ? a : b;

        Vector2 Default(Vector2 a, Vector2 b) => vec2IsFinite(a) ? a : b;

        Vector2 MinLength(Vector2 a, Vector2 b) => a.Length() <= b.Length() ? a : b;

        Vector2 capDist(Vector2 a, Vector2 b, float d) => a + Math.Min(Vector2.Distance(b, a), d) * (vec2IsFinite(Vector2.Normalize(b - a)) ? Vector2.Normalize(b - a) : Vector2.Zero); 

        public static float FSmoothstep(float x, float start, float end)
        {
            x = Math.Clamp((x - start) / (end - start), 0.0f, 1.0f);
            return x * x * (3.0f - 2.0f * x);
        }

        public static float FSmootherstep(float x, float start, float end)
        {
            x = Math.Clamp((x - start) / (end - start), 0.0f, 1.0f);
            return (x * x * x * (x * (6.0f * x - 15.0f) + 10.0f));
        }

        public static float ClampedLerp(float start, float end, float scale)
        {
            return start + Math.Clamp(scale, 0, 1) * (end - start);
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
                return Start + t * ((2 * p1) - Start - tMid) + 0.5f * t * t * (2 * (tMid - p1));
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

        void InsertAtFirst<T>(T[] arr, T element)
        {
            for (int p = arr.Length - 1; p > 0; p--) arr[p] = arr[p - 1];
            arr[0] = element;
        }

        const int HMAX = 16;

        Vector2 planestart, planeend, peak;

        Vector2[] pos = new Vector2[HMAX];
        Vector2[] dir = new Vector2[HMAX];
        Vector2[] stdir = new Vector2[HMAX];
        Vector2[] ddir = new Vector2[HMAX];
        Vector2[] a1stdir = new Vector2[HMAX];
        float[] vel = new float[HMAX];
        float[] accel = new float[HMAX];
        float[] jerk = new float[HMAX];
        float[] pointaccel = new float[HMAX];
        uint[] pressure = new uint[HMAX];
        
        float peakMag, planeMag;
        float ohmygodbruh;
        Vector2 clusterpos0, clusterpos1;
        Vector2 clusterdir0, clusterdir1;
        float magcluster0, magcluster1;
        Line plane, ctozero, turnmirror;
        bool clusterjumping, magclusterjumping;
        float stmag0, stmag1;
        float reportTime;
        float reportMsAvg;
        Vector2 trueOutput, trueDir;
        Vector2 ldDir, ldOutput;
        Vector2 aemaOutput;
        Vector2 iRingPos0, iRingPos1, ringDir, ringOutput, ringInputPos1, ringInputPos0, ringInputDir;
        int emergency;
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
        Vector2 sdirt1;
        Vector2 pps2Dir;
        float pathpreservationsociety, pps2, pps4;
        bool consume;
        Vector2 sense;
        float peakAccel0, peakAccel1;
        Vector2 trDir;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
        private HPETDeltaStopwatch perfStopwatch = new HPETDeltaStopwatch();
        double updatePerfTimeAvg;
        double updates = 0;
        bool liftorpress;
        bool moveOk;
        Vector2 dirOfOutput, lastOutputPos;
        float pps3;

        float expect => 1000 / Frequency;
    }
}