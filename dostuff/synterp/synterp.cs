using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace async
{
    [PluginName("synterp")]
    public class FilterPlugin : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public FilterPlugin() : base()
        {
        }

        public HPETDeltaStopwatch stopwatch = new HPETDeltaStopwatch(true);

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        protected override void ConsumeState()
        {
            

            if ((State is ITabletReport report && PenIsInRange())) {

                if (!init) {
                    position = report.Position;
                    positionL = report.Position;
                    init = true;
                    OnEmit();
                    return;
                }

                float time = (float)stopwatch.Restart().TotalMilliseconds;
                reportMsAvg = 0.9f * reportMsAvg + 0.1f * time;
                positionL = position;
                position = report.Position;
                UpdateState();
            }
            else {
                OnEmit();
            }
        }

        protected override void UpdateState()
        {
            if ((State is ITabletReport report && PenIsInRange())) {
                if ((float)stopwatch.Elapsed.TotalMilliseconds >= 0.45f) {
                    report.Position = position;
                  //  Console.WriteLine("o");
                }
                else {
                    report.Position = Vector2.Lerp(position, positionL, 0.5f);
                   // Console.WriteLine("u");
                }
                OnEmit();
            }
        }

        Vector2 position, positionL;

        bool init;

        float reportMsAvg;
    }
}