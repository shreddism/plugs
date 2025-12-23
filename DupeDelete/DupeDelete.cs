using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace DupeDelete
{
    [PluginName("DupeDelete")]
    public class DupeDelete : IPositionedPipelineElement<IDeviceReport>
    {
        public DupeDelete() : base()
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

            }
            Emit?.Invoke(value);
        }

        
        public Vector2 pos0, pos1, outputPos0, outputPos1, dir0, dir1, disc, outputVel0;
        public float dist, scale, compensation, index, realscale;
        public double vel0, vel1, accel0;
        
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}