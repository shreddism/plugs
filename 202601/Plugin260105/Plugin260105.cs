using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace Plugin260105
{
    [PluginName("Plugin260105")]
    public class Plugin260105 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Plugin260105() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        [Property("Limiter"), DefaultPropertyValue(2.5f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float opt1 { 
            set => _opt1 = (float)Math.Clamp(value, 2.5f, 3.0f);
            get => _opt1;
        }
        public float _opt1;

        [Property("Confidence 0"), DefaultPropertyValue(0f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float opt2 { 
            set => _opt2 = (float)Math.Clamp(value, 0.0f, 100.0f);
            get => _opt2;
        }
        public float _opt2;

        [Property("Confidence 1"), DefaultPropertyValue(0f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float opt3 { 
            set => _opt3 = (float)Math.Clamp(value, 0.0f, 100.0f);
            get => _opt3;
        }
        public float _opt3;

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

                bottom = -1 * Math.Max(alpha0 - opt1, 0);

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

                alpha1 = alpha0 - (opt1 - 1);

            if (consume) {
                alpha1 = 0;
                // Console.WriteLine("-- Consume");
                // Console.WriteLine(vel0);
                if ((alpha0PreservationSociety > 1) && (top < 1)) {
                    top = alpha0PreservationSociety - 1;
                    bottom = 0;
                }
                else top = 0;
            }
            else updatesSinceLastReport += 1;

            
                

            float ohmygodbruh = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg) * (1000 / Frequency);

            

                alpha0 = ((1 - top) * ohmygodbruh) + 1.120f * top;
            
                

                alpha0PreservationSociety = alpha0;

                

                

            
                alpha0 += (opt1 - 1);

                

                alpha0 = Math.Clamp(alpha0, (opt1 - 1), pathpreservationsociety);
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



                outputDir0 = Trajectory(dir0, dir1, dir2, alpha0, true, "t0 - ") / reportMsAvg;

                sdirt1 = Trajectory(a1dir0, a1dir1, a1dir2, alpha0 + 0.5f, true, "t1 - ") / reportMsAvg;

                outputDir0 = Vector2.Lerp(outputDir0, sdirt1, pps3);
                


                if (emergency < 0) {
                outputPos0 += outputDir0;
                outputPos0 = Vector2.Lerp(outputPos0, pos0 + dir0, 0.05f);
                outputPos0 = Vector2.Lerp(outputPos0, pos0, Math.Clamp(MathF.Pow(accel0 / -200, 2), 0, 0.25f));
                }
                

                //Console.WriteLine((Vector2.Distance(Vector2.Zero, thisshouldntexist) > 1000 + Vector2.Distance(Vector2.Zero, outputPos0)));

                if ((!vec2IsFinite(outputPos0)) || (emergency > 0) || (Vector2.Distance(Vector2.Zero, pos0) > 5000 + Vector2.Distance(Vector2.Zero, outputPos0))) {
                outputPos0 = report.Position;
                }

                report.Position = outputPos0;

                Plot();

            





                if (consume) {
                    consume = false;
                }

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


            dir5 = dir4;
            dir4 = dir3;
            dir3 = dir2;
            dir2 = dir1;
            dir1 = dir0;
            dir0 = pos0 - pos1;

            a1dir2 = (dir3 + dir2) / 2;
            a1dir1 = (dir2 + dir1) / 2;
            a1dir0 = (dir1 + dir0) / 2;
            
            vel2 = vel1;
            vel1 = vel0;
            vel0 = MathF.Sqrt(MathF.Pow(dir0.X, 2) + MathF.Pow(dir0.Y, 2));

            accel1 = accel0;
            accel0 = vel0 - vel1;

            jerk0 = accel0 - accel1;

            pathpreservationsociety = MathF.Min(MathF.Min(vel0, vel1), vel2);

            pathpreservationsociety = 2 + (opt1 - 2) * FSmoothstep(pathpreservationsociety, 0, 20);

            pps2Dir = (dir0 + dir1) - (dir2 + dir2);

            pps2 = 2 + (opt1 - 2) * 0.5f * FSmoothstep(pps2Dir.Length(), 0, 25) + (opt1 - 2) * 0.5f * FSmoothstep(pps2Dir.Length(), 50, 100);
            
            pathpreservationsociety = Math.Min(pathpreservationsociety, pps2);

            pps3 = FSmoothstep(dir3.Length() - dir0.Length(), -20, 0) - FSmoothstep(dir3.Length() - dir0.Length(), 0, 20);

          //  Console.WriteLine(pps3);


            //PrintStuff();
            
            estimationStart = Trajectory(dir0, dir1, dir2, opt1 - 1, false, "") / reportMsAvg;

            estimationEnd = Trajectory(dir0, dir1, dir2, opt1, false, "") / reportMsAvg;
            

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

                Console.Write("jx");

                Console.WriteLine(estimationStart.X);

                Console.Write("jy");

                Console.WriteLine(estimationStart.Y * -1);

                Console.Write("sx");

                Console.WriteLine(estimationEnd.X);

                Console.Write("sy");

                Console.WriteLine(estimationEnd.Y * -1);
 
                Console.WriteLine("xx");

                Console.WriteLine("dd");
        }

        public static float FSmoothstep(float x, float start, float end) // Copy pasted out of osu! pp. Thanks StanR 
        {
            x = (float)Math.Clamp((x - start) / (end - start), 0.0, 1.0);

            return x * x * (3 - 2 * x);
        }

        Vector2 Trajectory(Vector2 p0, Vector2 p1, Vector2 p2, float t, bool check, String info) {

            Vector2 tMid = 0.5f * (p0 + p2);

            if (consume && check) {
             //   Console.WriteLine(info + (Vector2.Distance(p1, tMid)));
            }

            Vector2 diff = tMid - p1;

            if (Vector2.Distance(p1, tMid) > 2) {
                p1 += 2 * Vector2.Normalize(diff);
            }
            else p1 = tMid;

         //   p1 = Vector2.Lerp(p1, tMid, FSmoothstep((Vector2.Distance(p1, tMid) / Vector2.Distance(p0, p2)), 0.1f, 0.33f));


            Vector2 tAccel = 2 * (tMid - p1);
            Vector2 tVel = (2 * p1) - p2 - tMid;

            

            return p2 + t * tVel + 0.5f * t * t * tAccel;

        }

        Vector2 pos0, pos1, dir0, dir1, outputDir0, outputPos0, outputPos1, distance, startPos, rememberOutputDir, rememberLastReportOutputDir, inheritance;
        Vector2 pos2;
        Vector2 a1dir0, a1dir1, a1dir2;
        Vector2 dir3, dir4, dir5;
        Vector2 predictedEndPos;
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
        float pathpreservationsociety, pps2;
        int emergency;
        Vector2 thisshouldntexist;
        Vector2 estimationStart, estimationEnd;
        Vector2 pps2Dir;
        Vector2 xdddzs;
        Vector2 sdirt1;
        float pps3;

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}