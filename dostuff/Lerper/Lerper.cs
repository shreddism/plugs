using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace Lerper
{
    [PluginName("Lerper")]
    public class Lerper : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Lerper() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PostTransform;

        protected override void ConsumeState()  // Report
        {
            if (State is ITabletReport report)
            {
                var consumeDelta = (float)reportStopwatch.Restart().TotalMilliseconds;
                if (consumeDelta < 150)
                    reportMsAvg += ((consumeDelta - reportMsAvg) * 0.1f);
                    
                lastPosition = currPosition;
                currPosition = report.Position; 
            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
            float alpha = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg);

            if (State is ITabletReport report && PenIsInRange())
            {
                alpha = (float)Math.Clamp(alpha, 0, 1);
                report.Position = Vector2.Lerp(lastPosition, currPosition, alpha);
                State = report;
                OnEmit();
            }
        }

        Vector2 currPosition, lastPosition;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 300);
    }
}