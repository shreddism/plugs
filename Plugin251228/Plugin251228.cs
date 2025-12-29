using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace Plugin251228
{
    [PluginName("v0.0.1")]
    public class Plugin251228 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Plugin251228() : base()
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
                UpdateARFReports(report.Position);

                

                startPos = outputPos0;
                UpdateState();
            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
            alpha1 = alpha0;

            if (consume) {
                alpha1 = 0;
                emergency = false;
                // Console.WriteLine("-- Consume");
                // Console.WriteLine(vel0);
            }

            alpha0 = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg);
            delta = alpha0 - alpha1;

            if (State is ITabletReport report && PenIsInRange())
            {
            
                alpha0 += 2;

                alpha0 = Math.Clamp(alpha0, 2, 3);

                checkPos0 = pos2 + (alpha0 * tVel) + (0.5f * alpha0 * alpha0 * tAccel) + ((tAccel) - tAccel1) * (MathF.Max(MathF.Pow((alpha0 - 2), 3), 0));

                float pleasespeedineedthis = Vector2.Dot(Vector2.Normalize(checkPos0 - checkPos1), Vector2.Normalize(dir0));

                Console.WriteLine(pleasespeedineedthis);

                if (pleasespeedineedthis > 0.9f) {
                outputPos0 = checkPos0;
                checkPos1 = outputPos0;
                }
                else outputPos0 = checkPos1;

                outputPos0 = FakeAdaptiveRadialFollow(outputPos0);

                storelastPrediction = outputPos0;

                report.Position = outputPos0;

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

            vel1 = vel0;
            vel0 = MathF.Sqrt(MathF.Pow(dir0.X, 2) + MathF.Pow(dir0.Y, 2));

            accel1 = accel0;
            accel0 = vel0 - vel1;

            jerk0 = accel0 - accel1;


            PrintStuff();
            

            predictedEndPos = pos2 + 3 * tVel + 0.5f * 3 * 3 * tAccel + ((tAccel) - tAccel1) * 1;


        }

        void PrintStuff() {
            Console.WriteLine(" Start -------------------");
            Console.WriteLine(pos0 - predictedEndPos);
            Console.WriteLine(dir0);
            Console.WriteLine(accel0);




            Console.WriteLine(" End ---------------------");
        }



        Vector2 pos0, pos1, dir0, dir1, outputDir0, outputPos0, outputPos1, distance, startPos, rememberOutputDir, rememberLastReportOutputDir, inheritance;
        Vector2 pos2;
        Vector2 predictedEndPos;
        Vector2 tMid, tAccel, tAccel1, tVel;
        Vector2 checkPos0, checkPos1;
        public float vel0, vel1, accel0, accel1, jerk0;
        public float alpha1, alpha0, delta;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 300);
        bool consume, emergency;


        public double arf_lastVel, arf_vel, arf_lastAccel, arf_accel, arf_lastLastJerk, arf_lastJerk, arf_jerk, arf_lastLastSnap, arf_lastSnap, arf_snap, lastIndex, index, lastChange, change;
        public double ra1_lastAccel, ra1_accel, ra1a_ra1_lastJerk, ra1a_ra1_jerk, ra1a_ra1j_ra1_snap;
        public double DecellingThreshold, LPICAS;
        public Vector2 arf_lastReport, arf_currReport, arf_diff, hold, storelastPrediction; 
        public int state;
        public bool StateEvaluator = false;
        public double combinationChange;

        void UpdateARFReports(Vector2 position) {
            arf_lastReport = arf_currReport;
            arf_currReport = position;

            arf_diff = arf_currReport - arf_lastReport;

            arf_lastVel = arf_vel;
            arf_vel =  ((Math.Sqrt(Math.Pow(arf_diff.X, 2) + Math.Pow(arf_diff.Y, 2)) / 1) / 3);

            arf_lastAccel = arf_accel;
            arf_accel = arf_vel - arf_lastVel;
            ra1_lastAccel = ra1_accel;
            ra1_accel = arf_lastAccel + arf_accel;

            arf_lastLastJerk = arf_lastJerk;
            arf_lastJerk = arf_jerk;
            arf_jerk = arf_accel - arf_lastAccel;
            ra1a_ra1_lastJerk = ra1a_ra1_jerk;
            ra1a_ra1_jerk = ra1_accel - ra1_lastAccel;
            

            arf_lastLastSnap = arf_lastSnap;
            arf_lastSnap = arf_snap;
            arf_snap = arf_jerk - arf_lastJerk;
            ra1a_ra1j_ra1_snap = ra1a_ra1_jerk;

            lastIndex = index;
            index = (arf_jerk + arf_lastJerk + arf_accel + arf_lastAccel) / (Math.Log((Math.Pow(arf_lastVel - ra1_lastAccel, 1.2) + Math.E) / Math.E + 1) + 1);


            lastChange = change;
            change = index + lastIndex;

            combinationChange = (change + index) - (lastChange  + lastIndex);

            StateEvaluator = true;

            /* Console.WriteLine("-----------------------");
            Console.WriteLine(arf_vel);
            Console.WriteLine(arf_accel);
            Console.WriteLine(arf_jerk);
            Console.WriteLine(index);
            Console.WriteLine("-----------------------"); */

   /*         Console.Write("v");   
                Console.WriteLine(arf_vel);
                Console.Write("a");
                Console.WriteLine(arf_accel);
                Console.Write("j");
                Console.WriteLine(arf_jerk);
                Console.Write("s");
                Console.WriteLine(ra1a_ra1j_ra1_snap);
                Console.WriteLine("x");
                Console.WriteLine("d"); */

              //  Console.WriteLine(ra1a_ra1j_ra1_snap - arf_accel);

        }

        public Vector2 FakeAdaptiveRadialFollow(Vector2 position) {
            
            if (StateEvaluator) {
                StateEvaluator = false;
                if ((
                    (
                     (((ra1_accel < 0) | (arf_lastVel < 10)) && (arf_accel  / (Math.Log((arf_lastVel + Math.E * 10) / 10 + 1) + 1) > 1) && ((ra1a_ra1_jerk) / (Math.Log((arf_lastVel + Math.E * 10) / 10 + 1) + 1) > 3)) |
                     (((ra1_accel) / (Math.Log((arf_lastVel + Math.E) / 1 + 1) + 1) > 1.25) && ((ra1a_ra1_jerk) / (Math.Log((arf_lastVel + Math.E) / 1 + 1) + 1) > 1.25) |
                     (index + change > 5)) |
                     (combinationChange > 3)
                    )
                && (index + change > 2)
                && (ra1a_ra1j_ra1_snap + index + change > 4)
                ) 
                )
                {
                    if (state == 0) {
                        hold = storelastPrediction;
                        state = 1;
                        
                    }
                    position = hold;
                }
                else if (state > 0) {
                    if (arf_accel > 0) {
                        if ((index + change > 10) && ((state == 1) | (state == 2))) {
                            state = 2;
                            position = hold;
                            
                        }
                        else {
                            if ((ra1_accel / (Math.Log((arf_lastVel + Math.E) + 1) + 1) > 2) | arf_vel > 100) {
                                position = hold;

                            }
                            else state = 0;
                        }
                    }
                    else if ((arf_jerk < 0) && (arf_vel > 100) && (ra1a_ra1j_ra1_snap - DecellingThreshold * ra1_accel) < 0) {
                        state = 3;
                        
                        position = hold;
                    
                    }
                    else {
                        if ((arf_vel > 10) && ((state == 3) | (state == 4)) && (arf_jerk < 0)) {
                            state = 4;
                           position = (float)LPICAS * hold + (1 - (float)LPICAS) * outputPos0;
                           hold = position;
                        }
                        else state = 0;
                    }

                }

                /* if (state == 0) {
                    Console.WriteLine("vr" + 255);
                    Console.WriteLine("ar" + 0);
                    Console.WriteLine("jr" + 0);
                    Console.WriteLine("sr" + 0);
                    Console.WriteLine("sb" + (int)Math.Clamp((arf_accel) / (Math.Log(arf_lastVel / 200 + 1) + 1) * 255, 0, 255));
                    Console.WriteLine("jb" + (int)Math.Clamp((arf_jerk) / (Math.Log(arf_lastVel / 20 + 1) + 1) * 255, 0, 255));
                    Console.WriteLine("vg" + 0);
                }
                else if (state == 1) {
                    Console.WriteLine("vr" + 0);
                    Console.WriteLine("ar" + 255);
                    Console.WriteLine("jr" + 0);
                    Console.WriteLine("sr" + 0);
                    Console.WriteLine("sb" + 0);
                    Console.WriteLine("jb" + 0);
                    Console.WriteLine("vg" + 0);
                }
                else if (state == 2) {
                    Console.WriteLine("vr" + 0);
                    Console.WriteLine("ar" + 0);
                    Console.WriteLine("jr" + 255);
                    Console.WriteLine("sr" + 0);
                    Console.WriteLine("sb" + 0);
                    Console.WriteLine("jb" + 0);
                    Console.WriteLine("vg" + 0);
                }
                else if (state == 3) {
                    Console.WriteLine("vr" + 0);
                    Console.WriteLine("ar" + 0);
                    Console.WriteLine("jr" + 0);
                    Console.WriteLine("sr" + 255);
                    Console.WriteLine("sb" + 0);
                    Console.WriteLine("jb" + 0);
                    Console.WriteLine("vg" + 255);
                }
                Console.WriteLine("xx");
                Console.WriteLine("ii"); */

           //      Console.WriteLine((index + change) + "         " + state);

            }
            else {
                if (state > 0) {
                    position = hold;
                }
                else if (state == 4) {
                position = (float)LPICAS * hold + (1 - (float)LPICAS) * outputPos0;
                hold = position;
                }

            }

            /* Console.WriteLine("---" + state);   */

            return position;


        }

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}