using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Stroker
{
    [PluginName("Stroker")]
    public class Stroker : IPositionedPipelineElement<IDeviceReport>
    {
        public Stroker() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PostTransform;

        [Property("Line Direction (Positive Degrees Counterclockwise From Positive X Axis)"), DefaultPropertyValue(0f)]
        public float opt1 { 
            set => _opt1 = (float.DegreesToRadians(-value));
            get => _opt1;
        }
        public float _opt1;

        [BooleanProperty("Two-Way Line", ""), DefaultPropertyValue(false)]
        public bool opt2 { 
            set => _opt2 = value;
            get => _opt2;
        }
        public bool _opt2;

        public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                pos2 = pos1;
                pos1 = pos0;
                pos0 = report.Position;
                outputPos1 = outputPos0;
                outputPos0 = pos0;

                press1 = press0;
                press0 = report.Pressure;

                if (press0 > 0) {
                    
                    if (press1 == 0) {
                        Vector2 dir;
                        dir.X = 100000 * MathF.Cos(opt1);
                        dir.Y = 100000 * MathF.Sin(opt1);
                        Vector2 sp = pos0;
                
                        if (opt2)
                        sp -= dir;
                    
                        Vector2 ep = pos0 + dir;
                       
                        line0 = new Line(sp, ep, 0);
                    }
                    else {
                        Vector2 dist = line0.DistanceToPoint(pos0);
                        
                            outputPos0 -= dist;
                        
                    }
                }
                else if (press1 > 0) {
                    outputPos0 = outputPos1;
                }

                report.Position = outputPos0;
                
               // Plot();
            }
            Emit?.Invoke(value);
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
        float press0, press1;

        Vector2 outputPos0, outputPos1;
        Line line0;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}