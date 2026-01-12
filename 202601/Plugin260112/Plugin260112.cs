using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Plugin260112
{
    [PluginName("Plugin260112")]
    public class Plugin260112 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Plugin260112() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        protected override void ConsumeState()
        {
            if (State is ITabletReport report)
            {
                StatUpdate(report);
                ConditionalUpdate();
            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
            if (State is ITabletReport report && PenIsInRange())
            {
                
                report.Position = pos0;
                OnEmit();
            }
        }

        void StatUpdate(ITabletReport report) {
            pos2 = pos1;
            pos1 = pos0;
            pos0 = report.Position;

            dir2 = dir1;
            dir1 = dir0;
            dir0 = (pos0 - pos1);

            vel1 = vel0;
            vel0 = dir0.Length();

            accel1 = accel0;
            accel0 = (vel0 - vel1);

            ddir1 = ddir0;
            ddir0 = (dir0 - dir1);

            pointaccel1 = pointaccel0;
            pointaccel0 = ddir0.Length();
        }

        void ConditionalUpdate() {
            if (pointaccel0 > 5 && !(accel0 > 0 && accel1 < 0) && !(accel0 < 0 && accel1 > 0)) {
                if (!clustermoving) {
                    clusterpos1 = clusterpos0;
                    clustermoving = true;
                }
            }
            else {
                clusterpos0 = pos0;
                clustermoving = false;
            }

            if (Math.Abs(accel0) > 3 && !(accel0 > 0 && accel1 < 0) && !(accel0 < 0 && accel1 > 0)) {
                if (!magclustermoving) {
                    magcluster1 = magcluster0;
                    magclustermoving = true;
                }
            }
            else {
                magcluster0 = vel0;
                magclustermoving = false;
            }
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
                Vector2 sp = p;
                Vector2 rp;
                rp.X = (MathF.Cos(a) * sp.X) - (MathF.Sin(a) * sp.Y);
                rp.Y = (MathF.Sin(a) * sp.X) + (MathF.Cos(a) * sp.Y);
                return rp;
            }

            public void Step(float t) {
                Vector2 ldir = ((Start - End) / Time) * t;
                Start += ldir;
                End += ldir;
            }

            public Vector2 Curve(Vector2 p1, float t) {
                Vector2 tMid = 0.5f * (End + Start);
                Vector2 tAccel = 2 * (tMid - p1);
                Vector2 tVel = (2 * p1) - End - tMid;
                return End + t * tVel + 0.5f * t * t * tAccel;
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

            public Vector2 DTP(Vector2 mp, Vector2 me) {
                float a = MathF.Atan2(me.Y, me.X);
                float ca = -1 * MathF.Atan2(me.Y, me.X);
                Vector2 rp = Rotate(mp, ca);
                Vector2 re = Rotate(me, ca);
                if (rp.X < 0)
                return Rotate(rp, a);
                else if (rp.X > re.X)
                return Rotate(rp - re, a);
                else {
                    rp.X = 0;
                    return Rotate(rp, a);
                }
            }
            
            public Vector2 FullDistanceToPoint(Vector2 p) {
                Vector2 mp = p - Start;
                Vector2 me = End - Start;
                return DTP(mp, me);
            }

            public Vector2 SegmentDistanceToPoint(Vector2 p, float t1, float t2) {
                Vector2 ss = Vector2.Lerp(Start, End, t1);
                Vector2 se = Vector2.Lerp(Start, End, t2);
                Vector2 mp = p - ss;
                Vector2 me = se - ss;
                return DTP(mp, me);
            }

            public Vector2 DirtyCurveDistanceToPoint(Vector2 p, Vector2 c, float t1, float t2) {
                Vector2 ss = Curve(c, t1);
                Vector2 se = Curve(c, t2);
                Vector2 mp = p - ss;
                Vector2 me = se - ss;
                return DTP(mp, me);
            }
        }

        Vector2 pos0, pos1, pos2, dir0, dir1, dir2, ddir0, ddir1, planestart, planeend, peak;
        float vel0, vel1, accel0, accel1, pointaccel0, pointaccel1;
        float peakMag, planeMag;
        Vector2 clusterpos0, clusterpos1;
        float magcluster0, magcluster1;
        Line plane, peaktozero, turnmirror;
        bool clustermoving, magclustermoving;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}