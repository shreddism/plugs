using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace OTDPlot
{
    [PluginName("OTDPlot")]
    public class OTDPlot : IPositionedPipelineElement<IDeviceReport>
    {
        public OTDPlot() : base()
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

                output = 0.9f * output + 0.1f * dir0;


                Console.Write("ax");

                Console.WriteLine(dir0.X);

                Console.Write("ay");

                Console.WriteLine(dir0.Y * -1);

                Console.WriteLine("xx");

                Console.WriteLine("dd");
                
                
            }
            Emit?.Invoke(value);
        }

        public static float Smootherstep(float x, float start, float end) // Copy pasted from osu! pp. Thanks StanR
        {
            x = (float)Math.Clamp((x - start) / (end - start), 0.0, 1.0);

            return (float)(x * x * x * (x * (6.0 * x - 15.0) + 10.0));
        }

        public static float ClampedLerp(float start, float end, float scale)
        {
            scale = (float)Math.Clamp(scale, 0, 1);

            return start + scale * (end - start);
        }

        public Vector2 pos0, pos1, pos2, dir0, output;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}