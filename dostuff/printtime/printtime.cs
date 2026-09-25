using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace printtime
{
    [PluginName("printtime")]
    public class printtime : IPositionedPipelineElement<IDeviceReport>
    {
        public printtime() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

       /* [Property("msOverride"), DefaultPropertyValue(0f), ToolTip
        (
            "You should know what you are doing if you change this from 0, or your given default (don't)."
        )]
        public float testValue { 
            set => _testValue = Math.Clamp(value, 0f, 100f);
            get => _testValue;
        }
        public float _testValue;

        [Property("mult"), DefaultPropertyValue(0f), ToolTip
        (
            "You should know what you are doing if you change this from 0, or your given default (don't)."
        )]
        public float mult { 
            set => _mult = Math.Clamp(value, 0f, 100f);
            get => _mult;
        }
        public float _mult;*/

        public void Consume(IDeviceReport value)
        {    
            if (value is ITabletReport report)
            {
                double dt = (double)reportStopwatch.Restart().TotalMilliseconds;
                ct++;

          //      for (int i = 5; i > 0; i--) {
         //           delta[i] = delta[i - 1];
          //      }
         //           delta[0] = dt;

                if (ct > 50) {
                   // total += dt;
                   // tick += 1.0;
                    
                        Console.WriteLine(Math.Round(dt, 1, MidpointRounding.AwayFromZero));
                    //    Console.WriteLine(report.Position);
                      //  Console.WriteLine("------");
                    
                }
              //  if (!init) {
              //  Initialize();
             //   init = true;
              ///  return;
             //   }
             //   if (reportTime < 25f)   {
             //       reportMsAvg += ((reportTime - reportMsAvg) * 0.1f);
             //       error += (reportTime - testValue);
             //       lol += error;
             //   }
            //    Console.WriteLine(lol * mult);
            }
            Emit?.Invoke(value);
        }

   //     void Initialize() {
    //        reportMsAvg = testValue;
    //    }
    //    double total;
    //    double tick;

        int ct;

   //     float reportMsAvg;
    //    bool init = false;
     //   float error = 0.0f;
     //   float lol = 0.0f;


      //  double[] delta = new double[6];

        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
    }
}