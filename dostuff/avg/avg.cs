using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace avg
{
    [PluginName("display std dev / land ratio")]
    public class Plugin : IPositionedPipelineElement<IDeviceReport>
    {
        public Plugin() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        [Property("option1"), DefaultPropertyValue(250), ToolTip(
            "option1"
        )]
        public int opt1 { 
            set => _opt1 = Math.Max(2, value);
            get => _opt1;
        }
        public int _opt1 = 250;

        public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report) {
                if (!init) {
                    arr = new Vector2[opt1];
                    init = true;
                }
                if (tick == 49) {
                    initPos = report.Position;
                }

                if (tick == 50 + opt1) {
                    tick = 48;
                    Vector2 avg = folkington / (float)opt1;
                    for (int i = 0; i < opt1; i++) {
                        double xerm = (double)avg.X - (double)arr[i].X;
                        double yerm = (double)avg.Y - (double)arr[i].Y;
                        xdev += xerm * xerm;
                        ydev += yerm * yerm;
                    }
                    xdev = Math.Sqrt(xdev * (1.0 / (double)(opt1 - 1)));
                    ydev = Math.Sqrt(ydev * (1.0 / (double)(opt1 - 1)));

                    double withinX = 0;
                    double withinY = 0;
                   
                    for (int i = 0; i < opt1; i++) {
                        if (Math.Abs(avg.X - arr[i].X) <= 1.0 * xdev)
                            withinX += 1.0;

                        if (Math.Abs(avg.Y - arr[i].Y) <= 1.0 * ydev)
                            withinY += 1.0;
                    }                    

                    Console.WriteLine((xdev / (withinX / (double)(opt1))) + ", " + (ydev / (withinY / (double)(opt1))));

                    folkington = Vector2.Zero;
                    xdev = 0.0;
                    ydev = 0.0;
                }

                if (tick > 49) {
                    arr[tick - 50] = report.Position - initPos;
                    folkington += arr[tick - 50];
                } 
                tick++;
            }
            Emit?.Invoke(value);
        }

        Vector2 initPos;
        Vector2 folkington;
        double xdev, ydev;
        Vector2[] arr;
        bool init;
        int tick;
    }
}