using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace dac2
{
    [PluginName("dac2")]
    public class dac2 : IPositionedPipelineElement<IDeviceReport>
    {
        public dac2() : base()
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

        [Property("Inner 'Radius'"), DefaultPropertyValue(0f), ToolTip
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

        [Property("Outer 'Radius'"), DefaultPropertyValue(2f), ToolTip
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

        [Property("Velocity Outer 'Range'"), DefaultPropertyValue(3f), ToolTip
        (
            "Will act the same, but for magnitude of direction.\n" +
            "No functionality changes, this was just internally set to the above option in early builds." 
        )]
        public float vOuter { 
            set => _vOuter = Math.Max(value, 0.1f);
            get => _vOuter;
        }
        public float _vOuter;

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
                if (reportTime < 25) {
                    float vscale = FSmoothstep(vel[0], 5, 10 + dacOuter);
                    float scale = MathF.Pow(FSmoothstep(Math.Max(pointaccel[0], Vector2.Distance(stdir[0], dir[0])), Math.Max(0, vscale * dacInner), 0.01f + (vscale * dacOuter)), 3);
                    Vector2 stabilized = Vector2.Lerp(stdir[0], dir[0], scale);
                    if (vel[0] >= 1 && vel[1] >= 1 && vel[0] < 100 * areaScale && stabilized.Length() > 1) {
                        float ascale = Math.Max(Math.Abs(accel[0]), Math.Abs(vel[0] - stdir[0].Length()));
                        stabilized = Vector2.Lerp(stabilized, stdir[0].Length() * Vector2.Normalize(stabilized), vscale * (1 - scale) * (FSmoothstep(ascale, 0, vOuter)));
                    }
                    InsertAtFirst(stdir, stabilized * timeScale);
                    unaccounted += (stdir[0] - dir[0]);
                    outputPos += stdir[0];
                    fucking += dir[0];
                    Console.WriteLine(unaccounted);
                    Console.WriteLine(outputPos - fucking);
                    Console.WriteLine(outputPos - pos[0]);
                }
                else {
                    InsertAtFirst(stdir, Vector2.Zero);
                    outputPos = pos[0];
                    fucking = pos[0];
                    unaccounted = Vector2.Zero;
                }

                if (vec2IsFinite(outputPos)) {
                    outputPos -= 0.005f * unaccounted;
                    unaccounted *= 0.995f;
                    report.Position = outputPos;
                }
                else {
                    InsertAtFirst(stdir, Vector2.Zero);
                    outputPos = pos[0];
                    fucking = pos[0];
                    unaccounted = Vector2.Zero;
                }

            }
            Emit?.Invoke(value);
        }

        void StatUpdate(ITabletReport report) {
            InsertAtFirst(pos, report.Position);
            InsertAtFirst(dir, pos[0] - pos[1]);
            InsertAtFirst(vel, dir[0].Length());
            InsertAtFirst(accel, vel[0] - vel[1]);
        }

       /* void Plot() {
            Console.Write("vx");
            Console.WriteLine(tp.X);
            Console.Write("vy");
            Console.WriteLine(tp.Y * -1);
            Console.WriteLine("xx");
            Console.WriteLine("dd");
        }*/

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

        Vector2 unaccounted = Vector2.Zero;

        Vector2[] pos = new Vector2[HMAX];
        Vector2[] dir = new Vector2[HMAX];
        Vector2[] stdir = new Vector2[HMAX];
        Vector2[] ddir = new Vector2[HMAX];
        float[] vel = new float[HMAX];
        float[] accel = new float[HMAX];
        float[] pointaccel = new float[HMAX];

        Vector2 outputPos;

        Vector2 fucking;

        float reportTime, timeScale;

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();

    }
}
