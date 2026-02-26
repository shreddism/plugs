using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       
using System.Numerics;

namespace Saturn
{
    [PluginName("Saturn - Multifilter (Temporal Resampler)")]
    public class MultifilterTR : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public MultifilterTR() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        [Property("Prediction Ratio"), DefaultPropertyValue(0f), ToolTip
        (
            "Default: 0.5\n" +
            "Range: 0.0 - 1.0\n\n" +

            "Determines the time distance the filter predicts inputs for each tablet update.\n" +
            "Prediction brought to you by Kalman filtering.\n" +
            "0.0 == [slower] 0% predicted, one rps of latency\n" +
            "0.5 == [balanced] 50% predicted, half rps of latency\n" +
            "1.0 == [overkill] 100% predicted, no added latency (works best with some smoothing)"
        )]
        public float frameShift { 
            set => _frameShift = Math.Clamp(value, 0.0f, 1.0f);
            get => _frameShift;
        }
        public float _frameShift;

        [Property("Reverse EMA"), DefaultPropertyValue(1f), ToolTip
        (
            "Default: 1.0\n" +
            "Range: 0.0 - 1.0\n\n" +

            "Removes hardware smoothing, fine-tuned this to your tablet.\n" +
            "1.0 == no effect\n" +
            "lower == removes more hardware smoothing"
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

        [Property("Accel Response Aggressiveness"), DefaultPropertyValue(1.5f), ToolTip
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

        float correctWeight = 0.025f;

        [Property("Ring Antichatter Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Ring Antichatter is relatively simple, making it non-invasive and low-latency.\n" +
            "It works similarly to Radial Follow, but it ensures\n" +
            "to not underaim if the radius is anything over the raw tablet noise."
        )]
        public bool ringToggle { set; get; }

        [Property("Ring Radius"), DefaultPropertyValue(5f), ToolTip
        (
            "The cursor will not move if it has not moved this much. Unit is raw data. Shouldn't be very high."
        )]
        public float rInner { 
            set => _rInner = Math.Clamp(value, 0f, 1000000.0f);
            get => _rInner;
        }
        public float _rInner;

        [Property("Outer Radial Mult"), DefaultPropertyValue(1f), ToolTip
        (
            "Useful values range from 0 to ~10.\n" +
            "Shouldn't be very high."
        )]
        public float oMult { 
            set => _oMult = Math.Clamp(value, 0f, 1000000.0f);
            get => _oMult;
        }
        public float _oMult;

        [Property("wire"), DefaultPropertyValue(true), ToolTip
        (
            "You should definitely leave this enabled unless your specific situation requires otherwise.\n" +
            "If the filter is breaking, disabling this may solve it.\n" +
            "Equivalent to 'extraFrames' from Temporal Resampler."
        )]
        public bool wire { set; get; }

       [Property("msOverride"), DefaultPropertyValue(0f), ToolTip
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

        [Property("Hover Band-Aid"), DefaultPropertyValue(false), ToolTip
        (
            "Wacom PTK-x70 - keep enabled."
        )]
        public bool hoverbandaid { set; get; }

        [Property("High Confidence Toggle"), DefaultPropertyValue(false), ToolTip
        (
            "ok"
        )]
        public bool testToggle { set; get; }

        public event Action<IDeviceReport> Emit;

        protected override void ConsumeState()
        {
            if (State is ITabletReport report)
            {   
                if (!init) {
                    ResetValues(report.Position);
                    Initialize();
                    init = true;
                    emergency = 6;
                    eflag = false;
                    return;
                }

                reportTime = (float)reportStopwatch.Restart().TotalMilliseconds;
                consumeDelta = reportTime / 1000f;
                if (reportTime < 25f && reportTime > 0.01f) {
                    if (msOverride == 0) {
                    reportMsAvg += ((reportTime - reportMsAvg) * 0.1f);
                    rpsAvg += (1f / (consumeDelta) - rpsAvg) * (1f - MathF.Exp(-2f * (consumeDelta)));
                    secAvg = 1f / rpsAvg;
                    msAvg = 1000f * secAvg;
                    correctWeight = startCorrectWeight * expect * (msStandard / reportMsAvg);
                    }

                    if (emergency > 0)
                    emergency--;

                  

                    
                }
                else {
                    emergency = 5;
                    eflag = false;
                    ResetValues(report.Position);
                }
            
                moveOk = false;
                consume = true;

            
                      
                StatUpdate(report);

        /*                Console.WriteLine("pathdiff (X = over/undershoot): " + PathDiff(pos[1], pos[0], lastOutputPos));

               Console.WriteLine("report milliseconds: " + reportTime);

               Console.WriteLine("raw velocity: " + vel[0]);

               Console.WriteLine("------"); */
               

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
                    ResetValuesWithoutKf(pos[0]);
                    InsertAtFirst(smpos, pos[0]);
                    if (emergency > 1 && eflag) {
                        float cTime = ((float)reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg) * (expect);
                        float scale = Math.Min((((float)(5 - emergency) + Math.Min(cTime, 1.0f)) * 0.2f), 1.0f);
                        report.Position = Vector2.Lerp(lastOutputPos, pos[0], scale);
                    }
                    else lastOutputPos = pos[0];
                    OnEmit();
                    return;
                }

                float t = 1 + (float)(runningStopwatch.Elapsed - latestReport).TotalSeconds * rpsAvg;
                t = Math.Clamp(t, 0, 3);
    
                ldOutput = RTrajectory(t, prpos[2], prpos[1], prpos[0]);

                RF();

                if (moveOk) {
                    Vector2 hard = testToggle ? smpos[0] + (prpos[0] - smpos[0]) : smpos[0];
                    float cWeight = WireAdjust(adjdWeight, expect, updateTime, wire);
                    float dWeight = cWeight * FSmoothstep(accel[0], -10 * areaScale, -200 * areaScale);
                    ldOutput = Vector2.Lerp(ldOutput, hard, cWeight);
                    ldOutput = Vector2.Lerp(ldOutput, smpos[0], dWeight);
                    ringOutput = Vector2.Lerp(ringOutput, hard, cWeight);
                    ringOutput = Vector2.Lerp(ringOutput, smpos[0], dWeight);
                }
              
                AEMA();

                report.Position = aemaOutput;
                dirOfOutput = (report.Position - lastOutputPos) / updateTime;
                lastOutputPos = report.Position;
                report.Pressure = pressure[0];

                

                if (!vec2IsFinite(report.Position + ringOutput + iRingPos0 + ldOutput)) {
                    report.Position = pos[0];
                    aemaOutput = pos[0];
                    ldOutput = pos[0];
                    ringOutput = pos[0];
                    iRingPos0 = pos[0];
                    emergency = 3;
                    ResetValues(pos[0]);
                    OnEmit();
                    return;
                }
                Plot();

                consume = false;

                

                OnEmit();
            }
        }

        void StatUpdate(ITabletReport report) {
            InsertAtFirst(pos, report.Position);
            Vector2 smoothed = pos[0];
            if (reverseSmoothing < 1f && reverseSmoothing > 0f)
                smoothed = pos[1] + (pos[0] - pos[1]) / reverseSmoothing;
            InsertAtFirst(smpos, smoothed);
            InsertAtFirst(pressure, report.Pressure);
            InsertAtFirst(dir, smpos[0] - smpos[1]);
            InsertAtFirst(vel, dir[0].Length());
            InsertAtFirst(ddir, dir[0] - dir[1]);
            InsertAtFirst(accel, vel[0] - vel[1]);
            InsertAtFirst(jerk, accel[0] - accel[1]);
            InsertAtFirst(pointaccel, ddir[0].Length());
            DAC();

          //  Console.WriteLine(Vector2.Distance(pos[0], stpos[0]));
            
            Vector2 predict = stpos[0];
            crPoint = pos[0];
            if (frameShift > 0f) {
                predict = kf.Update(stpos[0], secAvg);
                crPoint = kf.Update(pos[0], secAvg);
                predict += (stpos[0] - predict) * (1f - frameShift);
                crPoint += (pos[0] - crPoint) * (1f - frameShift);
            }

            tOffset += secAvg - consumeDelta;
            tOffset *= MathF.Exp(-5f * consumeDelta);
            tOffset = Math.Clamp(tOffset, -secAvg, secAvg);
            latestReport = runningStopwatch.Elapsed + TimeSpan.FromSeconds(tOffset);

            InsertAtFirst(prpos, predict);

            
        //    Console.WriteLine(dir[0]);

            if (((hoverbandaid) && (pressure[0] > 0 && pressure[1] == 0) || (pressure[0] == 0 && pressure[1] > 0)) || (dir[0] == pos[0])) {
                if (emergency == 0) eflag = true;
                emergency = 5;
            }

            


/*
            pathpreservationsociety = Math.Min(Math.Min(stdir[0].Length(), stdir[1].Length()), stdir[2].Length());
            pathpreservationsociety = FSmoothstep(pathpreservationsociety, 0, 20);
            pps2Dir = (stdir[0] + stdir[1]) - (stdir[2] + stdir[2]);
            pps2 = FSmoothstep(pps2Dir.Length(), 0, 15);
            pps3 = FSmoothstep(Vector2.Distance(stdir[0], stdir[1]), dacInner, dacOuter);
            pathpreservationsociety = 2 + (vtlimiter - 2) * Math.Min(Math.Min(pathpreservationsociety, pps2), pps3);
            pps4 = FSmoothstep(stdir[3].Length() - stdir[0].Length(), -15, 0) - FSmoothstep(stdir[3].Length() - stdir[0].Length(), 0, 15);
            
        //    Console.WriteLine(pathpreservationsociety);

            if (pressure[0] == 0)
                pathpreservationsociety = Math.Min(pathpreservationsociety, 3 - FSmoothstep(Vector2.Distance(ddir[0], ddir[1]), 30, 69));  
                */
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
            Vector2 stpoint = stpos[0] + stdir[0];
            InsertAtFirst(stpos, stpoint);
            }
            else {
                InsertAtFirst(stdir, dir[0]);
                InsertAtFirst(stpos, pos[0]);
            }
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
                ringOutput = capDist(ringOutput, Vector2.Lerp(ringOutput, ldOutput, FSmoothstep(ringDir.Length(), -1, oMult * rInner)), 2000f);
                ringOutput = Vector2.Lerp(ringOutput, ldOutput, FSmoothstep(accel[0], -10 * areaScale, -200 * areaScale));
                moveOk = true;
                }
                else moveOk = false;
               // Console.WriteLine(ringOutput - ldOutput);
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
                float mod4 = (1 + MathF.Log10(Math.Max(aResponse, 0.75f))) * stockWeight * MathF.Pow(FSmoothstep(dist, 2500 * aResponse * areaScale, (500 * aResponse * areaScale) - 1.0f) * FSmoothstep(accel[0] + Math.Max(0, jerk[0]), 10 * areaScale, 30 * areaScale), 2) * DotNorm(ddir[0], dir[0], 0);
                weight += Math.Max(mod2, mod3) - mod4;
                weight = Math.Clamp(weight, 0, 1);
              //  Console.WriteLine(weight);
            }
            aemaOutput = Vector2.Lerp(aemaOutput, ringOutput, weight);
        }

