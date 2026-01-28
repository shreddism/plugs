using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Saturn
{
    [PluginName("Saturn - Anti-Gap Hawku Smoothing/Devocub Antichatter")]
    public class AntiGap : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public AntiGap() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PostTransform;

        [Property("Hawku/Devocub Toggle"), DefaultPropertyValue(true), ToolTip
        (
            "Toggles whether the filter mimics Hawku Smoothing or Devocub Antichatter,\n" +
            "the difference being that Devocub is somewhat adaptive to how far it is currently behind raw report."
        )]
        public bool hdToggle { set; get; }

        [Property("'Latency'"), DefaultPropertyValue(1.0f), ToolTip
        (
            "This is just a fancy way of selecting weight.\n" +
            "An added latency of 0 won't break things."
        )]
        public float latency { 
            set => _latency = Math.Clamp(value, 0.0f, 1000000.0f);
            get => _latency;
        }
        public float _latency;

        [Property("Internal 'Threshold' Constant"), DefaultPropertyValue(0.9f), ToolTip
        (
            "I'm not sure what the purpose of this is.\n" +
            "0.63 is what Hawku uses, 0.9 is what Devocub uses."
        )]
        public float threshold { 
            set => _threshold = Math.Clamp(value, 0.0f, 1.0f);
            get => _threshold;
        }
        public float _threshold;

        [Property("Antichatter Strength"), DefaultPropertyValue(3.0f), ToolTip(ANTICHATTER_TOOLTIP)]
        public float AntichatterStrength { set; get; }

        [Property("Antichatter Multiplier"), DefaultPropertyValue(1.0f), ToolTip(ANTICHATTER_TOOLTIP)]
        public float AntichatterMultiplier { set; get; }

        [Property("Antichatter Offset X"), ToolTip(ANTICHATTER_TOOLTIP)]
        public float AntichatterOffsetX { set; get; }

        [Property("Antichatter Offset Y"), DefaultPropertyValue(1.0f), ToolTip(ANTICHATTER_TOOLTIP)]
        public float AntichatterOffsetY { set; get; }

        [Property("Scale Override"), DefaultPropertyValue(5.0f), ToolTip
        (
            "To ensure proper order, this filter is post transform, which uses pixels instead of millimeters.\n" +
            "Use this to change the scale of distance."
        )]
        public float scale {
            set => _scale = Math.Clamp(value, 0.1f, 1000000.0f);
            get => _scale;
        }
        public float _scale;

        [Property("wire"), DefaultPropertyValue(true), ToolTip
        (
            "Should probably be enabled."
        )]
        public bool wire { set; get; }

        [Property("Spring Mult"), DefaultPropertyValue(0.1f), ToolTip
        (
            "Modifies the filter slightly to act more SpringInterpolator-like.\n" +
            "Overshoot is always prevented."
        )]
        public float sMult {
            set => _sMult = Math.Clamp(value, 0.0f, 1.0f);
            get => _sMult;
        }
        public float _sMult;

        [Property("Async Wire Stack Override"), DefaultPropertyValue(false), ToolTip
        (
            "Only enable this if you know what you are doing."
        )]
        public bool asyncwire { set; get; }

        [Property("Expected Update Time Maximum"), DefaultPropertyValue(0.5f), ToolTip
        (
            "Applies to Async Wiring. You should know what you are doing."
        )]
        public float expectU { set; get; }

        [Property("Expected Consume Time Override"), DefaultPropertyValue(0.0f), ToolTip
        (
            "You should know what you are doing if you change this from 0."
        )]
        public float expectC { set; get; }

        public event Action<IDeviceReport> Emit;

        protected override void ConsumeState() {
            if (State is ITabletReport report)
            {  
                if (isReady) {  
                    if (!constantEvenedWeight) {
                        consumeTime = (float)consumeStopwatch.Restart().TotalMilliseconds;

                        if (consumeTime < 25.0f && consumeTime > 0.1f) {
                            consumeMsAvg += ((consumeTime - consumeMsAvg) * 0.1f);
                        }

                        evenedWeight = (1 - MathF.Pow(1 - weight, (consumeMsAvg / timerInterval))) / (consumeMsAvg / timerInterval);
                    }
        
                    pos0 = report.Position;
                    press0 = report.Pressure;
                    dist = pos0 - outputPos;
                    consume = true;

                    if (wire)
                        UpdateState();

                        
                }
                else {
                    SetWeight(latency);
                }
            }
            else OnEmit();
        }

        protected override void UpdateState() {
            if (State is ITabletReport report && PenIsInRange()) {

                updateTime = (float)updateStopwatch.Restart().TotalMilliseconds;

                if (wire) {
                    timeMult = (updateTime / timerInterval);
                }
                else { 
                    timeMult = 1.0f;
                }

                remainingDist = pos0 - outputPos;

                if (hdToggle) {
                    var weightModifier = (float)(MathF.Pow((remainingDist.Length() / scale) + AntichatterOffsetX, AntichatterStrength * -1) * AntichatterMultiplier);
                    if (weightModifier + AntichatterOffsetY < 0) {
                        if (!constantEvenedWeight) {
                            modWeight = 1 / Math.Max(consumeMsAvg, 1);
                        }
                        else {
                            if (expectC > 0) {
                                modWeight = 1 / expectC;
                            }
                            else modWeight = 1;     // Weight has yet to be multiplied by timeMult, which should always be below 1 in this case
                        } 
                    }
                    else {
                        if (!constantEvenedWeight) {
                            modWeight = Math.Clamp(evenedWeight / weightModifier, 0, 1 / Math.Max(consumeMsAvg, 1));
                        }
                        else {
                            if (expectC > 0) {
                                modWeight = Math.Clamp(evenedWeight / weightModifier, 0, 1 / expectC);
                                
                            }
                            else modWeight = Math.Clamp(evenedWeight / weightModifier, 0, 1);
                        }
                    } 
                }
                else modWeight = evenedWeight;

             //   Console.WriteLine(modWeight);

                adjWeight = modWeight * timeMult;
                adjSpringTest = springTest * timeMult;

                springTestSave = MinLength((adjWeight) * (dist + springTest), remainingDist);
                outputPos += springTestSave;
                springTest += springTestSave;
                springTest *= MathF.Pow(sMult, timeMult);
                
                report.Position = outputPos;
                report.Pressure = press0;

                if (!vec2IsFinite(report.Position)) {
                    report.Position = pos0;
                }

                if (asyncwire && Math.Abs(updateTime - expectU) > 0.1f && updateTime > expectU) {
                    Console.WriteLine(updateTime);
                }

                if (consume) {
                    consume = false;
                }
                OnEmit();
            }
        }

        Vector2 MinLength(Vector2 a, Vector2 b) => a.Length() <= b.Length() ? a : b;

        Vector2 outputPos, dist, pos0, evenedDist, remainingDist;
        Vector2 springTestSave, springTest, adjSpringTest;
        float evenedWeight, adjWeight, modWeight;
        float consumeTime, consumeMsAvg;
        float updateTime;
        float timeMult;
        bool consume;
        private float timerInterval => 1000 / Frequency;
        private HPETDeltaStopwatch consumeStopwatch = new HPETDeltaStopwatch();
        private HPETDeltaStopwatch updateStopwatch = new HPETDeltaStopwatch();
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
        float weight;
        bool isReady = false;
        bool constantEvenedWeight = false;
        uint press0;

        void SetWeight(float lat)
        {
            if (lat != 0) {
                float stepCount = lat / timerInterval;
                float target = 1 - threshold;
                weight = 1f - (1f / MathF.Pow(1f / target, 1f / stepCount));
                if (asyncwire) {
                    evenedWeight = weight;
                    constantEvenedWeight = true;
                } 
                else if (expectC > 0) {
                    evenedWeight = (1 - MathF.Pow(1 - weight, (expectC / (1000 / Frequency)))) / (expectC / (1000 / Frequency));
                    constantEvenedWeight = true;
                }
                else evenedWeight = 1;
            }
            else {
                weight = 1;
                if (asyncwire) {
                    evenedWeight = 1;
                    constantEvenedWeight = true;
                } 
                else if (expectC > 0) {
                    evenedWeight = 1 / expectC;
                    constantEvenedWeight = true;
                }
                else evenedWeight = 1;
            }
            isReady = true;
        }

        private const string ANTICHATTER_TOOLTIP = 

        "Same deal as Devocub filters. Only applies if the top toggle is checked.";

    }
}