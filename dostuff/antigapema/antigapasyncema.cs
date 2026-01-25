using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace antigapasyncema
{
    [PluginName("antigapasyncema")]
    public class antigapasyncema : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public antigapasyncema() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PostTransform;

        [Property("weight"), DefaultPropertyValue(1f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float weight { 
            set => _weight = Math.Clamp(value, 0, 1);
            get => _weight;
        }
        public float _weight;

        [Property("wire"), DefaultPropertyValue(true), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public bool wire { set; get; }

        [Property("Async Wire Stack Override"), DefaultPropertyValue(true), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public bool asyncwire { set; get; }

        [Property("expect"), DefaultPropertyValue(0.5f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float expect { set; get; }

        public event Action<IDeviceReport> Emit;

        protected override void ConsumeState() {
            if (State is ITabletReport report)
            {
                
                
                consumeTime = (float)consumeStopwatch.Restart().TotalMilliseconds;
                if (consumeTime < 25 && consumeTime > 0.1f && !asyncwire) {
                    consumeMsAvg += ((consumeTime - consumeMsAvg) * 0.1f);
                    consumeFrequency = 1 / consumeMsAvg;
                }

                if (asyncwire)
                consumeMsAvg = (1000 / Frequency);
                
                evenedWeight = (1 - MathF.Pow(1 - weight, (consumeMsAvg / (1000 / Frequency)))) / (consumeMsAvg / (1000 / Frequency));

              //  Console.WriteLine(consumeMsAvg);

            //    Console.WriteLine(evenedWeight);

                pos0 = report.Position;

                dist = pos0 - outputPos;

                consume = true;

              //  Console.WriteLine(consumeTime);

                if (wire)
                    UpdateState();
            }
            else OnEmit();
        }

        protected override void UpdateState() {
            if (State is ITabletReport report && PenIsInRange()) {

                updateTime = (float)updateStopwatch.Restart().TotalMilliseconds;

                if (wire) {
                    adjWeight = evenedWeight * (updateTime / (1000 / Frequency));
                }
                else adjWeight = evenedWeight;

                
                remainingDist = pos0 - outputPos;
                outputPos += MinLength(adjWeight * dist, remainingDist);
                
                report.Position = outputPos;

                /*Console.WriteLine(updateTime);
                Console.WriteLine(adjWeight);
                Console.WriteLine("-------");*/

                if (asyncwire && Math.Abs(updateTime - expect) > 0.1f && updateTime > expect) {
                    Console.WriteLine(updateTime);
                }

            //    Console.WriteLine(pos0 - outputPos);


            

                if (consume) {
                    consume = false;
                }
                
                OnEmit();
            }
        }

        Vector2 MaxLength(Vector2 a, Vector2 b) => a.Length() >= b.Length() ? a : b;

        Vector2 MinLength(Vector2 a, Vector2 b) => a.Length() <= b.Length() ? a : b;

        public static float FSmootherstep(float x, float start, float end) 
        {
            x = (float)Math.Clamp((x - start) / (end - start), 0.0f, 1.0f);

            return (float)(x * x * x * (x * (6.0f * x - 15.0f) + 10.0f));
        }

        public static float ClampedLerp(float start, float end, float scale)
        {
            scale = (float)Math.Clamp(scale, 0, 1);

            return start + scale * (end - start);
        }

        Vector2 outputPos, dist, pos0, evenedDist, remainingDist;
        float evenedWeight, adjWeight;
        float consumeTime, consumeMsAvg = (1000 / 303);
        float updateTime;
        float consumeFrequency = 303;
        bool consume;
        int sc;
        private HPETDeltaStopwatch consumeStopwatch = new HPETDeltaStopwatch();
        private HPETDeltaStopwatch updateStopwatch = new HPETDeltaStopwatch();
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}