        float WireAdjust(float a, float be, float br, bool w) => w ? a * (br / be) : a;

        Vector2 WireAdjust(Vector2 a, float be, float br, bool w) => w ? a * (br / be) : a;

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

        void ResetValues(Vector2 p) {
            kf = new KalmanVector2(4, p);
            ckf = new KalmanVector2(4, p);
            stpos = Enumerable.Repeat(p, stpos.Length).ToArray();
            smpos = Enumerable.Repeat(p, smpos.Length).ToArray();
            latestReport = runningStopwatch.Elapsed;
            tOffset = 0;
        }

        void ResetValuesWithoutKf(Vector2 p) {
            stpos = Enumerable.Repeat(p, stpos.Length).ToArray();
            smpos = Enumerable.Repeat(p, smpos.Length).ToArray();
            latestReport = runningStopwatch.Elapsed;
            tOffset = 0;
        }

        void Initialize() {
            if (msOverride > 0) {
                reportMsAvg = msOverride;
                correctWeight = startCorrectWeight * expect * (msStandard / msOverride);
                reportMsAvg = msAvg = msOverride;
                secAvg = reportMsAvg / 1000f;
                rpsAvg = 1f / secAvg;
                if (!dacToggle) {
                    adjdWeight = 0;
                }
            }
            adjDacOuter = Math.Max(dacOuter, dacInner + 0.01f);
        }

