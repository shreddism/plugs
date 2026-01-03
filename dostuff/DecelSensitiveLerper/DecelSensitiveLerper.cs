using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace DecelSensitiveLerper
{
    [PluginName("DecelSensitiveLerper")]
    public class DecelSensitiveLerper : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public DecelSensitiveLerper() : base()
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
                currPosition = vec2IsFinite(currPosition) ? currPosition : report.Position;
                currPosition = report.Position; 

                lastVelocity = velocity;
                velocity = (float)Math.Sqrt(Math.Pow(currPosition.X - lastPosition.X, 2) + Math.Pow(currPosition.Y - lastPosition.Y, 2));
                accel = velocity - lastVelocity;

                if (!(velocity == 0))
                pow = Math.Clamp(1 + (accel / velocity), 0, 1);
                else pow = 1;

          //  Console.WriteLine(pow);
            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
            float alpha = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg);

            if (State is ITabletReport report && PenIsInRange())
            {
                alpha = (float)Math.Clamp(alpha, 0, 1);
                report.Position = Vector2.Lerp(lastPosition, currPosition, (float)Math.Pow(alpha, pow));
                State = report;
                OnEmit();
            }
        }

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
        Vector2 currPosition, lastPosition;
        float velocity, lastVelocity, accel, pow;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 300);
    }
}