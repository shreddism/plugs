using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;    

namespace ThresholdTester
{
    [PluginName("ThresholdTester")]
    public class ThresholdTester : IPositionedPipelineElement<IDeviceReport>
    {
        public ThresholdTester() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            processTime.Restart();
            if (value is ITabletReport report) {
                StatUpdate(report.Position);

            }
            else if (value is IAuxReport auxReport)
            {

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
                        ChangeByScale(selected, -0.05);
                    break;
                    case 6:
                        ChangeByScale(selected, 0.05);
                    break;
                    default:
                    break;
                }

                    saveSelect = selected;

                    Console.WriteLine("--- Setting Hotswap ---");
                    Console.WriteLine(Selection(selected) + "Zero: " + opt0 + check0);
                    Console.WriteLine(Selection(selected) + "One: " + opt1 + check1);
                    Console.WriteLine(Selection(selected) + "Two: " + opt2 + check2);
                    Console.WriteLine(Selection(selected) + "Three: " + opt3 + check3);
                    Console.WriteLine(Selection(selected) + "Four: " + opt4 + check4);
                    Console.WriteLine(Selection(selected) + "Five: " + opt5 + check5);
                    Console.WriteLine("-----------------------");

                    selected = saveSelect;

                
                Console.WriteLine("Aux Process: " + processTime.Restart().TotalMicroseconds);                
            }
            Emit?.Invoke(value);
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

        public void StatUpdate(Vector2 position) {
            
            pos1 = pos0;
            pos0 = position;

            vel1 = vel0;
            vel0 = Vector2.Distance(pos0, pos1);

            Console.WriteLine(vel0);
            
        }

        

        public HPETDeltaStopwatch processTime = new HPETDeltaStopwatch(true);
        public int selected, saveSelect;
        public double opt0, opt1, opt2, opt3, opt4, opt5;
        public bool check0, check1, check2, check3, check4, check5;
        public double vel0, vel1, accel0, accel1, jerk0, jerk1, snap0, snap1;
        public int auxInput;
        Vector2 pos0, pos1;
    }
}