        float adjDacOuter;

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

        void InsertAtFirst<T>(T[] arr, T element)
        {
            for (int p = arr.Length - 1; p > 0; p--) arr[p] = arr[p - 1];
            arr[0] = element;
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

        private static readonly int steps = 256;
        private static readonly float dt = 1f / steps;
        private float[] arcArr = new float[steps];
        private float arcTar = 0;
        private Vector2 _v1, _v2, _v3;
        private int _floor;
        Vector2 RTrajectory(float t, Vector2 v3, Vector2 v2, Vector2 v1)
        {
            var mid = 0.5f * (v1 + v3);
            var accel = 2f * (mid - v2);
            var vel = 2f * v2 - v3 - mid;
            
            // if there is acceleration, then start spacing points evenly using integrals
            if (Vector2.Dot(accel, accel) > 0.001f)
            {
                int floor = (int)Math.Floor(t);
                var _vel = vel + accel * floor;

                // if any of the inputs have changed, recalculate arcArr
                if ((_floor != floor) || (_v1 != v1) || (_v2 != v2) || (_v3 != v3))
                {
                    _v1 = v1;
                    _v2 = v2;
                    _v3 = v3;
                    _floor = floor;
                    arcTar = 0;

                    for (int _t = 0; _t < steps; _t++)
                    {
                        arcArr[_t] = arcTar;
                        arcTar += (_vel + _t * dt * accel).Length();
                    }
                }

                float _arcTar = arcTar * (t - floor);

                for (int _t = 0; _t < steps; _t++)
                {
                    if (arcArr[_t] < _arcTar) continue;
                    t = _t * dt + floor;
                    break;
                }
            }

            return v3 + t * vel + 0.5f * t * t * accel;
        }

        Vector2 crPoint;

        const int HMAX = 4;

        Vector2 planestart, planeend, peak;

        Vector2[] pos = new Vector2[HMAX];
        Vector2[] dir = new Vector2[HMAX];
        Vector2[] stdir = new Vector2[HMAX];
        Vector2[] ddir = new Vector2[HMAX];
        Vector2[] a1stdir = new Vector2[HMAX];
        Vector2[] stpos = new Vector2[HMAX];
        Vector2[] smpos = new Vector2[HMAX];
        Vector2[] prpos = new Vector2[HMAX];
        float[] vel = new float[HMAX];
        float[] accel = new float[HMAX];
        float[] jerk = new float[HMAX];
        float[] pointaccel = new float[HMAX];
        uint[] pressure = new uint[HMAX];
        
        float peakMag, planeMag;
        float ohmygodbruh;
        Vector2 clusterpos0, clusterpos1;
        Vector2 clusterdir0, clusterdir1;
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
        bool eflag;
        const float startCorrectWeight = 0.01f;
        const float msStandard = 3.302466f;

        HPETDeltaStopwatch runningStopwatch = new HPETDeltaStopwatch(true);

        KalmanVector2 kf;
        KalmanVector2 ckf;
        TimeSpan latestReport = TimeSpan.Zero;
        float rpsAvg = 200f, tOffset;
        float msAvg = 5;
        float secAvg = 0.005f;
        float consumeDelta;
        float expect => 1000 / Frequency;
        bool init = false;
        float adjdWeight;

        public static Vector2 PathDiff(Vector2 s, Vector2 e, Vector2 p) {
                Vector2 mp = p - s;
                Vector2 me = e - s;
                float ca = -MathF.Atan2(me.Y, me.X);
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                return rp - re;
        }

        public static Vector2 Rotate(Vector2 p, float a) {
                float cosine = MathF.Cos(a);
                float sine = MathF.Sin(a);
                return new Vector2((cosine * p.X) - (sine * p.Y), (sine * p.X) + (cosine * p.Y));
        }

    }

