using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace Plugin251229
{
    [PluginName("Plugin251229")]
    public class Plugin251229 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Plugin251229() : base()
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


                bottom = 3 - alpha0;

                Console.WriteLine("bottom: " + bottom);

                

                startPos = outputPos0;
                UpdateState();
            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
            
            

            if (State is ITabletReport report && PenIsInRange())
            {

                updateDelta = (float)updateStopwatch.Elapsed.TotalMilliseconds;

                alpha1 = alpha0 - 2;

            if (consume) {
                alpha1 = 0;
                emergency = false;
             //Console.WriteLine("-- Consume");
                // Console.WriteLine(vel0);
            }

            alpha0 = 0.15f + 0.85f * MathF.Sqrt((float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg)) * endt;

                
                alpha0 += top;
                Console.WriteLine(top);


                if ((alpha0 > 1) && (top < 1)) {
                    top = alpha0 - 1;
                }
                else top = 0;
            
                
                alpha0 = Math.Clamp(alpha0, 0, 1);

                

                


                delta = alpha0 - alpha1;





            
                alpha0 += 2;

                if (consume) {
                    
                    Console.WriteLine(consume);
                    consume = false;
                }

                

                checkPos0 = pos2 + (alpha0 * tVel) + (0.5f * alpha0 * alpha0 * tAccel) + ((tAccel) - tAccel1) * (MathF.Max(MathF.Pow((alpha0 - 2), 3), 0));

                

                float pleasespeedineedthis = Vector2.Dot(Vector2.Normalize(checkPos0 - checkPos1), Vector2.Normalize(dir0));

                
                /* Console.WriteLine("---------------"); */
                if (pleasespeedineedthis > 0.66f) {

                    if (bottom > 0) {
                    checkPos0 = Vector2.Lerp(checkPos0, checkPos1, bottom);
                    bottom = 0;
                    }
                report.Position = checkPos0;
                /* Console.WriteLine(Vector2.Distance(checkPos0, checkPos1));
                Console.WriteLine(updateDelta);
                Console.WriteLine(Vector2.Distance(checkPos0, checkPos1) / updateDelta);  */
                
                checkPos1 = report.Position;
                }
                else {

                    CheckMoreDir();

                    if (gooddir) {
                        gooddir = false;
                        if (bottom > 0) {
                    checkPos0 = Vector2.Lerp(checkPos0, checkPos1, 0.5f * (bottom + (saveAlpha - alpha0)));
                    bottom *= 0.5f;
                    }
                        report.Position = checkPos0;             // Everything in the surrounding area is FUCKING stupid
                        checkPos1 = report.Position;
                    }
                    else {
                        /* Console.WriteLine("oops");
                    Console.WriteLine(alpha0); */
                    report.Position = checkPos1;
                    } 

                    

                    
                
                     
                /* if (!consume && alpha0 != 3) {           
                    Console.WriteLine("oops");
                    Console.WriteLine(alpha0);
                } */

                }
                /* Console.WriteLine(alpha0);
                Console.WriteLine("---------------"); */


               

                State = report;

                updateStopwatch.Restart();


                OnEmit();
            }
        }

        public void CheckMoreDir() {
             saveAlpha = alpha0;
            while (gooddir == false && saveAlpha < 3) {
                saveAlpha += 0.1f;

                checkPos0 = pos2 + (saveAlpha * tVel) + (0.5f * saveAlpha * saveAlpha * tAccel) + ((tAccel) - tAccel1) * (MathF.Max(MathF.Pow((saveAlpha - 2), 3), 0));

                float kindahomeless = Vector2.Dot(Vector2.Normalize(checkPos0 - checkPos1), Vector2.Normalize(dir0));

                if (kindahomeless > 0.66f) {
            
                gooddir = true;
                }
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


           // PrintStuff();
            

            predictedEndPos = pos2 + 3 * tVel + 0.5f * 3 * 3 * tAccel + ((tAccel) - tAccel1) * 1;



            endt = 0.5f + 0.5f * FSmoothstep(Math.Min(Math.Min(vel0, vel1), vel2), 0, 10);

            Console.WriteLine(endt);

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
        public float vel0, vel1, accel0, accel1, jerk0, vel2;
        public float alpha1, alpha0, delta, updateDelta;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private HPETDeltaStopwatch updateStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 300);
        bool consume, emergency;
        private float top, bottom;
        bool gooddir = false;
        Vector2 checkedPos;
        float endt;
        float saveAlpha;

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}