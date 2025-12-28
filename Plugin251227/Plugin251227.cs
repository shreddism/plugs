using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace Plugin251227
{
    [PluginName("ActualEvilResampler")]
    public class ActualEvilResampler : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public ActualEvilResampler() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

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

                

                startPos = outputPos0;
               // UpdateState();
            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
            alpha1 = alpha0;

            if (consume) {
                alpha1 = 0;
                consume = false;
                emergency = false;
                // Console.WriteLine("-- Consume");
                // Console.WriteLine(vel0);
            }

            alpha0 = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg);
            delta = alpha0 - alpha1;

            if (State is ITabletReport report && PenIsInRange())
            {
            
                alpha0 += 2;

                alpha0 = Math.Clamp(alpha0, 0, 4);

                outputPos0 = pos2 + alpha0 * tVel + 0.5f * alpha0 * alpha0 * tAccel;

                report.Position = outputPos0;

                Console.WriteLine(alpha0);

                State = report;
                OnEmit();
            }
        }

        public void StatUpdate(ITabletReport report) {

            pos2 = pos1;
            pos1 = pos0;
            pos0 = report.Position;

            // temporal resampler trajectory

            tMid = 0.5f * (pos0 + pos2);
            tAccel = 2 * (tMid - pos1);
            tVel = (2 * pos1) - pos2 - tMid;

            dir1 = dir0;
            dir0 = pos0 - pos1;

            vel1 = vel0;
            vel0 = MathF.Sqrt(MathF.Pow(dir0.X, 2) + MathF.Pow(dir0.Y, 2));

            accel1 = accel0;
            accel0 = vel0 - vel1;

            jerk0 = accel0 - accel1;
        }



        Vector2 pos0, pos1, dir0, dir1, outputDir0, outputPos0, outputPos1, distance, startPos, rememberOutputDir, rememberLastReportOutputDir, inheritance;
        Vector2 pos2;
        Vector2 tMid, tAccel, tVel;
        public float vel0, vel1, accel0, accel1, jerk0;
        public float alpha1, alpha0, delta;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 300);
        bool consume, emergency;

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}