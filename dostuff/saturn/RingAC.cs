using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Saturn
{
    [PluginName("Saturn - Ring Antichatter")]
    public class RingAC : IPositionedPipelineElement<IDeviceReport>
    {
        public RingAC() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        [Property("Area Scale"), DefaultPropertyValue(0.5f), ToolTip
        (
            "Multiplies every area-subjective threshold."
        )]
        public float areaScale { 
            set => _areaScale = Math.Clamp(value, 0.01f, 100f);
            get => _areaScale;
        }
        public float _areaScale;

        [Property("Ring Radius"), DefaultPropertyValue(5f), ToolTip
        (
            "The cursor will not move if it has not moved this much. Unit is raw data."
        )]
        public float rInner { 
            set => _rInner = Math.Clamp(value, 0f, 1000000.0f);
            get => _rInner;
        }
        public float _rInner;

        [Property("Outer Radial Mult"), DefaultPropertyValue(2f), ToolTip
        (
            "Useful values range from 0 to ~10.\n" +
            "A slight latency compromise to be made if hovering."
        )]
        public float oMult { 
            set => _oMult = Math.Clamp(value, 0f, 1000000.0f);
            get => _oMult;
        }
        public float _oMult;

        [Property("Expected Time"), DefaultPropertyValue(false), ToolTip
        (
            "Only enable this if you know what you are doing."
        )]
        public bool asyncwire { set; get; }

        [Property("Expected Time Override"), DefaultPropertyValue(1.0f), ToolTip
        (
            "You should know what you are doing if you change this."
        )]
        public float expectC { set; get; }

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                reportTime = (float)reportStopwatch.Restart().TotalMilliseconds;
                timeScale = asyncwire ? (reportTime / expectC) : 1;
                StatUpdate(report);
                ringInputPos1 = ringInputPos0;
                ringInputPos0 = pos[0];
                ringInputDir = ringInputPos0 - ringInputPos1;
                Vector2 dist = pos[0] - iRingPos0;
                iRingPos1 = iRingPos0;
                iRingPos0 += Math.Max(0, dist.Length() - (rInner)) * Default(Vector2.Normalize(dist), Vector2.Zero);
                ringDir = iRingPos0 - iRingPos1;
                ringOutput += ringDir;

                if (ringDir.Length() > 0 || dist.Length() > rInner || accel[0] < -10  * areaScale|| vel[0] > 10 * rInner) {
                ringOutput = capDist(ringOutput, Vector2.Lerp(ringOutput, pos[0], timeScale * FSmoothstep(ringDir.Length(), -1, oMult * rInner)), 2000f);
                ringOutput = Vector2.Lerp(ringOutput, pos[0], FSmoothstep(accel[0], -10 * areaScale, -200 * areaScale));
                }
                if (vec2IsFinite(ringOutput))
                report.Position = ringOutput;
            }
            Emit?.Invoke(value);
        }

        void StatUpdate(ITabletReport report) {
            InsertAtFirst(pos, report.Position);
            InsertAtFirst(dir, pos[0] - pos[1]);
            InsertAtFirst(vel, dir[0].Length() * timeScale);
            InsertAtFirst(accel, vel[0] - vel[1]);
        }

        void InsertAtFirst<T>(T[] arr, T element)
        {
            for (int p = arr.Length - 1; p > 0; p--) arr[p] = arr[p - 1];
            arr[0] = element;
        }

        public static float FSmoothstep(float x, float start, float end)
        {
            x = Math.Clamp((x - start) / (end - start), 0.0f, 1.0f);
            return x * x * (3.0f - 2.0f * x);
        }

        const int HMAX = 4;
        Vector2[] pos = new Vector2[HMAX];
        Vector2[] dir = new Vector2[HMAX];
        float[] vel = new float[HMAX];
        float[] accel = new float[HMAX];

        Vector2 Default(Vector2 a, Vector2 b) => vec2IsFinite(a) ? a : b;

        Vector2 capDist(Vector2 a, Vector2 b, float d) => a + Math.Min(Vector2.Distance(b, a), d) * (vec2IsFinite(Vector2.Normalize(b - a)) ? Vector2.Normalize(b - a) : Vector2.Zero);

        Vector2 ringInputPos0, ringInputPos1, iRingPos0, iRingPos1, ringDir, ringOutput, ringInputDir;

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

        float reportTime, timeScale;

        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
    }
}