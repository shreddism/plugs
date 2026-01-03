using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;    

/// Separate filter at the frequency of display hz that renders the image at a desired framerate.
/// d tells the graph tool to draw at frequency when the pen is being applied.
/// i polls for input at frequency and only draws when input is applied (mode change?).
/// Data is handled by UpdateTool.

namespace OGTRenderTool
{
    [PluginName("OGTRenderTool")]
    public class RenderTool : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public RenderTool() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        [BooleanProperty("Wire (Hover Over This)", ""), DefaultPropertyValue(true), ToolTip
        (
            "Set this to true and frequency to 0 to only render on tablet update.\n" +
            "Should probably set frequency to display refresh rate though, making setting this your decision."
        )]
        public bool Wire { get; set; }

        protected override void ConsumeState()
        {
            if (State is ITabletReport report)
            {
                raw1Pos = report.Position;
                if (Wire)
                    UpdateState();
            }
            else OnEmit();
        }

        protected override void UpdateState()
        {    
            if (State is ITabletReport report && PenIsInRange())
            {
                report.Position = raw1Pos;
                State = report;
                Console.WriteLine("d");
                OnEmit();
            }
            else Console.WriteLine("i");
        }

        Vector2 raw1Pos;

    }
}