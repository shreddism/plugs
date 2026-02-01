using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace pg    
{
    [PluginName("pg")]
    public class pg : IPositionedPipelineElement<IDeviceReport>
    {
        public pg() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PostTransform;

        public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                StatUpdate(report);
                
                Console.WriteLine(pos[0]);
            Emit?.Invoke(value);
            }
        }

        void StatUpdate(ITabletReport report) {
            InsertAtFirst(pos, report.Position);
            InsertAtFirst(dir, pos[0] - pos[1]);
            InsertAtFirst(ddir, dir[0] - dir[1]);
            InsertAtFirst(vel, dir[0].Length());
            InsertAtFirst(accel, vel[0] - vel[1]);
            InsertAtFirst(pointaccel, ddir[0].Length());
        }

        void InsertAtFirst<T>(T[] arr, T element)
        {
            for (int p = arr.Length - 1; p > 0; p--) arr[p] = arr[p - 1];
            arr[0] = element;
        }

        const int HMAX = 128;

        float DotNorm(Vector2 a, Vector2 b) => Vector2.Dot(Vector2.Normalize(a), Vector2.Normalize(b));

        float DotNorm(Vector2 a, Vector2 b, float x) => (a != Vector2.Zero && b != Vector2.Zero) ? Vector2.Dot(Vector2.Normalize(a), Vector2.Normalize(b)) : x;

        float CrossNorm(Vector2 a, Vector2 b) => Vector2.Cross(Vector2.Normalize(a), Vector2.Normalize(b));

        float CrossNorm(Vector2 a, Vector2 b, float x) => (a != Vector2.Zero && b != Vector2.Zero) ? Vector2.Cross(Vector2.Normalize(a), Vector2.Normalize(b)) : x;

        Vector2 NormDotCross(Vector2 a, Vector2 b) => (a != Vector2.Zero && b != Vector2.Zero) ? Vector2.Normalize(new Vector2(Vector2.Dot(a, b), Vector2.Cross(a, b))) : Vector2.Zero;

        Vector2 NormDotCrossAlt(Vector2 a, Vector2 b) {
            if (a != Vector2.Zero) {
                if (b != Vector2.Zero) return Vector2.Normalize(new Vector2(Vector2.Dot(a, b), Vector2.Cross(a, b)));
                else return -Vector2.Normalize(a);
            }
            else {
                if (b != Vector2.Zero) return Vector2.Normalize(b);
                else return Vector2.Zero;
            }
        }

        int pls = 0;

        Vector2[] pos = new Vector2[HMAX];
        Vector2[] dir = new Vector2[HMAX];
        Vector2[] ddir = new Vector2[HMAX];
        float[] vel = new float[HMAX];
        float[] accel = new float[HMAX];
        float[] pointaccel = new float[HMAX];
    }
}