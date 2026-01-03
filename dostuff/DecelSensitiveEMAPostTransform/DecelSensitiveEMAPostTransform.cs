using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace DSEMA
{
    [PluginName("DecelSensitiveEMAPostTransform")]
    public class DecelSensitiveEMAPostTransform : IPositionedPipelineElement<IDeviceReport>
    {
        public DecelSensitiveEMAPostTransform() : base()
        {
        }

       public PipelinePosition Position => PipelinePosition.PostTransform;

        [Property("Normal Weight"), DefaultPropertyValue(0.9f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float normalWeight { get; set; }

        [Property("Decel Weight"), DefaultPropertyValue(1f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float decelWeight { get; set; }


       public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                raw3Pos = raw2Pos;
                raw2Pos = raw1Pos;
                raw1Pos = report.Position;
                velocity = (float)Math.Sqrt(Math.Pow(raw1Pos.X - raw2Pos.X, 2) + Math.Pow(raw1Pos.Y - raw2Pos.Y, 2));
                if (velocity == 0)
                accel = 0;
                else accel = velocity - (float)Math.Sqrt(Math.Pow(raw2Pos.X - raw3Pos.X, 2) + Math.Pow(raw2Pos.Y - raw3Pos.Y, 2));
                emaWeight = ClampedLerp(decelWeight, normalWeight, Smootherstep(accel / velocity, -1f, 0));
                calc1Pos = vec2IsFinite(calc1Pos) ? calc1Pos : report.Position;
                calc1Pos += emaWeight * (report.Position - calc1Pos);
                calc1Pos = vec2IsFinite(calc1Pos) ? calc1Pos : report.Position;
                if (velocity != 0)
                report.Position = calc1Pos;
                Console.WriteLine(velocity);
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

        public float emaWeight, velocity, accel;
        public Vector2 raw3Pos, raw2Pos, raw1Pos, calc2Pos, calc1Pos;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}