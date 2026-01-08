using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Plugin260107
{
    [PluginName("Plugin260107")]
    public class Plugin260107 : IPositionedPipelineElement<IDeviceReport>
    {
        public Plugin260107() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                pos0 = report.Position;
                FakeRadialFollow(pos0);
                diff = FRFPoint - pos0;
                report.Position = FRFPoint;
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
            FRFPoint = Vector2.Lerp(FRFPoint, p0, FSmootherstep(Vector2.Distance(FRFPoint, p0), 0, 100));
        }

        void Plot() {
            Console.Write("vx");
            Console.WriteLine((pos0.X - 18700) / 200);
            Console.Write("vy");
            Console.WriteLine((pos0.Y - 10500) / -200);
            Console.Write("ax");
            Console.WriteLine((FRFPoint.X - 18700) / 200);
            Console.Write("ay");
            Console.WriteLine((FRFPoint.Y - 10500) / -200);
            Console.Write("jx");
            Console.WriteLine(diff.X);
            Console.Write("jy");
            Console.WriteLine(diff.Y * -1);
            Console.WriteLine("xx");
            Console.WriteLine("dd");
        }

        Vector2 FRFPoint, pos0, diff;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}