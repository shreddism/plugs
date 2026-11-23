using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace sync
{
    [PluginName("syncfilter")]
    public class FilterPlugin : IPositionedPipelineElement<IDeviceReport>
    {
        public FilterPlugin() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        [Property("option1"), DefaultPropertyValue(0f), ToolTip(
            "option1"
        )]
        public float opt1 { 
            set => _opt1 = value;
            get => _opt1;
        }
        public float _opt1;

        public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {

            if (value is ITabletReport report)
            {
            }
            Emit?.Invoke(value);
        }
    }
}