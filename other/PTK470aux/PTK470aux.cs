using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;    

namespace Aux
{
    [PluginName("PTK-470 aux print in console")]
    public class aux : IPositionedPipelineElement<IDeviceReport>
    {
        public aux() : base()
        {
        }

       public PipelinePosition Position => PipelinePosition.PreTransform;

       public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is IAuxReport report)
            {
                if (report.Raw[4] == 127)
                    Console.WriteLine(5);
                else if (report.Raw[4] == 1)
                    Console.WriteLine(6);
                
                for (int i = 0; i < report.AuxButtons.Length; i++) {
                    if (report.AuxButtons[i])
                        Console.WriteLine(i);
                }
                                
            }
            Emit?.Invoke(value);
        }
    }
}