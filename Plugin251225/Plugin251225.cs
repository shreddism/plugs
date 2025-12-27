using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace Plugin251225
{
    [PluginName("EvilResampler")]
    public class EvilResampler : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public EvilResampler() : base()
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

                rememberLastReportOutputDir = (outputPos0 - outputPos1) / delta;

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
                Console.WriteLine("-- Consume");
                Console.WriteLine(vel0);
            }

            alpha0 = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg);
            delta = alpha0 - alpha1;

            if (State is ITabletReport report && PenIsInRange())
            {
                // alpha0 = (float)Math.Clamp(alpha0, 0, 1);

            //    Console.WriteLine(alpha0);

                distance = pos0 - startPos;

            inheritance = 2 *(alpha0 - (alpha0 * alpha0)) * MathF.Cbrt(1 - Math.Abs(Vector2.Dot(Vector2.Normalize(rememberLastReportOutputDir), Vector2.Normalize(distance)))) * rememberLastReportOutputDir;

                outputPos1 = outputPos0;
                outputPos0 = Vector2.Lerp(startPos, pos0, alpha0) + (inheritance * delta);

               

                if ((Vector2.Distance(outputPos0, outputPos1) == 0) && !(vel0 == 0)) {
                    Console.WriteLine(delta);
                    outputPos0 += rememberOutputDir * delta;
                }
                else rememberOutputDir = (outputPos0 - outputPos1) / delta;

                if (!vec2IsFinite(outputPos0)) {
                    emergency = true;
                }
                else report.Position = outputPos0;


                if (emergency) {
                    outputPos0 = pos0;
                    outputDir0 = Vector2.Zero;
                    report.Position = pos0;
                    emergency = false;
                }

                Console.WriteLine(Vector2.Distance(outputPos0, outputPos1) / delta);

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
        }



        Vector2 pos0, pos1, dir0, dir1, outputDir0, outputPos0, outputPos1, distance, startPos, rememberOutputDir, rememberLastReportOutputDir, inheritance;
        public float vel0, vel1, accel0, accel1, jerk0;
        public float alpha1, alpha0, delta;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 300);
        bool consume, emergency;

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}