    public class KalmanFilter
    {
        private readonly double[,] scale_const;
        private readonly int states;
        private double lastMeasuredPos;

        private Matrix x;
        private Matrix P;
        private Matrix Q;
        private Matrix R;
        private Matrix H;

        public KalmanFilter(uint statesNumber, double initialPosition)
        {
            states = (int)statesNumber + 2;

            scale_const = new double[states, states];
            for (int i = 0; i < states; i++)
            {
                int fac_n = 1;
                int fac_i = 0;
                for (int j = i; j < states; j++)
                {
                    scale_const[i, j] = 1d / fac_n;
                    fac_i++;
                    fac_n *= fac_i;
                }
            }

            lastMeasuredPos = initialPosition;
            double[,] xArr = new double[states, 1];
            xArr[0, 0] = initialPosition;

            x = Matrix.Build.DenseOfArray(xArr);
            P = Matrix.Build.DenseIdentity(states);
            Q = Matrix.Build.DenseIdentity(states) * 1;
            R = Matrix.Build.DenseDiagonal(2, 2, 0.0001);
            H = Matrix.Build.DenseDiagonal(2, states, 1);
        }

        public double Update(double measuredPos, double dt)
        {
            double measuredVel = (measuredPos - lastMeasuredPos) / dt;
            lastMeasuredPos = measuredPos;

            var z = Matrix.Build.DenseOfArray(new double[,] { { measuredPos }, { measuredVel } });

            double[,] Aarr = new double[states, states];
            for (int i = 0; i < states; i++) 
            {
                double time_pow = 1;
                for (int j = i; j < states; j++) 
                {
                    Aarr[i, j] = time_pow * scale_const[i, j];
                    time_pow *= dt;
                } 
            }

            /*
                vvvvvvvvvvv
            4 states should look like this
            double[,] Aarr = new double[,] {
                {          1,          dt^1/1!,    dt^2/2!,    dt^3/3!     },
                {          0,          1,          dt^1/1!,    dt^2/2!     },
                {          0,          0,          1,          dt^1/1!     },
                {          0,          0,          0,          1           }
            }
            */

            var A = Matrix.Build.DenseOfArray(Aarr);

            x = A * x;
            P = A * P * A.Transpose() + Q;

            var S = H * P * H.Transpose() + R;
            var K = P * H.Transpose() * S.Inverse();

            x = x + K * (z - H * x);
            P = (Matrix.Build.DenseIdentity(states) - K * H) * P;

            return (A * x)[0, 0];
        }
    }

