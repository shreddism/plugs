using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Plugin260111
{
    [PluginName("Plugin260111")]
    public class Plugin260111 : IPositionedPipelineElement<IDeviceReport>
    {
        public Plugin260111() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        [Property("opt1"), DefaultPropertyValue(3f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float opt1 { 
            set => _opt1 = value;
            get => _opt1;
        }
        public float _opt1;

        [Property("opt2"), DefaultPropertyValue(0.25f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float opt2 { 
            set => _opt2 = value;
            get => _opt2;
        }
        public float _opt2;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                pos1 = pos0;
                pos0 = report.Position;
                dir0 = pos0 - pos1;
            
                if (reportStopwatch.Restart().TotalMilliseconds < 25) {
                    
                FakeRadialFollow(dir0);

                mag1 = mag0;
                    mag0 = FRFPoint.Length();


                if (mag1 >= 1 && mag0 >= 1 && mag0 < 100) {
               FRFPoint = Vector2.Lerp(FRFPoint, mag1 * Vector2.Normalize(FRFPoint), FSmootherstep(mag0 / mag1, 0.8f, 0.95f) - FSmootherstep(mag0 / mag1, 1.05f, 1.15f));
              
                }
            
                outputPos0 += FRFPoint;
                }
                else {
                    outputPos0 = pos0;
                    FRFPoint = Vector2.Zero;
                }

                outputPos0 = Vector2.Lerp(outputPos0, pos0, 0.05f);

                diff = dir0 - FRFPoint;
                report.Position = outputPos0;
              //  Plot();
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
            FRFPoint = Vector2.Lerp(FRFPoint, p0, FSmootherstep(Vector2.Distance(FRFPoint, p0), MathF.Max(0, opt2 * MathF.Log(dir0.Length() + 1) - 1), 0.01f + opt1 * (MathF.Log(dir0.Length() / 1 + 1))));
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
        float mag0, mag1;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();

    }
}