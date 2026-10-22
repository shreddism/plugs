using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace checkdirforcross
{
    [PluginName("checkdirforcross")]
    public class checkdirforcross : IPositionedPipelineElement<IDeviceReport>
    {
        public checkdirforcross() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        [Property("threshold"), DefaultPropertyValue(0f), ToolTip
        (
            ""
        )]
        public float testValue { 
            set => _testValue = Math.Clamp(value, 0f, 100f);
            get => _testValue;
        }
        public float _testValue;

        public void Consume(IDeviceReport value)
        {    
            if (value is ITabletReport report)
            {
                d = report.Position - p1;
                p1 = report.Position;
                if (d.Length() > testValue) {
                    Console.WriteLine(d);
                }
            }
            Emit?.Invoke(value);
        }
        Vector2 p1, d;

    }
}