using System;
using System.Numerics;
using System.Runtime;
using System.Runtime.CompilerServices;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Saturn
{
    [PluginName("Saturn - Multifilter (Velocity Interpolation)")]
    public class MultifilterVI : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public MultifilterVI() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        [Property("Velocity Trajectory Limiter"), DefaultPropertyValue(3f), ToolTip
        (
            "2 = zero prediction, only interpolation, 3 = only prediction under sufficient situations.\n" +
            "If on a Intuos Pro (200hz or 300hz), put this to 3.\n" +
            "Use the other multifilter otherwise."
        )]
        public float vtlimiter { 
            set => _vtlimiter = Math.Clamp(value, 2.0f, 3.0f);
            get => _vtlimiter;
        }
        public float _vtlimiter;

        [Property("Reverse EMA"), DefaultPropertyValue(1f), ToolTip
        (
            "Default: 1.0\n" +
            "Range: 0.0 - 1.0\n\n" +

            "Removes hardware smoothing, fine-tuned this to your tablet.\n" +
            "1.0 == no effect\n" +
            "lower == removes more hardware smoothing\n" +
            "Probably should use the other one if you think you need this."
        )]
        public float reverseSmoothing
        {
            set => _reverseSmoothing = Math.Clamp(value, 0.001f, 1);
            get => _reverseSmoothing;
        }
        public float _reverseSmoothing;

        [Property("Directional Antichatter Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Antichatter, but applied to direction per report's point instead.\n" +
            "Improves frame pacing heavily in small area scenarios given proper settings.\n" +
            "Your tablet should hopefully have even internal report intervals (Wacom)"
        )]
        public bool dacToggle { set; get; }

        [Property("Directional Antichatter Inner 'Radius'"), DefaultPropertyValue(0f), ToolTip
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

        [Property("Directional Antichatter Outer 'Radius'"), DefaultPropertyValue(3f), ToolTip
        (
            "Similar method to Radial Follow. The unit of this is tablet raw data unit per report.\n" +
            "If on a large-small area on a Wacom Pro, try 1-3 respectively.\n" +
            "It's really your preference, though this should not go high.\n" +
            "Internal thresholds are used to prevent this from messing things up horribly."
        )]
        public float dacOuter { 
            set => _dacOuter = Math.Max(value, 0.1f);
            get => _dacOuter;
        }
        public float _dacOuter;

        [Property("Velocity Outer 'Range'"), DefaultPropertyValue(2f), ToolTip
        (
            "Will act the same, but for magnitude of direction.\n" +
            "No functionality changes, this was just internally set to the above option in early builds." 
        )]
        public float vOuter { 
            set => _vOuter = Math.Max(value, 0.1f);
            get => _vOuter;
        }
        public float _vOuter;

        [Property("Line Drive Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Experimental feature that may have little effect at all.\n" +
            "Attempts to reduce decel noise on a jump by taking the acceleration time of a jump\n" +
            "and creating a theoretical noiseless path of velocity in deceleration to zero,\n" +
            "which has a 'gravitational pull' on interpolated velocity.\n" +
            "I seriously cannot explain it better. Feel free to leave disabled."
        )]
        public bool ldToggle { set; get; }

        [Property("LD Outer"), DefaultPropertyValue(25f), ToolTip
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
            "The issue that Devocub and Hawku have is that at a high enough weight (low latency), it ends up\n" +
            "skipping a large distance to get mostly to raw in the first 2 or so refreshes after a report,\n" +
            "and staying between where it landed itself and raw position for the next (whatever amount) of updates before the next report.\n" +
            "This is because it simply runs EMA at a disjointed frequency from its information update without adjustment,\n" +
            "which is not how EMA should work at all, and if it were adjusted to shift its behavior to even spacing when the weight approaches 1,\n" +
            "it would eventually converge to a filter that lerps between last position and current raw position using an expected \n" +
            "amount of time to the next report.\n\n" +
            "This does not have this issue, though, because we are applying EMA to output, and even if wired, time is adjusted for.\n" +
            "For context, Temporal Resampler has the opportunity to also run EMA at 1000hz, but it ends up favoring\n" +
            "simply running its input point for trajectory through it at report rate, which may not be preferable.\n" +
            "Do not use this after another asynchronous filter; the other smoothing is more well-suited."

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

        [Property("Accel Response Aggressiveness"), DefaultPropertyValue(1f), ToolTip
        (
            "Useful values range between 0 and 2.\n" +
            "Do not put above 0 if you hover, as reporting becomes buggy.\n" +
            "Makes aim 'snappier' on sharp accel."
        )]
        public float aResponse { 
            set => _aResponse = Math.Clamp(value, 0, 1000000.0f);
            get => _aResponse;
        }
        public float _aResponse;

        [Property("Ring Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Ring Antichatter is relatively simple, making it non-invasive and low-latency.\n" +
            "It works similarly to Radial Follow, but it ensures\n" +
            "to not underaim if the radius is anything over the raw tablet noise."
        )]
        public bool ringToggle { set; get; }

        [Property("Ring Radius"), DefaultPropertyValue(5f), ToolTip
        (
            "The cursor will not move if it has not moved this much. Unit is raw data."
        )]
        public float rInner { 
            set => _rInner = Math.Clamp(value, 0f, 1000000.0f);
            get => _rInner;
        }
        public float _rInner;

        [Property("Outer Radial Mult"), DefaultPropertyValue(1f), ToolTip
        (
            "Useful values range from 0 to ~2.\n" +
            "Unsure - keep at 1."
        )]
        public float oMult { 
            set => _oMult = Math.Clamp(value, 0f, 1000000.0f);
            get => _oMult;
        }
        public float _oMult;

        [Property("wire"), DefaultPropertyValue(true), ToolTip
        (
            "You should definitely leave this enabled unless your specific situation requires otherwise.\n" +
            "Some people have reported this breaking things. Their timers don't work for reasons beyond me.\n" +
            "Equivalent to 'extraFrames' from Temporal Resampler."
        )]
        public bool wire { set; get; }

        [Property("msOverride"), DefaultPropertyValue(3.302466f), ToolTip
        (
            "You should know what you are doing if you change this from 0.\n" +
            "Wacom PTK-x70 - make this 3.302466 if using given pen, otherwise you are on your own."
        )]
        public float msOverride { 
            set => _msOverride = Math.Clamp(value, 0f, 100f);
            get => _msOverride;
        }
        public float _msOverride;

        [Property("Area Scale"), DefaultPropertyValue(0.5f), ToolTip
        (
            "Multiplies every area-subjective threshold."
        )]
        public float areaScale { 
            set => _areaScale = Math.Clamp(value, 0.2f, 5f);
            get => _areaScale;
        }
        public float _areaScale;

        [Property("Hover Band-Aid"), DefaultPropertyValue(true), ToolTip
        (
            "Wacom PTK-x70 - keep enabled."
        )]
        public bool hoverbandaid { set; get; }

        [Property("High Confidence Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Wacom PTK-x70 - keep enabled."
        )]
        public bool testToggle { set; get; }

        [Property("correction behavior mod 1"), DefaultPropertyValue(0.0f), ToolTip
        (
            "*Possible* value ranges from 0.0 - 1.0.\n" +
            "Default of 0 has slight relative undershoot on high accel and extremely slight relative overshoot on sharp decel.\n" +
            "Higher - this is influenced the opposite way in position correction.\n" +
            "Unsure - keep at 0."
        )]
        public float dCorrect { 
            set => _dCorrect = Math.Clamp(value, 0f, 1.0f);
            get => _dCorrect;
        }
        public float _dCorrect;

        [Property("correction behavior mod 2"), DefaultPropertyValue(5.0f), ToolTip
        (
            "Higher = less chance of bad overshoot.\n" +
            "Unsure - keep at 5."
        )]
        public float tv2 { 
            set => _tv2 = Math.Clamp(value, 0f, 100.0f);
            get => _tv2;
        }
        public float _tv2;

        public event Action<IDeviceReport> Emit;

    
        protected override void ConsumeState()
        {
            if (!init) {
                Initialize();
                init = true;
                emergency = 6;
                eflag = false;
            }
            if (State is ITabletReport report)
            {
                reportTime = (float)reportStopwatch.Restart().TotalMilliseconds;
                if (reportTime < 25) {
                    if (msOverride == 0) {
                        reportMsAvg += ((reportTime - reportMsAvg) * 0.1f);
                        expectC = reportMsAvg / expect;
                        correctWeight = startCorrectWeight * expect * (msStandard / reportMsAvg);
                    }
                    if (emergency > 0)
                    emergency--;
                }
                else {
                    emergency = 5;
                    eflag = false;
                }

                moveOk = false;
                consume = true;

                StatUpdate(report);
                ConditionalUpdate();
                
                bottom = -1 * Math.Max(alpha0 - vtlimiter, 0);
                if (top > 1f || bottom > 1f) {
                    top = 0;
                    bottom = 0;
                }

               /* Console.WriteLine("pathdiff (X = over/undershoot): " + pathdiffs[0]);

                Console.WriteLine("report milliseconds: " + reportTime);

                Console.WriteLine("raw velocity: " + vel[0]);

                Console.WriteLine("raw accel: " + accel[0]);

                Console.WriteLine("raw jerk: " + jerk[0]);

                Console.WriteLine("cch: " + cmod1);  */

                if (wire) {
                    
                    UpdateState();
                }
            }
            else {
                OnEmit();
            } 
        }


        protected override void UpdateState()
        {
            if (State is ITabletReport report && PenIsInRange())
            {
                updateTime = (float)updateStopwatch.Restart().TotalMilliseconds;

                if (emergency > 0) {
                    report.Position = pos[0];
                    ldOutput = pos[0];
                    aemaOutput = pos[0];
                    ringOutput = pos[0];
                    iRingPos0 = pos[0];
                    InsertAtFirst(smpos, pos[0]);
                    if (emergency > 1 && eflag) {
                        cTime = ((float)reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg) * (expect);
                        float scale = Math.Min((((float)(5 - emergency) + Math.Min(cTime, 1.0f)) * 0.2f), 1.0f);
                        report.Position = Vector2.Lerp(lastOutputPos, pos[0], scale);
                    }
                    else lastOutputPos = pos[0]; 
                    top = 0;
                    OnEmit();
                    return;
                }

                if (consume) {
                    alpha1 = 0;
                    if ((alpha0PreservationSociety > 1) && (top < 1)) {
                        top = (alpha0PreservationSociety - 1);            
                        bottom = 0;
                    }
                    else top = 0;
                } 
                cTime = ((float)reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg) * (expect);
                alpha0 = ((1 - top) * cTime) + top;
                top *= 0.99f;

                alpha0PreservationSociety = alpha0 + (expect / reportMsAvg);
                alpha0 += (vtlimiter - 1);
                alpha0 = Math.Clamp(alpha0, (vtlimiter - 1), pathpreservationsociety);

                trDir = Trajectory(stdir[0], stdir[1], stdir[2], alpha0);
                sdirt1 = Trajectory(a1stdir[0], a1stdir[1], a1stdir[2], alpha0 + 0.5f);
                trDir = Vector2.Lerp(trDir, sdirt1, pps4);
                LD();
                ldDir = WireAdjust(ldDir / expectC, expect, updateTime, wire);
    
                ldOutput += ldDir;

                RF();

                if (moveOk) {
                    Vector2 cDir = (trDir - (trDir - (stdir[1] / reportMsAvg))) * Math.Max(cTime + (vtlimiter - 3), 0.0f) * expectC;
                    Vector2 hDir = cDir * cmod1;
                    Vector2 hard = testToggle ? smpos[0] + hDir: smpos[0];
                    float cWeight = WireAdjust(adjdWeight, expect, updateTime, wire) / (1 + dscale);
                    float dWeight = tv2 * cWeight * (dscale + dscalebonus);
                    ldOutput = Vector2.Lerp(ldOutput, hard, cWeight * cmod1);
                    ldOutput = Vector2.Lerp(ldOutput, smpos[0], dWeight);
                    ringOutput = Vector2.Lerp(ringOutput, hard, cWeight * cmod1);
                    ringOutput = Vector2.Lerp(ringOutput, smpos[0], dWeight);
                } 
              
                AEMA();

                report.Position = aemaOutput;
                dirOfOutput = (report.Position - lastOutputPos) / updateTime;
                lastOutputPos = report.Position;
                report.Pressure = pressure[0];

                if (!vec2IsFinite(report.Position + ringOutput + iRingPos0 + ldOutput) | liftorpress) {
                    report.Position = pos[0];
                    aemaOutput = pos[0];
                    ldOutput = pos[0];
                    ringOutput = pos[0];
                    iRingPos0 = pos[0];
                    InsertAtFirst(smpos, pos[0]);
                    lastOutputPos = pos[0];
                    emergency = 5;
                    OnEmit();
                    return;
                }
                
                consume = false;
                
               // Plot();

                OnEmit();
            }
        }
        
        void StatUpdate(ITabletReport report) {
            InsertAtFirst(pos, report.Position);

            Vector2 smoothed = pos[0];
            if (reverseSmoothing < 1f && reverseSmoothing > 0f) {
                smoothed = pos[1] + (pos[0] - pos[1]) / reverseSmoothing;
            }

            InsertAtFirst(smpos, smoothed);
            InsertAtFirst(pressure, report.Pressure);
            InsertAtFirst(dir, smpos[0] - smpos[1]);
            InsertAtFirst(vel, dir[0].Length());
            InsertAtFirst(ddir, dir[0] - dir[1]);
            InsertAtFirst(accel, vel[0] - vel[1]);
            InsertAtFirst(jerk, accel[0] - accel[1]);
            InsertAtFirst(pointaccel, ddir[0].Length());
            if (emergency == 0) {
                InsertAtFirst(pathdiffs, Line.PathDiff(pos[1], pos[0], lastOutputPos));
                InsertAtFirst(upds, Line.UPD(pos[1], pos[0], lastOutputPos));
            }
            DAC();

            if (((hoverbandaid) && (pressure[0] > 0 && pressure[1] == 0) || (pressure[0] == 0 && pressure[1] > 0)) || (dir[0] == pos[0])) {
                if (emergency == 0) eflag = true;
                emergency = 5;
            }

            dscale = FSmoothstep(accel[0] - Math.Max(0, jerk[0]), -10 * areaScale, -200 * areaScale);
            dscalebonus = FSmoothstep(pathdiffs[0].X, 0, 25) * FSmoothstep(vel[0] + accel[0], 50, 0);
            vascale = FSmoothstep(vel[0] + accel[0], 25 * areaScale, 100 * areaScale);

            float bonus = FSmoothstep((accel[0] + jerk[0]), 10, 200);
            cmod1 = (1 - dCorrect) + (dCorrect) * (MathF.Pow(FSmoothstep((accel[0] + jerk[0]), -200, -10), 2) + (2 * bonus - bonus * bonus));

            pathpreservationsociety = Math.Min(Math.Min(stdir[0].Length(), stdir[1].Length()), stdir[2].Length());
            pathpreservationsociety = FSmoothstep(pathpreservationsociety, 2, 20);
            pps2Dir = (stdir[0] + stdir[1]) - (stdir[2] + stdir[2]);
            pps2 = FSmoothstep(pps2Dir.Length(), 1, 15);
            pps3 = FSmoothstep(Vector2.Distance(stdir[0], stdir[1]), dacInner, adjDacOuter);
            pathpreservationsociety = 2 + (vtlimiter - 2) * Math.Min(Math.Min(pathpreservationsociety, pps2), pps3);
            pps4 = FSmoothstep(stdir[3].Length() - stdir[0].Length(), -15, -3) - FSmoothstep(stdir[3].Length() - stdir[0].Length(), 3, 15);

            if (hoverbandaid && pressure[0] == 0) {
                pathpreservationsociety = Math.Min(pathpreservationsociety, 3 - FSmoothstep(Vector2.Distance(ddir[0], ddir[1]), 70, 100));
            }
        }

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
            if (accel[1] > 0 && accel[0] < 0) {
                arc = (dir[0] - dir[2]) / 2;
            }
        }

        void DAC() {
            if (dacToggle) {
                float vscale = FSmoothstep(vel[0], 5, 10 + dacOuter);
                float scale = MathF.Pow(FSmoothstep(Math.Max(pointaccel[0], Vector2.Distance(stdir[0], dir[0])), Math.Max(0, vscale * dacInner), 0.01f + (vscale * adjDacOuter)), 3);
                adjdWeight = correctWeight * Math.Max(scale + 1 - vscale, 0.25f);
                Vector2 stabilized = Vector2.Lerp(stdir[0], dir[0], scale); 
                if (vel[0] >= 1 && vel[1] >= 1 && vel[0] < 150 * areaScale && stabilized.Length() > 1) {
                    float ascale = Math.Max(Math.Abs(accel[0]), Math.Abs(vel[0] - stdir[0].Length()));
                    stabilized = Vector2.Lerp(stabilized, stdir[0].Length() * Vector2.Normalize(stabilized), vscale * (1 - scale) * (FSmoothstep(ascale, 0, vOuter)));
                }
                InsertAtFirst(stdir, stabilized);
                InsertAtFirst(a1stdir, (stdir[1] + stdir[0]) / 2);
                unaccounted += (stdir[0] - dir[0]);
            }
            else {
                InsertAtFirst(stdir, dir[0]);
                InsertAtFirst(a1stdir, (stdir[1] + stdir[0]) / 2);
            }
        }

        void LD() {
            if (ldToggle) {
                if (clusterjumping && accel[0] < 0 && namelesstime1 > 6 && peakAccel1 > 25 * areaScale) {
                    linedrivetime = Math.Min(linedrivetime + 1, namelesstime1);
                    float scale1 = MathF.Pow(DotNorm(trDir - clusterdir1, Vector2.Zero - clusterdir1, 0), 5);
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
                ringInputPos1 = ringInputPos0;
                ringInputPos0 = ldOutput;
                ringInputDir = ringInputPos0 - ringInputPos1;
                Vector2 dist = ldOutput - iRingPos0;
                iRingPos1 = iRingPos0;
                iRingPos0 += Math.Max(0, dist.Length() - (rInner)) * Default(Vector2.Normalize(dist), Vector2.Zero);
                ringDir = iRingPos0 - iRingPos1;
                ringOutput += ringDir;
                if (ringDir.Length() > 0 || dist.Length() > rInner || accel[0] < -10  * areaScale|| vel[0] > 10 * rInner) {
                    ringOutput = capDist(ringOutput, Vector2.Lerp(ringOutput, ldOutput, FSmoothstep(ringDir.Length(), -1, oMult * rInner)), (rInner));
                    ringOutput = Vector2.Lerp(ringOutput, ldOutput, FSmoothstep(accel[0], -10 * areaScale, -200 * areaScale));
                    moveOk = true;
                }
                else moveOk = false;
            }
            else {
                moveOk = true;
                ringOutput = ldOutput;
            }
        }

        void AEMA() {
            float weight = 1;
            if (aemaToggle) {
                weight = stockWeight;
                float mod1 = (1f - stockWeight) * (FSmoothstep(vel[0], 25 * areaScale, 50 * areaScale) - FSmoothstep(vel[0], 150 * areaScale, 250 * areaScale)) * FSmoothstep(MathF.Abs(accel[0]), 30 * areaScale, 10 * areaScale);
                float dist = Vector2.Distance(aemaOutput, ringOutput);
                float mod2 = mod1 * FSmoothstep(dist, 0, 50 * areaScale);
                float mod3 = (1f - stockWeight) * FSmoothstep(dist, 0, 100 * areaScale) * FSmoothstep(accel[0] + Math.Min(0, -jerk[0]), -10 * areaScale, -30 * areaScale);
                float mod4 = (1 + MathF.Log10(Math.Max(aResponse, 0.75f))) * stockWeight * MathF.Pow(FSmoothstep(dist, 2500 * aResponse * areaScale, (500 * aResponse * areaScale) - 1.0f) * FSmoothstep(accel[0] + Math.Max(0, jerk[0]), 10 * areaScale, 30 * areaScale), 5) * DotNorm(ddir[0], dir[0], 0);
                weight += Math.Max(mod2, mod3) - mod4;
                weight = Math.Clamp(weight, 0, 1);
            }
            aemaOutput = Vector2.Lerp(aemaOutput, ringOutput, weight);
        }

        void Initialize() {
            if (msOverride > 0) {
                reportMsAvg = msOverride;
                expectC = reportMsAvg / expect;
                correctWeight = startCorrectWeight * expect * (msStandard / msOverride);
                if (!dacToggle) {
                    adjdWeight = correctWeight;
                }
            }
            adjDacOuter = Math.Max(dacOuter, dacInner + 0.01f);
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

        float WireAdjust(float a, float be, float br, bool w) => w ? a * (br / be) : a;

        Vector2 WireAdjust(Vector2 a, float be, float br, bool w) => w ? a * (br / be) : a;

        float DotNorm(Vector2 a, Vector2 b) => Vector2.Dot(Vector2.Normalize(a), Vector2.Normalize(b));

        float DotNorm(Vector2 a, Vector2 b, float x) => (a != Vector2.Zero && b != Vector2.Zero) ? Vector2.Dot(Vector2.Normalize(a), Vector2.Normalize(b)) : x;

        float Default(float a, float b) => float.IsFinite(a) ? a : b;

        Vector2 Default(Vector2 a, Vector2 b) => vec2IsFinite(a) ? a : b;

        Vector2 MinLength(Vector2 a, Vector2 b) => a.LengthSquared() <= b.LengthSquared() ? a : b;

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

            public static float FlatOverUnder(Vector2 s, Vector2 e, Vector2 p) {
                if (e == p) return 0f;
                Vector2 mp = p - s;
                Vector2 me = e - s;
                float ca = -MathF.Atan2(me.Y, me.X);
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                Vector2 pd = rp - re;
                Vector2 npd = Vector2.Normalize(pd);
                return Vector2.Dot(npd, new Vector2(1, 0)) * pd.Length();
            }

            public static Vector2 UPD(Vector2 s, Vector2 e, Vector2 p) {
                if (e == p) return Vector2.Zero;
                Vector2 mp = p - s;
                Vector2 me = e - s;
                float a = MathF.Atan2(me.Y, me.X);
                float ca = -a;
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                Vector2 pd = rp - re;
                return Rotate(new Vector2(0, pd.Y), a);
            }

        }

        void InsertAtFirst<T>(T[] arr, T element)
        {
            for (int p = arr.Length - 1; p > 0; p--) arr[p] = arr[p - 1];
            arr[0] = element;
        }

        const int HMAX = 4;

        Vector2 planestart, planeend, peak;

        Vector2[] pos = new Vector2[HMAX];
        Vector2[] dir = new Vector2[HMAX];
        Vector2[] stdir = new Vector2[HMAX];
        Vector2[] ddir = new Vector2[HMAX];
        Vector2[] a1stdir = new Vector2[HMAX];
        Vector2[] smpos = new Vector2[HMAX];
        Vector2[] pathdiffs = new Vector2[HMAX];
        Vector2[] upds = new Vector2[HMAX];
        float[] vel = new float[HMAX];
        float[] accel = new float[HMAX];
        float[] jerk = new float[HMAX];
        float[] pointaccel = new float[HMAX];
        uint[] pressure = new uint[HMAX];

        float dscalebonus;
        Vector2 unaccounted = Vector2.Zero;
        float expectC;
        float dscale, vascale, scscale;
        float peakMag, planeMag;
        float cTime;
        Vector2 clusterpos0, clusterpos1;
        Vector2 clusterdir0, clusterdir1;
        Line ctozero;
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
        bool init = false;
        float adjdWeight;
        bool eflag;
        const float startCorrectWeight = 0.01f;
        const float msStandard = 3.302466f;
        float correctWeight;
        float adjDacOuter;
        float cmod1 = 1;

        float expect => 1000 / Frequency;
    }
}