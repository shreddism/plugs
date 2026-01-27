using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace DualEMA
{
    [PluginName("DualEMA")]
    public class DualEMA : IPositionedPipelineElement<IDeviceReport>
    {
        public DualEMA() : base()
        {
        }

       public PipelinePosition Position => PipelinePosition.PreTransform;

       public event Action<IDeviceReport> Emit;

       const float expect = 1;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                float consumeTime = (float)consumeStopwatch.Restart().TotalMilliseconds;

                float oops = Math.Clamp(consumeTime / expect, 0, 1);

                StatUpdate(report);

                if (pos[HMAX - 1] == Vector2.Zero) {
                    outputPos = pos[0];
                    report.Position = outputPos;
                    Emit?.Invoke(value);
                    return;
                }


                Vector2 dird = (dir[0] / oops) - outputDir;

                outputDir += 0.5f * oops * dird;

                outputPos += outputDir * oops;

                Vector2 posd = pos[0] - outputPos;
     
                outputPos += (0.5f * oops) * posd;

                report.Position = outputPos;   

                Console.WriteLine(oops);
            }
            Emit?.Invoke(value);
        }

        void StatUpdate(ITabletReport report) {
            InsertAtFirst(pos, report.Position);
            InsertAtFirst(dir, pos[0] - pos[1]);
            InsertAtFirst(ddir, dir[0] - dir[1]);
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

        float DotNorm(Vector2 a, Vector2 b) {
            if (a != Vector2.Zero && b != Vector2.Zero)
                return Vector2.Dot(Vector2.Normalize(a), Vector2.Normalize(b));
            else return 1;
        }

        const int HMAX = 4;

        Vector2[] pos = new Vector2[HMAX];
        Vector2[] dir = new Vector2[HMAX];
        Vector2[] ddir = new Vector2[HMAX];

        Vector2 outputPos, outputDir;

        void InsertAtFirst<T>(T[] arr, T element)
        {
            for (int p = arr.Length - 1; p > 0; p--) arr[p] = arr[p - 1];
            arr[0] = element;
        }

        Vector2 MaxLength(Vector2 a, Vector2 b) => a.Length() >= b.Length() ? a : b;

        Vector2 MinLength(Vector2 a, Vector2 b) => a.Length() <= b.Length() ? a : b;

        Vector2 MaxDist(Vector2 p, Vector2 a, Vector2 b) => Vector2.Distance(p, a) >= Vector2.Distance(p, b) ? a : b;

        Vector2 MinDist(Vector2 p, Vector2 a, Vector2 b) => Vector2.Distance(p, a) <= Vector2.Distance(p, b) ? a : b;

        Vector2 capDist(Vector2 a, Vector2 b, float d) => a + MathF.Min(Vector2.Distance(b, a), d) * (vec2IsFinite(Vector2.Normalize(b - a)) ? Vector2.Normalize(b - a) : Vector2.Zero); 

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

        private HPETDeltaStopwatch consumeStopwatch = new HPETDeltaStopwatch();

    }
}