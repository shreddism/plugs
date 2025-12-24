using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace Plugin251223
{
    [PluginName("Plugin251223")]
    public class Plugin251223 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Plugin251223() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PostTransform;

        protected override void ConsumeState()  // Report
        {
            if (State is ITabletReport report)
            {
                var consumeDelta = (float)reportStopwatch.Restart().TotalMilliseconds;
                if (consumeDelta < 50)
                    reportMsAvg += ((consumeDelta - reportMsAvg) * 0.1f);
                else emergency = true;

                    consume = true;
                    
                StatUpdate(report);
                UpdateState();
            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
            alpha1 = alpha0;

            if (consume) {
                alpha1 = 0;
                consume = false;
            }

            alpha0 = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg);
            delta = alpha0 - alpha1;

            if (State is ITabletReport report && PenIsInRange())
            {
                alpha0 = (float)Math.Clamp(alpha0, 0, 1);

                distance = dir0;

                rsVelU = (MathF.Pow(alpha0, 2) * rsVel0) + ((1 - MathF.Pow(alpha0, 2)) * rsVel1);

                outputDir0 = Vector2.Multiply(Vector2.Normalize(distance), rsVelU * (delta));

                

                if (!vec2IsFinite(outputDir0)) {
                    outputDir0 = dir0;
                }

                outputPos0 = outputPos0 + outputDir0;

              //  outputPos0 = Vector2.Lerp(outputPos0, pos0, 0.05f);

                report.Position = outputPos0;

                Console.WriteLine(outputPos0 - pos0);


                if (emergency) {
                    outputPos0 = pos0;
                    outputDir0 = Vector2.Zero;
                    report.Position = pos0;
                    rsVel0 = 0;
                    rsVel1 = 0;
                    emergency = false;
                }


                State = report;
                OnEmit();
            }
        }

        public void StatUpdate(ITabletReport report) {

            pos1 = pos0;
            pos0 = report.Position;

            dir1 = dir0;
            dir0 = pos0 - pos1;

            vel1 = vel0;
            vel0 = MathF.Sqrt(MathF.Pow(dir0.X, 2) + MathF.Pow(dir0.Y, 2));

            accel1 = accel0;
            accel0 = vel0 - vel1;

            jerk0 = accel0 - accel1;

            rsAccel1 = accel1 + 0.5f * jerk0;
            rsAccel0 = accel0 + 0.5f * jerk0;

            rsVel1 = vel1 + 0.5f * rsAccel1;
            rsVel0 = vel0 + 0.5f * rsAccel0;
        }



        Vector2 pos0, pos1, dir0, dir1, outputDir0, outputPos0, outputPos1, distance;
        public float vel0, vel1, accel0, accel1, jerk0;
        public float alpha1, alpha0, delta;
        public float rsAccel0, rsAccel1, rsVel0, rsVel1, rsVelU;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 300);
        bool consume, emergency;

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}