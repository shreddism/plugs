using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace Plugin251214
{
    [PluginName("Plugin251214")]
    public class Plugin251214 : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Plugin251214() : base()
        {
        }

        // Smells weird.

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        protected override void ConsumeState()  // Report
        {
            if (State is ITabletReport report)
            {            
                lastPosition = currPosition;
                currPosition = report.Position; 
              //   Console.WriteLine("----------------");
                StatUpdate(report.Position);

                
            }
            else if (State is IAuxReport auxReport) {
                
                auxInput = 7;
                if (auxReport.Raw[4] == 127) {
                   auxInput = 5;
                }
                else if (auxReport.Raw[4] == 1) {
                   auxInput = 6;
                }
                else for (int i = 0; i < auxReport.AuxButtons.Length; i++) {
                    if (auxReport.AuxButtons[i]) {
                       auxInput = i;
                    }
                }

                switch (auxInput) {
                    case 0:
                        if (selected == 0)
                            selected = 5;
                        else selected--;
                    break;
                    case 1:
                        ChangeByScale(selected, 1);
                    break;
                    case 2:
                        if (selected == 5)
                            selected = 0;
                        else selected++;
                    break;
                    case 3:
                        ChangeByScale(selected, -1);
                    break;
                    case 5:
                        ChangeByScale(selected, -0.01);
                    break;
                    case 6:
                        ChangeByScale(selected, 0.01);
                    break;
                    default:
                    break;
                }

                    
                PrintSettings();

            }
            else OnEmit();
        }

        public string Selection(int num) {
            if (num == 0) {
                selected--;
                return "> ";
            }
            else { 
                selected--;
                return "  ";
                
            }
        }

        public void ChangeByScale(int selection, double scale) {
            switch (selection) {
            case 0:
                opt0 = CleanUp2(opt0 + scale);
            break;
            case 1:
                opt1 = CleanUp2(opt1 + scale);
            break;
            case 2:
                opt2 = CleanUp2(opt2 + scale);
            break;
            case 3:
                opt3 = CleanUp2(opt3 + scale);
            break;
            case 4:
                opt4 = CleanUp2(opt4 + scale);
            break;
            case 5:
                opt5 = CleanUp2(opt5 + scale);
            break;
            }

        }

        public double CleanUp2(double num) {
            return Math.Round(num * 100) / 100;
        }

        public void PrintSettings() {

            saveSelect = selected;

                    Console.WriteLine("--- Setting Hotswap ---");
                    Console.WriteLine(Selection(selected) + "Zero: " + opt0);// + " " + check0 + (check0 ? "-----------------" : ""));
                    Console.WriteLine(Selection(selected) + "One: " + opt1);// + " " + check1 + (check1 ? "-----------------" : ""));
                    Console.WriteLine(Selection(selected) + "Two: " + opt2);// + " " + check2 + (check2 ? "-----------------" : ""));
                    Console.WriteLine(Selection(selected) + "Three: " + opt3);// + " " + check3 + (check3 ? "-----------------" : ""));
                    Console.WriteLine(Selection(selected) + "Four: " + opt4);// + " " + check4 + (check4 ? "-----------------" : ""));
                    Console.WriteLine(Selection(selected) + "Five: " + opt5);// + " " + check5 + (check5 ? "-----------------" : ""));
                    Console.WriteLine("-----------------------");

            selected = saveSelect;

        }

        protected override void UpdateState()   // Interpolation
        {

            if (State is ITabletReport report && PenIsInRange())
            {

                if (
                    (
                IsZero(Vector2.DistanceSquared(outputPosition, currPosition)) &&
                IsZero(velocity.X * velocity.X * velocity.Y * velocity.Y))
                || (reportStopwatch.Elapsed.TotalMilliseconds > 5)) {
                    outputPosition = currPosition;
                    velocity = Vector2.Zero;
                    accel_v = Vector2.Zero;
                    outputVelocity = Vector2.Zero;
                    lastOutputVelocity = Vector2.Zero;
                    lastOutputPosition = outputPosition;
                    reportStopwatch.Restart();
                   
                }
                else {
                    var delta = (float)reportStopwatch.Restart().TotalMilliseconds / (float)opt2;

                    lastOutputVelocity = outputVelocity;

                    lastOutputPosition = outputPosition;

                    accel_v += (1f / (float)(opt0 / mod1)) * ((pos0 - pos1) - outputVelocity) * delta;

                    accel_v *= MathF.Pow(1f / (float)(opt1 * mod1), delta);

                    outputVelocity += accel_v;

                    outputPosition = lastOutputPosition + (outputVelocity / (float)opt3);

                    Console.WriteLine(pos0 - outputPosition);

                    outputPosition = Vector2.Lerp(outputPosition, currPosition, 0.05f);
                  
                }

               // if (Vector2.DistanceSquared(outputPosition, currPosition) > Vector2.DistanceSquared(lastOutputPosition, currPosition))
                //    outputPosition = currPosition;
          //      Console.WriteLine(Vector2.Distance(outputPosition, lastOutputPosition));

                report.Position = outputPosition;

                State = report;

                OnEmit();
            }
        }

        public void StatUpdate(Vector2 position) {
            
            pos1 = pos0;
            pos0 = position;

            vel1 = vel0;
            vel0 = Vector2.Distance(pos0, pos1);

            accel1 = accel0;
            accel0 = vel0 - vel1;

            jerk1 = jerk0;
            jerk0 = accel0 - accel1;
            
            snap1 = snap0;
            snap0 = jerk0 - jerk1;

             mod1 = Math.Min(Math.Max((accel1 < 0 ? 1 : 0) * Math.Max(Math.Abs(accel0) + jerk0 + jerk1, 10) / 10, 1), 10);
            
        }

        private static bool IsZero(float a)
        {
            return MathF.Abs(a) < 0.0001f;
        }

        public int auxInput, selected, saveSelect;
        public double opt0 = 1;
        public double opt1 = 5;
        public double opt2 = 10;
        public double opt4;
        public double opt5;

        Vector2 currPosition, lastPosition;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private double opt3 = (3.3);
        Vector2 velocity;
        Vector2 outputPosition, lastOutputPosition;
        public double vel0, vel1, accel0, accel1, jerk0, jerk1, snap0, snap1;
        public double mod0, mod1, mod2;
        Vector2 pos0, pos1;
        public double springvel0, springvel1;
        Vector2 outputVelocity, lastOutputVelocity;
        Vector2 accel_v;
    }
}