    public class KalmanVector2
    {
        private KalmanFilter xFilter;
        private KalmanFilter yFilter;

        public KalmanVector2(uint states, Vector2 initialPosition)
        {
            xFilter = new KalmanFilter(states, initialPosition.X);
            yFilter = new KalmanFilter(states, initialPosition.Y);
        }

        public Vector2 Update(Vector2 measuredPosition, float dt)
        {
            float xState = (float)xFilter.Update(measuredPosition.X, dt);
            float yState = (float)yFilter.Update(measuredPosition.Y, dt);
            return new Vector2(xState, yState);
        }
    }

    public class Matrix
    {
        internal readonly double[,] data;

        public Matrix(double[,] data)
        {
            this.data = data;
        }

        public int Rows => data.GetLength(0);
        public int Cols => data.GetLength(1);

        public double this[int i, int j]
        {
            get => data[i, j];
            set => data[i, j] = value;
        }

        public static Matrix operator +(Matrix a, Matrix b)
        {
            var result = new double[a.Rows, a.Cols];
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Cols; j++)
                    result[i, j] = a[i, j] + b[i, j];
            return new Matrix(result);
        }

        public static Matrix operator -(Matrix a, Matrix b)
        {
            var result = new double[a.Rows, a.Cols];
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Cols; j++)
                    result[i, j] = a[i, j] - b[i, j];
            return new Matrix(result);
        }

        public static Matrix operator *(Matrix a, Matrix b)
        {
            var result = new double[a.Rows, b.Cols];
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < b.Cols; j++)
                    for (int k = 0; k < a.Cols; k++)
                        result[i, j] += a[i, k] * b[k, j];
            return new Matrix(result);
        }

        public static Matrix operator *(Matrix a, double scalar)
        {
            var result = new double[a.Rows, a.Cols];
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Cols; j++)
                    result[i, j] = a[i, j] * scalar;
            return new Matrix(result);
        }

        public Matrix Transpose()
        {
            var result = new double[Cols, Rows];
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    result[j, i] = data[i, j];
            return new Matrix(result);
        }

        public Matrix Inverse()
        {
            if (Rows != Cols) throw new InvalidOperationException("Matrix must be square to invert.");

            int n = Rows;
            var result = new double[n, n];
            var identity = Build.DenseIdentity(n).data;
            var copy = (double[,])data.Clone();

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    result[i, j] = identity[i, j];

            for (int i = 0; i < n; i++)
            {
                double diag = copy[i, i];
                if (diag == 0) throw new InvalidOperationException("Matrix is singular.");

                for (int j = 0; j < n; j++)
                {
                    copy[i, j] /= diag;
                    result[i, j] /= diag;
                }

                for (int k = 0; k < n; k++)
                {
                    if (k == i) continue;
                    double factor = copy[k, i];
                    for (int j = 0; j < n; j++)
                    {
                        copy[k, j] -= factor * copy[i, j];
                        result[k, j] -= factor * result[i, j];
                    }
                }
            }

            return new Matrix(result);
        }

        public static class Build
        {
            public static Matrix DenseOfArray(double[,] data) => new Matrix(data);

            public static Matrix DenseIdentity(int size)
            {
                var result = new double[size, size];
                for (int i = 0; i < size; i++) result[i, i] = 1;
                return new Matrix(result);
            }

            public static Matrix DenseDiagonal(int rows, int cols, Func<int, double> diagFunc)
            {
                var result = new double[rows, cols];
                for (int i = 0; i < Math.Min(rows, cols); i++)
                    result[i, i] = diagFunc(i);
                return new Matrix(result);
            }

            public static Matrix DenseDiagonal(int rows, int cols, double value)
            {
                return DenseDiagonal(rows, cols, _ => value);
            }
        }
    }
}

