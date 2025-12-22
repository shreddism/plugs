using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Plugin251219
{
    [PluginName("Plugin251219")]
    public class Plugin251219 : IPositionedPipelineElement<IDeviceReport>
    {
        public Plugin251219() : base()
        {
        }

       public PipelinePosition Position => PipelinePosition.PreTransform;

       public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                pos1 = pos0;
                pos0 = report.Position;

                dir1 = dir0;
                dir0 = pos0 - pos1;

                vel1 = vel0;
                vel0 = Vector2.Distance(pos0, pos1);

                accel0 = vel0 - vel1;

                index = Math.Clamp(Math.Abs(accel0) / vel0, 0, 1);

                outputVel0 = VelocityFilter(dir0);

                outputPos0 = outputPos0 + outputVel0;


            
                
             //   Console.WriteLine(Math.Sqrt(Math.Pow(dir0.X, 2) + Math.Pow(dir0.Y, 2)) - Math.Sqrt(Math.Pow(outputVel0.X, 2) + Math.Pow(outputVel0.Y, 2)));
                
             //   Console.WriteLine("------------------------------");
                
              //  Console.WriteLine(compensation);

         //      Console.WriteLine(scale);

                outputPos0 = Vector2.Lerp(outputPos0, pos0, 0.01f);
                
                report.Position = outputPos0;
                
            }
            else if (value is IAuxReport auxReport) {
                
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
            Emit?.Invoke(value);
        }

        public Vector2 VelocityFilter(Vector2 velocity) {

            disc = dir0 - outputVel0;
            dist = MathF.Sqrt(MathF.Pow(disc.X, 2) + MathF.Pow(disc.Y, 2));
            compensation = MathF.Log((MathF.Sqrt(MathF.Pow(outputVel0.X, 2) + MathF.Pow(outputVel0.Y, 2)) / (float)opt5) + 1) + 1;
            scale = (float)opt0 + (1 - (float)opt0) * 
            MathF.Pow(1 / (1 + (float)Math.Pow((float)opt1, -1 * (dist - (float)opt2) / MathF.Pow(compensation, (float)opt3))), (float)opt4 * compensation);
            
            outputVel0 = Vector2.Lerp(outputVel0, velocity, scale);

            return outputVel0;
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

        public int auxInput, selected, saveSelect;
        public double opt0 = 0;
        public double opt1 = Math.E;
        public double opt2 = 0;
        public double opt3 = 2;
        public double opt4 = 2;
        public double opt5 = 25;

        public Vector2 pos0, pos1, outputPos0, outputPos1, dir0, dir1, disc, outputVel0;
        public float dist, scale, compensation;
        public double vel0, vel1, accel0;
        
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}