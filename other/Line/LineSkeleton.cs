using System;
using System.Numerics;
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

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                pos2 = pos1;
                pos1 = pos0;
                pos0 = report.Position;
                line0 = new Line(pos2, pos0, 2);
                line0.Step(1);
                tp = line0.DistanceToPoint(pos1);
                Plot();
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

            public Vector2 DistanceToPoint(Vector2 p0) {
                Vector2 mp0 = p0 - Start;
                Vector2 me = End - Start;
                float a = MathF.Atan2(me.Y, me.X);
                float ca = -1 * MathF.Atan2(me.Y, me.X);
                Vector2 rp0;
                Vector2 re;
                rp0 = Rotate(mp0, ca);
                re = Rotate(me, ca);
                if (rp0.X < 0)
                return Rotate(rp0, a);
                else if (rp0.X > re.X)
                return Rotate(rp0 - re, a);
                else {
                    rp0.X = 0;
                    return Rotate(rp0, a);
                }
            }

            public Vector2 Rotate(Vector2 p, float a) {
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

        }

        public Vector2 Rotate(Vector2 p, float a) {
            Vector2 sp = p;
            Vector2 rp;
            rp.X = (MathF.Cos(a) * sp.X) - (MathF.Sin(a) * sp.Y);
            rp.Y = (MathF.Sin(a) * sp.X) + (MathF.Cos(a) * sp.Y);
            return rp;
        }

        Vector2 pos0, pos1, pos2, tp;
        Line line0;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}