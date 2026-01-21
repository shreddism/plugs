using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace TR000110
{
    [PluginName("TR000110")]
    public class TR000110 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public TR000110() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        protected override void ConsumeState()
        {
            if (State is not IAbsolutePositionReport report || TabletReference == null)
            {
                OnEmit();
                return;
            }

            float consumeDelta = (float)reportStopwatch.Restart().TotalSeconds;

            if (consumeDelta > 0.03f || consumeDelta < 0.0001f)
            {
                tOffset = 0;
                return;
            }

            InsertAtFirst(points, report.Position);

            rpsAvg += (1f / consumeDelta - rpsAvg) * (1f - MathF.Exp(-2f * consumeDelta));
            float secAvg = 1f / rpsAvg;
            float msAvg = 1000f * secAvg;

            tOffset += secAvg - consumeDelta;
            tOffset *= MathF.Exp(-5f * consumeDelta);
            tOffset = Math.Clamp(tOffset, -secAvg, secAvg);
            latestReport = runningStopwatch.Elapsed + TimeSpan.FromSeconds(tOffset);

        }

        protected override void UpdateState()   // Interpolation
        {
            if (State is not IAbsolutePositionReport report || !PenIsInRange())
                return;

            float t = 1 + (float)(runningStopwatch.Elapsed - latestReport).TotalSeconds * rpsAvg;
            t = Math.Clamp(t, 0, 3);

            report.Position = Trajectory(t, points[2], points[1], points[0]);

            State = report;

            OnEmit();
        }

        private static readonly int steps = 256;
        private static readonly float dt = 1f / steps;
        private float[] arcArr = new float[steps];
        private float arcTar = 0;
        private Vector2 _v1, _v2, _v3;
        private int _floor;
        Vector2 Trajectory(float t, Vector2 v3, Vector2 v2, Vector2 v1)
        {
            var mid = 0.5f * (v1 + v3);
            var accel = 2f * (mid - v2);
            var vel = 2f * v2 - v3 - mid;
            
            // if there is acceleration, then start spacing points evenly using integrals
            if (Vector2.Dot(accel, accel) > 0.001f)
            {
                int floor = (int)Math.Floor(t);
                var _vel = vel + accel * floor;

                // if any of the inputs have changed, recalculate arcArr
                if ((_floor != floor) || (_v1 != v1) || (_v2 != v2) || (_v3 != v3))
                {
                    _v1 = v1;
                    _v2 = v2;
                    _v3 = v3;
                    _floor = floor;
                    arcTar = 0;

                    for (int _t = 0; _t < steps; _t++)
                    {
                        arcArr[_t] = arcTar;
                        arcTar += (_vel + _t * dt * accel).Length();
                    }
                }

                float _arcTar = arcTar * (t - floor);

                for (int _t = 0; _t < steps; _t++)
                {
                    if (arcArr[_t] < _arcTar) continue;
                    t = _t * dt + floor;
                    break;
                }
            }

            return v3 + t * vel + 0.5f * t * t * accel;
        }

        void InsertAtFirst<T>(T[] arr, T element)
        {
        for (int p = arr.Length - 1; p > 0; p--) arr[p] = arr[p - 1];
        arr[0] = element;
        }

        float rpsAvg = 200f, tOffset;
        TimeSpan latestReport = TimeSpan.Zero;
        Vector2[] points = new Vector2[3];

        HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch(false);
        HPETDeltaStopwatch runningStopwatch = new HPETDeltaStopwatch(true);

        [TabletReference]
        public TabletReference? TabletReference { get; set; }
    }
}