using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace printtime
{
    [PluginName("printtime")]
    public class printtime : IPositionedPipelineElement<IDeviceReport>
    {
        public printtime() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                float reportTime = (float)reportStopwatch.Restart().TotalMilliseconds;
                Console.WriteLine(reportTime);
            }
            Emit?.Invoke(value);
        }

        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
    }
}