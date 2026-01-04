using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace Plugin260103
{
    [PluginName("Plugin260103")]
    public class Plugin260103 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Plugin260103() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        protected override void ConsumeState()  // Report
        {
            if (State is ITabletReport report)
            {
                var consumeDelta = (float)reportStopwatch.Restart().TotalMilliseconds;
                if (consumeDelta < 50) {
                    reportMsAvg += ((consumeDelta - reportMsAvg) * 0.1f);
                    emergency--;
                }

                else emergency = 5;

                    consume = true;
                    
                StatUpdate(report);

                bottom = -1 * Math.Max(alpha0 - 2.5f, 0);

                if (top > 0.75f || bottom > 0.75f) {
                    top = 0;
                    bottom = 0;
                }

                updatesSinceLastReport = 0;
                

                startPos = outputPos0;
                //UpdateState();
            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
            

            if (State is ITabletReport report && PenIsInRange())
            {

                alpha1 = alpha0 - 1.5f;

            if (consume) {
                alpha1 = 0;
                // Console.WriteLine("-- Consume");
                // Console.WriteLine(vel0);
                if ((alpha0PreservationSociety > 1) && (top < 1)) {
                    top = alpha0PreservationSociety - 1;
                    bottom = 0;
                }
                else top = 0;
                consume = false;
            }
            else updatesSinceLastReport += 1;

            
                

            float ohmygodbruh = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg) * (1000 / Frequency);

            

                alpha0 = ((1 - top) * ohmygodbruh) + 1.120f * top;
            
                

                alpha0PreservationSociety = alpha0;

                

                

            
                alpha0 += 1.5f;

                

                alpha0 = Math.Clamp(alpha0, 1.5f, pathpreservationsociety);
           //     Console.WriteLine(alpha0);

              
                 
                /* checkPos0 = pos2 + (alpha0 * tVel) + (0.5f * alpha0 * alpha0 * tAccel) - ((tAccel) - tAccel1) * (MathF.Max(MathF.Pow((alpha0 - 2), 3), 0));
                
                float pleasespeedineedthis = Vector2.Dot(Vector2.Normalize(checkPos0 - checkPos1), Vector2.Normalize(dir0));

           //    Console.WriteLine(top + "   " + bottom);

                if (pleasespeedineedthis > 0) {
                    if (bottom > 0) {
                    checkPos0 = Vector2.Lerp(checkPos0, checkPos1, bottom);
                    bottom *= 0.5f;
                    }
                report.Position = checkPos0;
                checkPos1 = report.Position;
                }
                else {
                    report.Position = checkPos1;
                    Console.WriteLine("oops");
                } */

                thisshouldntexist = outputPos0;



                outputDir0 = dir2 + (alpha0 * tVel) + (0.5f * alpha0 * alpha0 * tAccel);

                
                outputDir0 /= reportMsAvg;


                if (emergency < 0) {
                outputPos0 += outputDir0;
                outputPos0 = Vector2.Lerp(outputPos0, pos0 + dir0, 0.05f);
                }
                

                //Console.WriteLine((Vector2.Distance(Vector2.Zero, thisshouldntexist) > 1000 + Vector2.Distance(Vector2.Zero, outputPos0)));

                if ((!vec2IsFinite(outputPos0)) || (emergency > 0) || (Vector2.Distance(Vector2.Zero, pos0) > 5000 + Vector2.Distance(Vector2.Zero, outputPos0))) {
                outputPos0 = report.Position;
                }

                report.Position = outputPos0;

                Plot();

            







              //  Console.WriteLine(alpha0);

                State = report;
                OnEmit();
            }
        }



        public void StatUpdate(ITabletReport report) {

            pos2 = pos1;
            pos1 = pos0;
            pos0 = report.Position;

           // Console.WriteLine(pos0 - predictedEndPos);

            // apply temporal resampler trajectory to velocity (?);

            dir2 = dir1;
            dir1 = dir0;
            dir0 = pos0 - pos1;



            tMid = 0.5f * (dir0 + dir2);
            tAccel = 2 * (tMid - dir1);
            tVel = (2 * dir1) - dir2 - tMid;

            

            vel2 = vel1;
            vel1 = vel0;
            vel0 = MathF.Sqrt(MathF.Pow(dir0.X, 2) + MathF.Pow(dir0.Y, 2));

            accel1 = accel0;
            accel0 = vel0 - vel1;

            jerk0 = accel0 - accel1;

            pathpreservationsociety = MathF.Min(MathF.Min(vel0, vel1), vel2);

            pathpreservationsociety = 2 + 0.5f * FSmoothstep(pathpreservationsociety, 0, 20);

            //PrintStuff();
            

            predictedEndPos = dir2 + 3 * tVel + 0.5f * 3 * 3 * tAccel;

           // Console.WriteLine(vel0);


        }

        void PrintStuff() {
            Console.WriteLine(" Start -------------------");
            Console.WriteLine(pos0 - predictedEndPos);
            Console.WriteLine(dir0);
            Console.WriteLine(accel0);




            Console.WriteLine(" End ---------------------");
        }

        void Plot() {

                Console.Write("vx");

                Console.WriteLine(outputDir0.X);

                Console.Write("vy");

                Console.WriteLine(outputDir0.Y * -1);

                Console.Write("ax");

                Console.WriteLine(dir0.X / reportMsAvg);

                Console.Write("ay");

                Console.WriteLine(dir0.Y / -reportMsAvg);

                Console.WriteLine("xx");

                Console.WriteLine("dd");
        }

        public static float FSmoothstep(float x, float start, float end) // Copy pasted out of osu! pp. Thanks StanR 
        {
            x = (float)Math.Clamp((x - start) / (end - start), 0.0, 1.0);

            return x * x * (3 - 2 * x);
        }



        Vector2 pos0, pos1, dir0, dir1, outputDir0, outputPos0, outputPos1, distance, startPos, rememberOutputDir, rememberLastReportOutputDir, inheritance;
        Vector2 pos2;
        Vector2 predictedEndPos;
        Vector2 tMid, tAccel, tVel;
        Vector2 checkPos0, checkPos1;
        Vector2 dir2;
        public float vel0, vel1, vel2, accel0, accel1, jerk0;
        public float alpha1, alpha0, delta;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 305);
        bool consume;
        float bottom, top, saveTop;
        float alpha0PreservationSociety;
        int updatesSinceLastReport;
        float updateDelta, pathDelta;
        float pathpreservationsociety;
        int emergency;
        Vector2 thisshouldntexist;

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}