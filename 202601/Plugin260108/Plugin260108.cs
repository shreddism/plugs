using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Plugin260108
{
    [PluginName("Plugin260108")]
    public class Plugin260108 : IPositionedPipelineElement<IDeviceReport>
    {
        public Plugin260108() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                pos1 = pos0;
                pos0 = report.Position;
                dir0 = pos0 - pos1;
            
                if (reportStopwatch.Restart().TotalMilliseconds < 25) {
                FakeRadialFollow(dir0);
                outputPos0 += FRFPoint;
                }
                else {
                    outputPos0 = pos0;
                    FRFPoint = Vector2.Zero;
                }

                outputPos0 = Vector2.Lerp(outputPos0, pos0, 0.01f);

                diff = dir0 - FRFPoint;
                report.Position = outputPos0;
                Plot();
            }
            Emit?.Invoke(value);
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

        void FakeRadialFollow(Vector2 p0) {
            FRFPoint = Vector2.Lerp(FRFPoint, p0, FSmootherstep(Vector2.Distance(FRFPoint, p0), 0, 0.5f + 4f * (MathF.Log(dir0.Length() / 1 + 1))));
        }

        void Plot() {
            Console.Write("vx");
            Console.WriteLine(dir0.X);
            Console.Write("vy");
            Console.WriteLine(dir0.Y * -1);
            Console.Write("ax");
            Console.WriteLine(FRFPoint.X);
            Console.Write("ay");
            Console.WriteLine(FRFPoint.Y * - 1);
            Console.Write("jx");
            Console.WriteLine(diff.X);
            Console.Write("jy");
            Console.WriteLine(diff.Y * -1);
            Console.WriteLine("xx");
            Console.WriteLine("dd");
        }

        Vector2 FRFPoint, pos0, pos1, dir0, diff, outputPos0;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();

    }
}