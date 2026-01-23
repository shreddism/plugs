using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace ring
{
    [PluginName("ring")]
    public class ring : IPositionedPipelineElement<IDeviceReport>
    {
        public ring() : base()
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
                vel1 = vel0;
                vel0 = dir0.Length();
                accel0 = vel0 - vel1;
                Vector2 dist = pos0 - ringPos0;
                float scale = 1;

                ringPos1 = ringPos0;
                ringPos0 += MathF.Max(0, MathF.Max(dist.Length() - 100, 0.0f * (dist.Length() - 500))) * Vector2.Normalize(dist);

                ringDir = ringPos0 - ringPos1;

                
                

                outputPos0 += ringDir;

                if (ringDir.Length() > 0)
                outputPos0 = capDist(outputPos0, Vector2.Lerp(outputPos0, pos0, 0.5f), 5);

                
                

               
                 //   outputPos0 = Vector2.Lerp(outputPos0, pos0, 0.1f * FSmootherstep(vel0 + accel0, 10, 0));
                

               

              //  Console.WriteLine(pos0 - outputPos0);

                if (!vec2IsFinite(outputPos0) && (pos0 - pos1 != Vector2.Zero)) {
                    outputPos0 = pos0;
                    ringPos0 = pos0;
                }
                
                report.Position = outputPos0;   

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

        Vector2 pos0, pos1, ringPos0, ringPos1, ringDir, outputPos0, outputPos1, outputDir;

        Vector2 dir0;
        float vel0, vel1, accel0;

        Vector2 MaxLength(Vector2 a, Vector2 b) => a.Length() >= b.Length() ? a : b;

        Vector2 MinLength(Vector2 a, Vector2 b) => a.Length() <= b.Length() ? a : b;

        Vector2 MaxDist(Vector2 p, Vector2 a, Vector2 b) => Vector2.Distance(p, a) >= Vector2.Distance(p, b) ? a : b;

        Vector2 MinDist(Vector2 p, Vector2 a, Vector2 b) => Vector2.Distance(p, a) <= Vector2.Distance(p, b) ? a : b;

        Vector2 capDist(Vector2 a, Vector2 b, float d) => a + MathF.Min(Vector2.Distance(b, a), d) * (vec2IsFinite(Vector2.Normalize(b - a)) ? Vector2.Normalize(b - a) : Vector2.Zero); 

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}