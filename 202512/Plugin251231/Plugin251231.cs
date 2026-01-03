using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace Plugin251231
{
    [PluginName("Plugin251231")]
    public class Plugin251231 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Plugin251231() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        [BooleanProperty("Hover Over The Checkbox", ""), DefaultPropertyValue(false), ToolTip
        (
            "Lerps to usual output position by 0 (smoothstep transition) if acceleration is high and velocity is low.\n" +
            "If you're not understanding all that, it gives a subtle 'adaptive radial follow'-like effect."
        )]
        public bool toggle1 {get; set;}
        
            

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

                bottom = -1 * Math.Max(alpha0 - 3, 0);

                if (top > 0.75f || bottom > 0.75f) {
                    top = 0;
                    bottom = 0;
                }

                updatesSinceLastReport = 0;
                

                startPos = outputPos0;
                UpdateState();
            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
            

            if (State is ITabletReport report && PenIsInRange())
            {

                alpha1 = alpha0 - 2;

            if (consume) {
                alpha1 = 0;
                emergency = false;
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

            

                alpha0 = ((1 - top) * ohmygodbruh) + 1.120f *top;
            
                

                alpha0PreservationSociety = alpha0;

                

                

            
                alpha0 += 2;

                

                alpha0 = Math.Clamp(alpha0, 2, pathpreservationsociety);
             //   Console.WriteLine(alpha0);

              
                 
                checkPos0 = pos2 + (alpha0 * tVel) + (0.5f * alpha0 * alpha0 * tAccel) - ((tAccel) - tAccel1) * (MathF.Max(MathF.Pow((alpha0 - 2), 3), 0));
                
                float pleasespeedineedthis = Vector2.Dot(Vector2.Normalize(checkPos0 - checkPos1), Vector2.Normalize(dir0));

           //    Console.WriteLine(top + "   " + bottom);

                if (pleasespeedineedthis > 0.5f) {
                    if (bottom > 0) {
                        checkPos0 = Vector2.Lerp(checkPos0, checkPos1, bottom);
                        bottom *= 0.5f;
                    }

                    if (toggle1)
                    actualoutputposlol = Vector2.Lerp(actualoutputposlol, checkPos0, allsoulsperished);

                    else actualoutputposlol = checkPos0;

                  //  Console.WriteLine(allsoulsperished);
                    report.Position = actualoutputposlol;
                    checkPos1 = actualoutputposlol;
                }
                else {
                    report.Position = checkPos1;
                   actualoutputposlol = report.Position;

                   // Console.WriteLine("oops");
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

            // temporal resampler trajectory

            tMid = 0.5f * (pos0 + pos2);
            tAccel1 = tAccel;
            tAccel = 2 * (tMid - pos1);
            tVel = (2 * pos1) - pos2 - tMid;

            dir1 = dir0;
            dir0 = pos0 - pos1;

            vel2 = vel1;
            vel1 = vel0;
            vel0 = MathF.Sqrt(MathF.Pow(dir0.X, 2) + MathF.Pow(dir0.Y, 2));

            accel1 = accel0;
            accel0 = vel0 - vel1;

            jerk0 = accel0 - accel1;

            pathpreservationsociety = MathF.Min(MathF.Min(vel0, vel1), vel2);

            pathpreservationsociety = 2.5f + 0.5f * FSmoothstep(pathpreservationsociety, 0, 40) + FSmoothstep(pathpreservationsociety, 40, 100);

            //PrintStuff();

            allsoulsperished = FSmoothstep(accel0 / (vel0 != 0 ? vel0 : 1), 0.2f, 0.05f);
            /* Console.WriteLine(allsoulsperished);
            Console.WriteLine(accel0); */
            

            predictedEndPos = pos2 + 3 * tVel + 0.5f * 3 * 3 * tAccel + ((tAccel) - tAccel1) * 1;

           // Console.WriteLine(vel0);


        }

        void PrintStuff() {
            Console.WriteLine(" Start -------------------");
            Console.WriteLine(pos0 - predictedEndPos);
            Console.WriteLine(dir0);
            Console.WriteLine(accel0);




            Console.WriteLine(" End ---------------------");
        }

        public static float FSmoothstep(float x, float start, float end) // Copy pasted out of osu! pp. Thanks StanR 
        {
            x = (float)Math.Clamp((x - start) / (end - start), 0.0, 1.0);

            return x * x * (3 - 2 * x);
        }



        Vector2 pos0, pos1, dir0, dir1, outputDir0, outputPos0, outputPos1, distance, startPos, rememberOutputDir, rememberLastReportOutputDir, inheritance;
        Vector2 pos2;
        Vector2 predictedEndPos;
        Vector2 tMid, tAccel, tAccel1, tVel;
        Vector2 checkPos0, checkPos1;
        public float vel0, vel1, vel2, accel0, accel1, jerk0;
        public float alpha1, alpha0, delta;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 305);
        bool consume, emergency;
        float bottom, top, saveTop;
        float alpha0PreservationSociety;
        int updatesSinceLastReport;
        float updateDelta, pathDelta;
        float pathpreservationsociety;
        Vector2 actualoutputposlol;
        float allsoulsperished;

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}