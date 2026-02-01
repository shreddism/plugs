using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace printpressure
{
    [PluginName("printpressure")]
    public class printpressure : IPositionedPipelineElement<IDeviceReport>
    {
        public printpressure() : base()
        {
        }

       public PipelinePosition Position => PipelinePosition.PostTransform;

       public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                Console.WriteLine(report.Pressure);
            }
            Emit?.Invoke(value);
        }

    }
}