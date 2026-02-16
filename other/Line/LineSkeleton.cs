using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace LineSkeleton
{
    [PluginName("LineSkeleton")]
    public class LineSkeleton : IPositionedPipelineElement<IDeviceReport>
    {
        public LineSkeleton() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        const int TEST_SIZE = 32;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                pos2 = pos1;
                pos1 = pos0;
                pos0 = report.Position;
                line0 = new Line(pos2, pos0, 2);
                line0.Step(1);
                tp = line0.FullDistanceToPoint(pos1);
                Plot();
            }
            else if (value is IAuxReport auxReport) {
                if (auxReport.Raw[4] == 127) {
                    perfStopwatch.Restart();
                    for (int t = 0; t < 50000; t++) {
                        Vector2 tPoint = new Vector2(aaar.Next(-1000, 1000), aaar.Next(-1000, 1000));
                        Vector2[] test = new Vector2[TEST_SIZE];
                        for (int i = 0; i < TEST_SIZE; i++) {
                            test[i].X = MathF.Pow(i, aaar.Next(0, 3)) * aaar.Next(-1, 1);
                            test[i].Y = MathF.Pow(aaar.Next(0, 3), i) * aaar.Next(-1, 1);
                        }
                        Line[] testlines = new Line[TEST_SIZE];
                        Vector2 xd = Vector2.Zero;
                        for (int i = 0; i < TEST_SIZE; i++) {
                            testlines[i] = new Line(test[i], Vector2.Zero, i);
                            xd += testlines[i].SegmentPerpendicularDistanceAIL(tPoint, (float)(aaar.Next(1, 9) / 20), (float)(aaar.Next(11, 19) / 20));
                        }


                      /*
                        Vector2[] test = new Vector2[TEST_SIZE];
                        float xd = 0;
                        for (int i = 0; i < TEST_SIZE; i++) {
                            test[i].X = MathF.Pow(i, aaar.Next(0, 3));
                            test[i].Y = MathF.Pow(aaar.Next(0, 3), i);
                        }
                        for (int i = 1; i < TEST_SIZE - 1; i++) {
                            xd += Trajectory(test[i - 1], test[i], test[i + 1], (float)(aaar.Next(1, 9) / 5)).Length();
                        } */

                        
                      /*  Vector2[,] test = new Vector2[TEST_SIZE, TEST_SIZE];
                        float xd = 0;
                        for (int r = 0; r < TEST_SIZE; r++) {
                            for (int c = 0; c < TEST_SIZE; c++) {
                                test[r, c].X = MathF.Pow(c, aaar.Next(0, 3));
                                test[r, c].Y = MathF.Pow(aaar.Next(0, 3), r);
                            }
                        }
                        for (int r = 0; r < TEST_SIZE; r++) {
                            for (int c = 0; c < TEST_SIZE; c++) {
                                xd += Line.Rotate(test[r, c], aaar.Next(1, 4)).Length();
                            }
                        }*/
                    }
                }
                else if (auxReport.Raw[4] == 1) {
                    perfStopwatch.Restart();
                    for (int t = 0; t < 50000; t++) {
                        Vector2 tPoint = new Vector2(aaar.Next(-1000, 1000), aaar.Next(-1000, 1000));
                        Vector2[] test = new Vector2[TEST_SIZE];
                        for (int i = 0; i < TEST_SIZE; i++) {
                            test[i].X = MathF.Pow(i, aaar.Next(0, 3)) * aaar.Next(-1, 1);
                            test[i].Y = MathF.Pow(aaar.Next(0, 3), i) * aaar.Next(-1, 1);
                        }
                        Line[] testlines = new Line[TEST_SIZE];
                        Vector2 xd = Vector2.Zero;
                        for (int i = 0; i < TEST_SIZE; i++) {
                            testlines[i] = new Line(test[i], Vector2.Zero, i);
                            xd += testlines[i].SegmentPerpendicularDistance(tPoint, (float)(aaar.Next(1, 9) / 20), (float)(aaar.Next(11, 19) / 20));
                        } 


                       /*
                        Vector2[] test = new Vector2[TEST_SIZE];
                        float xd = 0;
                        for (int i = 0; i < TEST_SIZE; i++) {
                            test[i].X = MathF.Pow(i, aaar.Next(0, 3));
                            test[i].Y = MathF.Pow(aaar.Next(0, 3), i);
                        }
                        for (int i = 1; i < TEST_SIZE - 1; i++) {
                            xd += Trajectory(test[i - 1], test[i], test[i + 1], (float)(aaar.Next(1, 9) / 5)).Length();
                        } */

                        /*Vector2[,] test = new Vector2[TEST_SIZE, TEST_SIZE];
                        float xd = 0;
                        for (int r = 0; r < TEST_SIZE; r++) {
                            for (int c = 0; c < TEST_SIZE; c++) {
                                test[r, c].X = MathF.Pow(c, aaar.Next(0, 3));
                                test[r, c].Y = MathF.Pow(aaar.Next(0, 3), r);
                            }
                        }
                        for (int r = 0; r < TEST_SIZE; r++) {
                            for (int c = 0; c < TEST_SIZE; c++) {
                                xd += Line.Rotate(test[r, c], aaar.Next(1, 4)).Length();
                            }
                        }*/

                    }
                }
                Console.WriteLine(perfStopwatch.Restart().TotalMicroseconds);
            }
            Emit?.Invoke(value);
        }

        void Plot() {
            Console.Write("vx");
            Console.WriteLine(tp.X);
            Console.Write("vy");
            Console.WriteLine(tp.Y * -1);
            Console.WriteLine("xx");
            Console.WriteLine("dd");
        }

       public class Line {
            public Vector2 Start;
            public Vector2 End;
            public float Time;

            public Line(Vector2 s, Vector2 e, float t) {
                Start = s;
                End = e;
                Time = t;
            }

            public static Vector2 Rotate(Vector2 p, float a) {
                float cosine = MathF.Cos(a);
                float sine = MathF.Sin(a);
                return new Vector2((cosine * p.X) - (sine * p.Y), (sine * p.X) + (cosine * p.Y));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector2 RotateAIL(Vector2 p, float a) {
                float cosine = MathF.Cos(a);
                float sine = MathF.Sin(a);
                return new Vector2((cosine * p.X) - (sine * p.Y), (sine * p.X) + (cosine * p.Y));
            }

            public void Step(float t) {
                Vector2 ldir = ((Start - End) / Time) * t;
                Start += ldir;
                End += ldir;
            }

            public Vector2 Curve(Vector2 p1, float t) {
                Vector2 tMid = 0.5f * (End + Start);
                return End + t * ((2 * p1) - End - tMid) + 0.5f * t * t * (2 * (tMid - p1));
            } 

            public static float SelfSmoothstep(float x) {
                x = Math.Clamp(x, 0, 1);
                return x * x * (3.0f - 2.0f * x);
            }

            public static float SelfSmootherstep(float x) {
                x = Math.Clamp(x, 0, 1);
                return x * x * x * (x * (6.0f * x - 15.0f) + 10.0f);
            }

            public float ASSSS(float x) => SelfSmoothstep(x + 1 / Time);
                
            public float ASSRSS(float x) => SelfSmootherstep(x + 1 / Time);

            public static Vector2 DTP(Vector2 mp, Vector2 me) {
                float a = MathF.Atan2(me.Y, me.X);
                float ca = -a;
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                if (rp.X < 0f) return mp;
                else if (rp.X > re.X) return Rotate(rp - re, a);
                else return Rotate(new Vector2(0f, rp.Y), a);
            }

            public static float DTPL(Vector2 mp, Vector2 me) {
                float ca = -MathF.Atan2(me.Y, me.X);
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                if (rp.X < 0f) return rp.Length();
                else if (rp.X > re.X) return (rp - re).Length();
                else return rp.Y;
            }

             [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector2 PDAIL(Vector2 mp, Vector2 me) {
                float a = MathF.Atan2(me.Y, me.X);
                float ca = -a;
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                if (rp.X < 0f) return Rotate(new Vector2(rp.X, 0f), a);
                else if (rp.X > re.X) return Rotate(new Vector2(rp.X - re.X, 0f), a);
                else return Vector2.Zero;
            }

            public static Vector2 PD(Vector2 mp, Vector2 me) {
                float a = MathF.Atan2(me.Y, me.X);
                float ca = -a;
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                if (rp.X < 0f) return Rotate(new Vector2(rp.X, 0f), a);
                else if (rp.X > re.X) return Rotate(new Vector2(rp.X - re.X, 0f), a);
                else return Vector2.Zero;
            }

            public static float PDL(Vector2 mp, Vector2 me) {
                float ca = -MathF.Atan2(me.Y, me.X);
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                if (rp.X < 0f) return rp.X;
                else if (rp.X > re.X) return rp.X - re.X;
                else return 0f;
            }

            public Vector2 FullDistanceToPoint(Vector2 p) {
                return DTP(p - Start, End - Start);
            }

            public Vector2 SegmentDistanceToPoint(Vector2 p, float t1, float t2) {
                Vector2 ss = Vector2.Lerp(Start, End, t1);
                Vector2 se = Vector2.Lerp(Start, End, t2);
                return DTP(p - ss, se - ss);
            } 

            public Vector2 DirtyCurveDistanceToPoint(Vector2 p, Vector2 c, float t1, float t2) {
                Vector2 ss = Curve(c, t1 * 2);
                Vector2 se = Curve(c, t2 * 2);
                return DTP(p - ss, se - ss);
            } 

            public float FullDistanceToPointL(Vector2 p) {
                return DTPL(p - Start, End - Start);
            }

            public float SegmentDistanceToPointL(Vector2 p, float t1, float t2) {
                Vector2 ss = Vector2.Lerp(Start, End, t1);
                Vector2 se = Vector2.Lerp(Start, End, t2);
                return DTPL(p - ss, se - ss);
            } 

            public float DirtyCurveDistanceToPointL(Vector2 p, Vector2 c, float t1, float t2) {
                Vector2 ss = Curve(c, t1 * 2);
                Vector2 se = Curve(c, t2 * 2);
                return DTPL(p - ss, se - ss);
            } 

            public Vector2 FullPerpendicularDistance(Vector2 p) {
                return PD(p - Start, End - Start);
            }

            public Vector2 SegmentPerpendicularDistance(Vector2 p, float t1, float t2) {
                Vector2 ss = Vector2.Lerp(Start, End, t1);
                Vector2 se = Vector2.Lerp(Start, End, t2);
                return PD(p - ss, se - ss);
            } 

           
            public Vector2 SegmentPerpendicularDistanceAIL(Vector2 p, float t1, float t2) {
                Vector2 ss = Vector2.Lerp(Start, End, t1);
                Vector2 se = Vector2.Lerp(Start, End, t2);
                return PDAIL(p - ss, se - ss);
            }

            public Vector2 DirtyCurvePerpendicularDistance(Vector2 p, Vector2 c, float t1, float t2) {
                Vector2 ss = Curve(c, t1 * 2);
                Vector2 se = Curve(c, t2 * 2);
                return PD(p - ss, se - ss);
            } 

            public float FullPerpendicularDistanceL(Vector2 p) {
                return PDL(p - Start, End - Start);
            }

            public float SegmentPerpendicularDistanceL(Vector2 p, float t1, float t2) {
                Vector2 ss = Vector2.Lerp(Start, End, t1);
                Vector2 se = Vector2.Lerp(Start, End, t2);
                return PDL(p - ss, se - ss);
            } 

            public float DirtyCurvePerpendicularDistanceL(Vector2 p, Vector2 c, float t1, float t2) {
                Vector2 ss = Curve(c, t1 * 2);
                Vector2 se = Curve(c, t2 * 2);
                return PDL(p - ss, se - ss);
            } 
        }

        public static Vector2 Trajectory(Vector2 p0, Vector2 p1, Vector2 p2, float t) {
            Vector2 tMid = 0.5f * (p0 + p2);
            return p2 + t * ((2 * p1) - p2 - tMid) + 0.5f * t * t * (2 * (tMid - p1));
        } 

        Random aaar = new Random();

        Vector2 tp;
        Line line0;
        Vector2 pos0, pos1, pos2, dir0, dir1, dir2, dir3, ddir0, ddir1, planestart, planeend, peak;
        float vel0, vel1, vel2, accel0, accel1, pointaccel0, pointaccel1;
        float peakMag, planeMag;
        Vector2 clusterpos0, clusterpos1;
        Vector2 clusterdir0, clusterdir1;
        float magcluster0, magcluster1;
        Line plane, ctozero, turnmirror;
        bool clusterjumping, magclusterjumping;
        Vector2 stdir0, stdir1, stdir2, stdir3;
        float stmag0, stmag1;
        float reportTime;
        float reportMsAvg = (1 / 303);
        Vector2 testOutput, testDir;
        bool emergency;
        int namelesstime0, namelesstime1;
        float linedrivetime;
        bool linedriving;
        Vector2 arc;
        float savetime;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        float alpha0, alpha1, alpha0PreservationSociety;
        float top, bottom;
        Vector2 a1stdir0, a1stdir1, a1stdir2;
        Vector2 sdirt1;
        Vector2 pps2Dir;
        float pathpreservationsociety, pps2, pps3;
        bool consume;
        Vector2 sense;
        float peakAccel0, peakAccel1;
        Vector2 trDir;
        private HPETDeltaStopwatch perfStopwatch = new HPETDeltaStopwatch();
        double updatePerfTimeAvg;
        double updates = 0;
        uint pressure0, pressure1;
        bool liftorpress;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}