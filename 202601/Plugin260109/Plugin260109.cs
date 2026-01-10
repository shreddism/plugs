using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Plugin260109
{
    [PluginName("Plugin260109")]
    public class Plugin260109 : IPositionedPipelineElement<IDeviceReport>
    {
        public Plugin260109() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        [Property("opt1"), DefaultPropertyValue(0.1f), ToolTip
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

        [Property("opt2"), DefaultPropertyValue(0.05f), ToolTip
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
                dir1 = dir0;
                dir0 = pos0 - pos1;
                ddir5 = ddir4;
                ddir4 = ddir3;
                ddir3 = ddir2;
                ddir2 = ddir1;
                ddir1 = ddir0;
                ddir0 = dir0 - dir1;

                index = MathF.Max(MathF.Max(ddir0.Length(),ddir1.Length()), MathF.Max(ddir2.Length(),ddir3.Length()));
            
            
                //FakeRadialFollow(ddir0);
            //  Console.WriteLine(FRFPoint);
              FRFPoint = Vector2.Lerp(FRFPoint, ddir0, 0f + 1f * FSmootherstep(index, 0, 4));
                outputDir0 += FRFPoint;
                outputDir0 = Vector2.Lerp(outputDir0, dir0, 0.1f);
                outputPos0 += outputDir0;
                
                outputPos0 = Vector2.Lerp(outputPos0, pos0, 0.1f);
                report.Position = outputPos0;
            
              //  Console.WriteLine(index);

                diff = ddir0 - FRFPoint;

                

                
                Plot();
            }
            else if (value is IAuxReport FUCK) {
                if (FUCK.Raw[4] == 127) {
                    FRFPoint = Vector2.Zero;
                    outputDir0 = Vector2.Zero;
                    outputPos0 = pos0;
                }
                    
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
            FRFPoint = Vector2.Lerp(FRFPoint, p0, FSmootherstep(Vector2.Distance(FRFPoint, p0), MathF.Max(0, MathF.Log(opt2 * ddir0.Length() + 1) - 1), 1 + opt1 * (MathF.Log(ddir0.Length() / 1 + 1))));
        }

        void Plot() {
            Console.Write("vx");
            Console.WriteLine(ddir0.X);
            Console.Write("vy");
            Console.WriteLine(ddir0.Y * -1);
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

        Vector2 FRFPoint, pos0, pos1, dir0, dir1, ddir0, ddir1, ddir2, ddir3, ddir4, ddir5, diff, outputPos0, outputDir0, idxv;
        float index, alsoindex;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();

    }
}